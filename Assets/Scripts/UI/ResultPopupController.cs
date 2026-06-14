using System;
using UnityEngine.UIElements;

namespace UI
{
    [UxmlElement]
    public partial class ResultPopupController : VisualElement
    {
        private VisualElement _resultOverlay;
        private VisualElement _resultContainer;
        private VisualElement _iconContainer;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Button _restartButton;
        private Button _nextLevelButton;
        private Button _mainMenuButton;

        public event Action OnRestart;
        public event Action OnNextLevel;
        public event Action OnMainMenu;

        public ResultPopupController()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttached);
        }

        private void OnAttached(AttachToPanelEvent evt)
        {
            _resultOverlay = this.Q<VisualElement>("result-overlay");
            _resultContainer = this.Q<VisualElement>("result-container");
            _iconContainer = this.Q<VisualElement>("icon-container");
            _titleLabel = this.Q<Label>("result-title");
            _subtitleLabel = this.Q<Label>("result-subtitle");
            _restartButton = this.Q<Button>("restart-button");
            _nextLevelButton = this.Q<Button>("next-level-button");
            _mainMenuButton = this.Q<Button>("main-menu-button");

            if (_restartButton != null)
                _restartButton.clicked += () => OnRestart?.Invoke();

            if (_nextLevelButton != null)
                _nextLevelButton.clicked += () => OnNextLevel?.Invoke();

            if (_mainMenuButton != null)
                _mainMenuButton.clicked += () => OnMainMenu?.Invoke();
        }

        /// <summary>
        /// Show the result popup with win/lose state.
        /// </summary>
        public void Show(GameResult result)
        {
            if (_resultOverlay == null || _resultContainer == null)
                return;

            _resultOverlay.style.display = DisplayStyle.Flex;

            if (result == GameResult.Win)
            {
                ShowWin();
            }
            else
            {
                ShowLose();
            }

            // Animate in
            _resultContainer.style.opacity = 0f;
            _resultContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            _resultContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _resultContainer.experimental.animation.Start(0f, 1f, 300, (elem, value) =>
            {
                elem.style.opacity = value;
            });
        }

        private void ShowWin()
        {
            if (_titleLabel != null)
                _titleLabel.text = "LEVEL COMPLETE!";

            if (_subtitleLabel != null)
                _subtitleLabel.text = "Amazing! You cleared all the items!";

            if (_iconContainer != null)
            {
                _iconContainer.RemoveFromClassList("lose-icon");
                _iconContainer.AddToClassList("win-icon");
            }

            // Show next level button for win
            if (_nextLevelButton != null)
                _nextLevelButton.style.display = DisplayStyle.Flex;
        }

        private void ShowLose()
        {
            if (_titleLabel != null)
                _titleLabel.text = "LEVEL FAILED";

            if (_subtitleLabel != null)
                _subtitleLabel.text = "The conveyor belt is full! Try again.";

            if (_iconContainer != null)
            {
                _iconContainer.RemoveFromClassList("win-icon");
                _iconContainer.AddToClassList("lose-icon");
            }

            // Hide next level button for lose
            if (_nextLevelButton != null)
                _nextLevelButton.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Hide the result popup.
        /// </summary>
        public void Hide()
        {
            if (_resultOverlay != null)
                _resultOverlay.style.display = DisplayStyle.None;
        }
    }
}