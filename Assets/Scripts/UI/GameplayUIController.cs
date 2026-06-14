using System;
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

        private Button _magnetButton;
        private Button _handButton;
        private Button _shuffleButton;

        private Label _magnetCount;
        private Label _handCount;
        private Label _shuffleCount;

        private SettingsPopupController _settingsPopup;

        private int _currentLevel = 1;
        private int _coins = 300;
        private int _magnetAbilityCount = 1;
        private int _handAbilityCount = 1;
        private int _shuffleAbilityCount = 1;

        public event Action OnMagnetUsed;
        public event Action OnHandUsed;
        public event Action OnShuffleUsed;
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
            UpdateUI();

            HideSettingsPopup();
        }

        private void OnDetached(DetachFromPanelEvent evt)
        {
            UnregisterEvents();
            UnsubscribeFromMessages();
        }

        private void InitializeUI()
        {
            _settingsButton = this.Q<Button>("settings-button");
            _levelLabel = this.Q<Label>("level-label");
            _coinAmount = this.Q<Label>("coin-amount");
            _addCoinButton = this.Q<Button>("add-coin-button");

            _magnetButton = this.Q<Button>("magnet-button");
            _handButton = this.Q<Button>("hand-button");
            _shuffleButton = this.Q<Button>("shuffle-button");

            _magnetCount = this.Q<Label>("magnet-count");
            _handCount = this.Q<Label>("hand-count");
            _shuffleCount = this.Q<Label>("shuffle-count");

            _settingsPopup = this.Q<SettingsPopupController>("settings-popup");
        }

        private void RegisterEvents()
        {
            _settingsButton.clicked += ShowSettingsPopup;
            _addCoinButton.clicked += HandleBuyCoins;

            _magnetButton.clicked += UseMagnetAbility;
            _handButton.clicked += UseHandAbility;
            _shuffleButton.clicked += UseShuffleAbility;

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

                _magnetButton.clicked -= UseMagnetAbility;
                _handButton.clicked -= UseHandAbility;
                _shuffleButton.clicked -= UseShuffleAbility;
            }

            if (_settingsPopup != null)
            {
                _settingsPopup.OnClose -= HideSettingsPopup;
                _settingsPopup.OnRetry -= HandleRetry;
                _settingsPopup.OnRestore -= HandleRestore;
                _settingsPopup.OnRate -= HandleRate;
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
            // Play UI sound feedback if enabled
            if (message.Enabled)
            {
                // Audio.AudioManager.Instance.PlaySound(uiClickSound);
            }
        }

        private void UpdateUI()
        {
            if (_levelLabel == null) return;

            _levelLabel.text = $"LEVEL {_currentLevel}";
            _coinAmount.text = _coins.ToString();
            _magnetCount.text = _magnetAbilityCount.ToString();
            _handCount.text = _handAbilityCount.ToString();
            _shuffleCount.text = _shuffleAbilityCount.ToString();

            _magnetButton.SetEnabled(_magnetAbilityCount > 0);
            _handButton.SetEnabled(_handAbilityCount > 0);
            _shuffleButton.SetEnabled(_shuffleAbilityCount > 0);
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

        private void UseMagnetAbility()
        {
            if (_magnetAbilityCount <= 0) return;

            _magnetAbilityCount--;
            UpdateUI();
            OnMagnetUsed?.Invoke();
            MessageDispatcher.MessageDispatcher.Publish(new AbilityUsedMessage(AbilityType.Magnet));
        }

        private void UseHandAbility()
        {
            if (_handAbilityCount <= 0) return;

            _handAbilityCount--;
            UpdateUI();
            OnHandUsed?.Invoke();
            MessageDispatcher.MessageDispatcher.Publish(new AbilityUsedMessage(AbilityType.Hand));
        }

        private void UseShuffleAbility()
        {
            if (_shuffleAbilityCount <= 0) return;

            _shuffleAbilityCount--;
            UpdateUI();
            OnShuffleUsed?.Invoke();
            MessageDispatcher.MessageDispatcher.Publish(new AbilityUsedMessage(AbilityType.Shuffle));
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

        public void AddAbility(AbilityType type, int count = 1)
        {
            switch (type)
            {
                case AbilityType.Magnet:
                    _magnetAbilityCount += count;
                    break;
                case AbilityType.Hand:
                    _handAbilityCount += count;
                    break;
                case AbilityType.Shuffle:
                    _shuffleAbilityCount += count;
                    break;
            }
            UpdateUI();
        }
    }

    public enum AbilityType
    {
        Magnet,
        Hand,
        Shuffle
    }
}
