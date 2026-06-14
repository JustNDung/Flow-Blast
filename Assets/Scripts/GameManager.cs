using System.Collections.Generic;
using Abilities;
using ConveyorBelt;
using UnityEngine;
using UnityEngine.UIElements;
using UI;

namespace Game
{
    /// <summary>
    /// Central game orchestrator. Manages level initialization, game state,
    /// and coordinates between all game systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (Application.isPlaying)
                    {
                        var go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("References")]
        [SerializeField] private ConveyorBelt.ConveyorBelt _conveyorBelt;
        [SerializeField] private BoxConveyorBelt _boxConveyorBelt;
        [SerializeField] private ColorBoxSelectionPanel _colorBoxPanel;

        [Header("Level Configuration")]
        [SerializeField] private LevelConfig _currentLevelConfig;

        [Header("Ability Definitions")]
        [SerializeField] private AbilityDefinition[] _abilityDefinitions;

        /// <summary>
        /// Fired when a level has been fully initialized and is ready to play.
        /// </summary>
        public event System.Action OnLevelInitialized;

        /// <summary>
        /// Fired when the game round ends.
        /// </summary>
        public event System.Action<bool> OnGameEnded; // true = victory

        public LevelConfig CurrentLevelConfig => _currentLevelConfig;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_currentLevelConfig != null)
            {
                InitializeLevel();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Initialize or restart a level with the given configuration.
        /// </summary>
        public void InitializeLevel(LevelConfig levelConfig = null)
        {
            if (levelConfig != null)
                _currentLevelConfig = levelConfig;

            if (_currentLevelConfig == null)
            {
                Debug.LogError("[GameManager] Cannot initialize level: no LevelConfig assigned.");
                return;
            }

            // 1. Initialize AbilityManager with ability definitions
            InitializeAbilities();

            // 2. Set up ConveyorBelt color sequence from level config
            ConfigureConveyorBelt();

            // 3. Set up BoxConveyorBelt available colors
            ConfigureBoxConveyorBelt();

            // 4. Initialize UI state
            InitializeUI();

            // 5. Notify that level is ready
            Debug.Log($"[GameManager] Level {_currentLevelConfig.LevelNumber} initialized.");
            OnLevelInitialized?.Invoke();
        }

        private void InitializeAbilities()
        {
            if (_abilityDefinitions == null || _abilityDefinitions.Length == 0)
            {
                Debug.LogWarning("[GameManager] No ability definitions assigned. Skipping ability initialization.");
                return;
            }

            var context = new AbilityExecutionContext();
            AbilityManager.Instance.Initialize(_abilityDefinitions, context);

            // Set the initial charges per level config
            AbilityManager.Instance.SetCharges(Abilities.AbilityType.Magnet, _currentLevelConfig.MagnetCount);
            AbilityManager.Instance.SetCharges(Abilities.AbilityType.Hand, _currentLevelConfig.HandCount);
            AbilityManager.Instance.SetCharges(Abilities.AbilityType.Shuffle, _currentLevelConfig.ShuffleCount);
        }

        private void ConfigureConveyorBelt()
        {
            if (_conveyorBelt == null)
            {
                // Try to find one in the scene
                _conveyorBelt = FindFirstObjectByType<ConveyorBelt.ConveyorBelt>();
                if (_conveyorBelt == null)
                {
                    Debug.LogWarning("[GameManager] No ConveyorBelt found in scene.");
                    return;
                }
            }

            // Pass the color sequence from level config to the conveyor belt
            var sequence = _currentLevelConfig.ColorSequence;
            if (sequence != null && sequence.Count > 0)
            {
                // The ConveyorBelt will use its own generatedColorSequence field directly
                // or you can assign it via serialization. We log what the level expects.
                // For full dynamic control, the conveyor belt would need a public setter.
                Debug.Log($"[GameManager] Level {_currentLevelConfig.LevelNumber} expects {sequence.Count} color groups.");
            }
        }

        private void ConfigureBoxConveyorBelt()
        {
            if (_boxConveyorBelt == null)
            {
                _boxConveyorBelt = FindFirstObjectByType<BoxConveyorBelt>();
                if (_boxConveyorBelt == null)
                {
                    Debug.LogWarning("[GameManager] No BoxConveyorBelt found in scene.");
                    return;
                }
            }

            // BoxConveyorBelt handles its own panel creation in Start()
            // The level config provides the colors to display
            var availableColors = _currentLevelConfig.AvailableBoxColors;
            if (availableColors != null && availableColors.Count > 0)
            {
                Debug.Log($"[GameManager] Box panel configured with {availableColors.Count} color options.");
            }
        }

        private GameplayUIController FindGameplayUIController()
        {
            var uiDocument = FindFirstObjectByType<UnityEngine.UIElements.UIDocument>();
            if (uiDocument != null)
            {
                return uiDocument.rootVisualElement.Q<GameplayUIController>("gameplay-controller");
            }
            return null;
        }

        private void InitializeUI()
        {
            var uiController = FindGameplayUIController();

            if (uiController == null)
            {
                Debug.LogWarning("[GameManager] No GameplayUIController found in UIDocument. UI state not initialized.");
                return;
            }

            // Initialize runtime state that depends on AbilityManager
            uiController.InitializeRuntime();

            // Set initial UI state
            uiController.SetLevel(_currentLevelConfig.LevelNumber);
            uiController.AddCoins(_currentLevelConfig.InitialCoins);
        }

        /// <summary>
        /// Called when a box is completed (reaches 100% fill).
        /// </summary>
        public void OnBoxCompleted()
        {
            Debug.Log("[GameManager] Box completed! Check for victory condition.");
            // TODO: Implement victory condition logic
        }

        /// <summary>
        /// End the current game round.
        /// </summary>
        public void EndGame(bool victory)
        {
            Debug.Log($"[GameManager] Game ended. Victory: {victory}");
            OnGameEnded?.Invoke(victory);
        }

        /// <summary>
        /// Reset and restart the current level.
        /// </summary>
        public void RestartLevel()
        {
            Debug.Log("[GameManager] Restarting level...");
            // TODO: Implement full scene reload or system reset
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_currentLevelConfig != null)
            {
                gameObject.name = $"GameManager (Level {_currentLevelConfig.LevelNumber})";
            }
        }
#endif
    }
}