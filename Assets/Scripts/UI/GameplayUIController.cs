using System;
using System.Collections.Generic;
using Abilities;
using ConveyorBelt;
using Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class GameplayUIController : VisualElement
    {
        private Button _settingsButton;
        private Label _levelLabel;
        private Label _coinAmount;
        private Button _addCoinButton;

        private SettingsPopupController _settingsPopup;
        private ResultPopupController _resultPopup;

        // Color selection panel
        private VisualElement _colorPanelInner;
        private readonly Dictionary<ColorGroup, Button> _colorButtons = new();
        private BoxConveyorBelt _boxConveyorBelt;

        // Ability UI elements - dynamically bound via data
        private readonly Dictionary<AbilityType, AbilityBinding> _abilityBindings = new();

        private int _currentLevel = 1;
        private int _coins = 300;

        // Auto-subscribe to AbilityManager events
        private bool _isInitialized;

        public event Action<AbilityType> OnAbilityUsed;
        public event Action OnBuyCoins;
        public event Action<ColorGroup> OnColorSelected;

        // Result popup events
        public event Action OnRestartLevel;
        public event Action OnNextLevel;
        public event Action OnMainMenu;

        public GameplayUIController()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttached);
            RegisterCallback<DetachFromPanelEvent>(OnDetached);
        }

        private void OnAttached(AttachToPanelEvent evt)
        {
            InitializeUI();
            RegisterEvents();
            SubscribeToMessages();
            HideSettingsPopup();
            HideResultPopup();
        }

        private void OnDetached(DetachFromPanelEvent evt)
        {
            UnregisterEvents();
            UnsubscribeFromMessages();
            UnbindFromAbilityManager();
            _isInitialized = false;
        }

        private void InitializeUI()
        {
            _settingsButton = this.Q<Button>("settings-button");
            _levelLabel = this.Q<Label>("level-label");
            _coinAmount = this.Q<Label>("coin-amount");
            _addCoinButton = this.Q<Button>("add-coin-button");
            _settingsPopup = this.Q<SettingsPopupController>("settings-popup");
            InitializeResultPopup();

            // Do NOT call RebindAbilities() here. OnAttached fires during UIDocument.OnEnable
            // (Awake phase), which is before GameManager.Start. The AbilityManager singleton may
            // not exist yet, or may not be initialized. GameManager.InitializeLevel() calls
            // RebindAbilities() after initializing the AbilityManager.
        }

        /// <summary>
        /// Initialize runtime state that depends on AbilityManager being ready.
        /// Called by GameManager after AbilityManager is initialized.
        /// </summary>
        public void InitializeRuntime()
        {
            RebindAbilities();
            RegisterAbilityButtons();
            BindToAbilityManager();
            UpdateUI();
            _isInitialized = true;
        }

        /// <summary>
        /// Rebuild ability bindings from the current AbilityManager state.
        /// Safe to call multiple times (e.g., after AbilityManager.Initialize()).
        /// </summary>
        public void RebindAbilities()
        {
            _abilityBindings.Clear();

            var definitions = AbilityManager.Instance.GetAllDefinitions();
            if (definitions == null || definitions.Count == 0)
            {
                // AbilityManager not initialized yet - skip silently
                return;
            }

            foreach (var definition in definitions)
            {
                var container = this.Q<VisualElement>(definition.UIContainerName);
                var button = this.Q<Button>(definition.UIButtonName);
                var countLabel = this.Q<Label>(definition.UICountLabelName);

                if (container == null)
                {
                    Debug.LogWarning($"[GameplayUI] UI container '{definition.UIContainerName}' not found for {definition.AbilityName}. Skipping.");
                    continue;
                }

                _abilityBindings[definition.AbilityType] = new AbilityBinding
                {
                    Definition = definition,
                    Container = container,
                    Button = button,
                    CountLabel = countLabel
                };
            }
        }

        private void RegisterEvents()
        {
            _settingsButton.clicked += ShowSettingsPopup;
            _addCoinButton.clicked += HandleBuyCoins;

            if (_settingsPopup != null)
            {
                _settingsPopup.OnClose += HideSettingsPopup;
                _settingsPopup.OnRetry += HandleRetry;
                _settingsPopup.OnRestore += HandleRestore;
                _settingsPopup.OnRate += HandleRate;
            }
        }

        private void UnregisterEvents()
        {
            if (_settingsButton != null)
            {
                _settingsButton.clicked -= ShowSettingsPopup;
                _addCoinButton.clicked -= HandleBuyCoins;
            }

            if (_settingsPopup != null)
            {
                _settingsPopup.OnClose -= HideSettingsPopup;
                _settingsPopup.OnRetry -= HandleRetry;
                _settingsPopup.OnRestore -= HandleRestore;
                _settingsPopup.OnRate -= HandleRate;
            }
        }

        private void RegisterAbilityButtons()
        {
            foreach (var kvp in _abilityBindings)
            {
                var binding = kvp.Value;
                if (binding.Button != null)
                {
                    var abilityType = kvp.Key;
                    binding.Button.clicked += () => UseAbility(abilityType);
                }
            }
        }

        private void UnregisterAbilityButtons()
        {
            foreach (var kvp in _abilityBindings)
            {
                var binding = kvp.Value;
                if (binding.Button != null)
                {
                    var abilityType = kvp.Key;
                    binding.Button.clicked -= () => UseAbility(abilityType);
                }
            }
        }

        private void SubscribeToMessages()
        {
            MessageDispatcher.MessageDispatcher.Subscribe<Audio.SoundSettingChangedMessage>(OnSoundChanged);
            MessageDispatcher.MessageDispatcher.Subscribe<GameStateMessage>(OnGameStateChanged);
        }

        private void UnsubscribeFromMessages()
        {
            MessageDispatcher.MessageDispatcher.Unsubscribe<Audio.SoundSettingChangedMessage>(OnSoundChanged);
            MessageDispatcher.MessageDispatcher.Unsubscribe<GameStateMessage>(OnGameStateChanged);
        }

        private void OnSoundChanged(Audio.SoundSettingChangedMessage message)
        {
            if (message.Enabled)
            {
                // Audio.AudioManager.Instance.PlaySound(uiClickSound);
            }
        }

        private void OnGameStateChanged(GameStateMessage message)
        {
            ShowResultPopup(message.Result);
        }

        private void BindToAbilityManager()
        {
            AbilityManager.Instance.OnCountChanged += OnAbilityCountChanged;
        }

        private void UnbindFromAbilityManager()
        {
            if (_isInitialized)
            {
                AbilityManager.Instance.OnCountChanged -= OnAbilityCountChanged;
            }
        }

        private void OnAbilityCountChanged(AbilityType type, int newCount)
        {
            if (_abilityBindings.TryGetValue(type, out var binding))
            {
                UpdateAbilityUI(binding);
            }
        }

        private void UpdateUI()
        {
            if (_levelLabel == null) return;

            _levelLabel.text = $"LEVEL {_currentLevel}";
            _coinAmount.text = _coins.ToString();

            // Update all ability UI from manager data
            foreach (var kvp in _abilityBindings)
            {
                UpdateAbilityUI(kvp.Value);
            }
        }

        private void UpdateAbilityUI(AbilityBinding binding)
        {
            if (binding.CountLabel == null) return;

            var count = AbilityManager.Instance.GetCount(binding.Definition.AbilityType);
            binding.CountLabel.text = count.ToString();

            if (binding.Button != null)
            {
                binding.Button.SetEnabled(count > 0);
            }
        }

        private void ShowSettingsPopup()
        {
            var overlay = this.Q<VisualElement>("settings-overlay");
            if (overlay != null)
            {
                overlay.style.display = DisplayStyle.Flex;
            }
        }

        private void HideSettingsPopup()
        {
            var overlay = this.Q<VisualElement>("settings-overlay");
            if (overlay != null)
            {
                overlay.style.display = DisplayStyle.None;
            }
        }

        private void UseAbility(AbilityType type)
        {
            if (AbilityManager.Instance.TryUseAbility(type))
            {
                OnAbilityUsed?.Invoke(type);
            }
        }

        private void InitializeResultPopup()
        {
            _resultPopup = this.Q<ResultPopupController>("result-popup");

            if (_resultPopup == null)
                return;

            _resultPopup.OnRestart += () => OnRestartLevel?.Invoke();
            _resultPopup.OnNextLevel += () => OnNextLevel?.Invoke();
            _resultPopup.OnMainMenu += () => OnMainMenu?.Invoke();
        }

        private void ShowResultPopup(GameResult result)
        {
            _resultPopup?.Show(result);
        }

        private void HideResultPopup()
        {
            _resultPopup?.Hide();
        }

        private void HandleBuyCoins()
        {
            OnBuyCoins?.Invoke();
        }

        private void HandleRetry()
        {
            HideSettingsPopup();
            Debug.Log("Retry level");
        }

        private void HandleRestore()
        {
            Debug.Log("Restore purchases");
        }

        private void HandleRate()
        {
            Debug.Log("Rate game");
        }

        // Public methods for external control
        public void SetLevel(int level)
        {
            _currentLevel = level;
            UpdateUI();
        }

        public void AddCoins(int amount)
        {
            _coins += amount;
            UpdateUI();
            MessageDispatcher.MessageDispatcher.Publish(new CoinChangedMessage(amount, _coins));
        }

        public void SpendCoins(int amount)
        {
            var oldCoins = _coins;
            _coins = Mathf.Max(0, _coins - amount);
            UpdateUI();
            MessageDispatcher.MessageDispatcher.Publish(new CoinChangedMessage(-(oldCoins - _coins), _coins));
        }

        /// <summary>
        /// Add ability charges via the AbilityManager.
        /// </summary>
        public void AddAbility(AbilityType type, int count = 1)
        {
            AbilityManager.Instance.AddCharges(type, count);
        }

        #region Color Selection Panel

        /// <summary>
        /// Initialize the color selection panel with available colors.
        /// Called by GameManager after BoxConveyorBelt is ready.
        /// </summary>
        public void InitializeColorPanel(BoxConveyorBelt boxConveyorBelt, IReadOnlyList<ColorGroup> availableColors)
        {
            _boxConveyorBelt = boxConveyorBelt;
            _colorPanelInner = this.Q<VisualElement>("color-panel-inner");

            if (_colorPanelInner == null)
            {
                Debug.LogWarning("[GameplayUI] color-panel-inner not found in UXML.");
                return;
            }

            // Clear any existing buttons
            _colorPanelInner.Clear();
            _colorButtons.Clear();

            if (availableColors == null || availableColors.Count == 0)
            {
                _colorPanelInner.style.display = DisplayStyle.None;
                return;
            }

            _colorPanelInner.style.display = DisplayStyle.Flex;

            foreach (var colorGroup in availableColors)
            {
                var button = new Button();
                button.name = $"color-btn-{colorGroup}";
                button.AddToClassList("color-button");
                button.style.backgroundColor = colorGroup.ToUnityColor();
                button.tooltip = colorGroup.ToString();

                var colorGroupCaptured = colorGroup;
                button.clicked += () => HandleColorButtonClicked(colorGroupCaptured);

                _colorPanelInner.Add(button);
                _colorButtons[colorGroup] = button;
            }
        }

        /// <summary>
        /// Update the visual state of color buttons (e.g., disable if belt is full).
        /// </summary>
        public void RefreshColorPanel()
        {
            bool canAdd = _boxConveyorBelt != null;
            foreach (var kvp in _colorButtons)
            {
                kvp.Value.SetEnabled(canAdd);
            }
        }

        private void HandleColorButtonClicked(ColorGroup colorGroup)
        {
            if (_boxConveyorBelt == null)
                return;

            OnColorSelected?.Invoke(colorGroup);
        }

        #endregion

        /// <summary>
        /// Internal binding data for a single ability in the UI.
        /// </summary>
        private class AbilityBinding
        {
            public AbilityDefinition Definition;
            public VisualElement Container;
            public Button Button;
            public Label CountLabel;
        }
    }
}