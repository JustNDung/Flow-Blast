using System;
using UnityEngine.UIElements;
using Audio;

namespace UI
{
    [UxmlElement]
    public partial class SettingsPopupController : VisualElement
    {
        private Button _closeButton;

        private Button _soundButton;
        private Button _musicButton;
        private Button _vibrateButton;

        private Button _retryButton;
        private Button _restoreButton;
        private Button _rateButton;

        private Label _versionLabel;

        public event Action OnClose;
        public event Action OnRetry;
        public event Action OnRestore;
        public event Action OnRate;

        public SettingsPopupController()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttached);
            RegisterCallback<DetachFromPanelEvent>(OnDetached);
        }

        private void OnAttached(AttachToPanelEvent evt)
        {
            _closeButton = this.Q<Button>("close-button");
            _soundButton = this.Q<Button>("sound-button");
            _musicButton = this.Q<Button>("music-button");
            _vibrateButton = this.Q<Button>("vibrate-button");
            _retryButton = this.Q<Button>("retry-button");
            _restoreButton = this.Q<Button>("restore-button");
            _rateButton = this.Q<Button>("rate-button");
            _versionLabel = this.Q<Label>("version-label");

            // Only register events and subscribe if all elements are found
            if (_closeButton != null && _soundButton != null && _musicButton != null &&
                _vibrateButton != null && _retryButton != null && _restoreButton != null &&
                _rateButton != null && _versionLabel != null)
            {
                RegisterEvents();
                SubscribeToMessages();
                RefreshStates();
            }
        }

        private void OnDetached(DetachFromPanelEvent evt)
        {
            UnsubscribeFromMessages();
        }

        private void RegisterEvents()
        {
            _closeButton.clicked += () =>
            {
                OnClose?.Invoke();
            };

            _retryButton.clicked += () =>
            {
                OnRetry?.Invoke();
            };

            _restoreButton.clicked += () =>
            {
                OnRestore?.Invoke();
            };

            _rateButton.clicked += () =>
            {
                OnRate?.Invoke();
            };

            _soundButton.clicked += ToggleSound;
            _musicButton.clicked += ToggleMusic;
            _vibrateButton.clicked += ToggleVibrate;
        }

        private void SubscribeToMessages()
        {
            MessageDispatcher.MessageDispatcher.Subscribe<SoundSettingChangedMessage>(OnSoundSettingChanged);
            MessageDispatcher.MessageDispatcher.Subscribe<MusicSettingChangedMessage>(OnMusicSettingChanged);
            MessageDispatcher.MessageDispatcher.Subscribe<VibrateSettingChangedMessage>(OnVibrateSettingChanged);
        }

        private void UnsubscribeFromMessages()
        {
            MessageDispatcher.MessageDispatcher.Unsubscribe<SoundSettingChangedMessage>(OnSoundSettingChanged);
            MessageDispatcher.MessageDispatcher.Unsubscribe<MusicSettingChangedMessage>(OnMusicSettingChanged);
            MessageDispatcher.MessageDispatcher.Unsubscribe<VibrateSettingChangedMessage>(OnVibrateSettingChanged);
        }

        private void OnSoundSettingChanged(SoundSettingChangedMessage message)
        {
            RefreshStates();
        }

        private void OnMusicSettingChanged(MusicSettingChangedMessage message)
        {
            RefreshStates();
        }

        private void OnVibrateSettingChanged(VibrateSettingChangedMessage message)
        {
            RefreshStates();
        }

        private void ToggleSound()
        {
            AudioManager.Instance.SetSound(!AudioManager.Instance.SoundEnabled);
        }

        private void ToggleMusic()
        {
            AudioManager.Instance.SetMusic(!AudioManager.Instance.MusicEnabled);
        }

        private void ToggleVibrate()
        {
            AudioManager.Instance.SetVibrate(!AudioManager.Instance.VibrateEnabled);
        }

        private void RefreshStates()
        {
            if (_soundButton == null || _musicButton == null || _vibrateButton == null)
                return;

            if (AudioManager.Instance == null)
                return;

            SetToggleClass(_soundButton, AudioManager.Instance.SoundEnabled);
            SetToggleClass(_musicButton, AudioManager.Instance.MusicEnabled);
            SetToggleClass(_vibrateButton, AudioManager.Instance.VibrateEnabled);
        }

        private void SetToggleClass(Button button, bool state)
        {
            if (button != null)
            {
                button.EnableInClassList("toggle-off", !state);
            }
        }

        public void SetVersion(string version)
        {
            if (_versionLabel != null)
            {
                _versionLabel.text = version;
            }
        }
    }
}