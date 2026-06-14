using System;
using System.Collections.Generic;
using Abilities;
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

        // Ability UI elements - dynamically bound via data
        private readonly Dictionary<AbilityType, AbilityBinding> _abilityBindings = new();

        private int _currentLevel = 1;
        private int _coins = 300;

        // Auto-subscribe to AbilityManager events
        private bool _isInitialized;

        public event Action<AbilityType> OnAbilityUsed;
        public event Action OnBuyCoins;

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
            BindToAbilityManager();
            UpdateUI();
            HideSettingsPopup();
            _isInitialized = true;
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

            // Build ability bindings dynamically from all registered ability definitions
            _abilityBindings.Clear();
            var definitions = AbilityManager.Instance.GetAllDefinitions();
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

            // Register ability button clicks from bindings
            foreach (var kvp in _abilityBindings)
            {
                var binding = kvp.Value;
                if (binding.Button != null)
                {
                    var abilityType = kvp.Key; // capture for closure
                    binding.Button.clicked += () => UseAbility(abilityType);
                }
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

            foreach (var kvp in _abilityBindings)
            {
                var binding = kvp.Value;
                if (binding.Button != null)
                {
                    var abilityType = kvp.Key; // capture for closure
                    binding.Button.clicked -= () => UseAbility(abilityType);
                }
            }
        }

        private void SubscribeToMessages()
        {
            MessageDispatcher.MessageDispatcher.Subscribe<Audio.SoundSettingChangedMessage>(OnSoundChanged);
        }

        private void UnsubscribeFromMessages()
        {
            MessageDispatcher.MessageDispatcher.Unsubscribe<Audio.SoundSettingChangedMessage>(OnSoundChanged);
        }

        private void OnSoundChanged(Audio.SoundSettingChangedMessage message)
        {
            if (message.Enabled)
            {
                // Audio.AudioManager.Instance.PlaySound(uiClickSound);
            }
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