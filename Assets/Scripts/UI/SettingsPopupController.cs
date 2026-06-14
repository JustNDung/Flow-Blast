using System;
using UnityEngine;
using UnityEngine.UIElements;

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

        public event Action<bool> OnSoundChanged;
        public event Action<bool> OnMusicChanged;
        public event Action<bool> OnVibrateChanged;

        private bool _soundEnabled = true;
        private bool _musicEnabled = true;
        private bool _vibrateEnabled = true;

        public SettingsPopupController()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttached);
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

            RegisterEvents();

            RefreshStates();
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

        private void ToggleSound()
        {
            _soundEnabled = !_soundEnabled;
            RefreshStates();

            OnSoundChanged?.Invoke(_soundEnabled);
        }

        private void ToggleMusic()
        {
            _musicEnabled = !_musicEnabled;
            RefreshStates();

            OnMusicChanged?.Invoke(_musicEnabled);
        }

        private void ToggleVibrate()
        {
            _vibrateEnabled = !_vibrateEnabled;
            RefreshStates();

            OnVibrateChanged?.Invoke(_vibrateEnabled);
        }

        private void RefreshStates()
        {
            SetToggleClass(_soundButton, _soundEnabled);
            SetToggleClass(_musicButton, _musicEnabled);
            SetToggleClass(_vibrateButton, _vibrateEnabled);
        }

        private void SetToggleClass(Button button, bool state)
        {
            button.EnableInClassList("toggle-off", !state);
        }

        public void SetVersion(string version)
        {
            _versionLabel.text = version;
        }
    }
}