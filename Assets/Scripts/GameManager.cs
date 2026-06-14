using System.Collections;
using System.Collections.Generic;
using Abilities;
using ConveyorBelt;
using Core;
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
        [SerializeField] private LevelSpawner _levelSpawner;

        [Header("Level Configuration")]
        [SerializeField] private LevelConfig _currentLevelConfig;

        [Header("Ability Definitions")]
        [SerializeField] private AbilityDefinition[] _abilityDefinitions;

        public event System.Action OnLevelInitialized;
        public event System.Action<bool> OnGameEnded;

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
            SubscribeToMessages();
        }

        private void Start()
        {
            if (_currentLevelConfig != null)
            {
                StartCoroutine(InitializeAfterUIDocumentReady());
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            UnsubscribeFromMessages();
        }

        private IEnumerator InitializeAfterUIDocumentReady()
        {
            // Wait 2 frames to ensure UIDocument and all UI toolkit elements are fully constructed
            yield return null;
            yield return null;

            InitializeLevel();
        }

        private void SubscribeToMessages()
        {
            MessageDispatcher.MessageDispatcher.Subscribe<BoxCompletedMessage>(OnBoxCompletedMessage);
            MessageDispatcher.MessageDispatcher.Subscribe<RestartLevelMessage>(OnRestartLevelMessage);
        }

        private void UnsubscribeFromMessages()
        {
            MessageDispatcher.MessageDispatcher.Unsubscribe<BoxCompletedMessage>(OnBoxCompletedMessage);
            MessageDispatcher.MessageDispatcher.Unsubscribe<RestartLevelMessage>(OnRestartLevelMessage);
        }

        private void OnBoxCompletedMessage(BoxCompletedMessage message)
        {
            OnBoxCompleted();
        }

        private void OnRestartLevelMessage(RestartLevelMessage message)
        {
            RestartLevel();
        }

        /// <summary>
        /// Initialize or restart a level.
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

            // 1. Re-find scene references (they might be stale after restart)
            RefreshSceneReferences();

            // 2. Spawn runtime items via LevelSpawner
            if (_levelSpawner != null)
            {
                _levelSpawner.SpawnLevel(_currentLevelConfig);
            }

            // 3. Initialize AbilityManager
            InitializeAbilities();

            // 4. Initialize UI
            InitializeUI();

            Debug.Log($"[GameManager] Level {_currentLevelConfig.LevelNumber} initialized.");
            OnLevelInitialized?.Invoke();
        }

        private void RefreshSceneReferences()
        {
            _conveyorBelt = FindFirstObjectByType<ConveyorBelt.ConveyorBelt>();
            _boxConveyorBelt = FindFirstObjectByType<BoxConveyorBelt>();
            _levelSpawner = FindFirstObjectByType<LevelSpawner>();
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
            AbilityManager.Instance.SetCharges(AbilityType.Magnet, _currentLevelConfig.MagnetCount);
            AbilityManager.Instance.SetCharges(AbilityType.Hand, _currentLevelConfig.HandCount);
            AbilityManager.Instance.SetCharges(AbilityType.Shuffle, _currentLevelConfig.ShuffleCount);
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

            uiController.InitializeRuntime();
            uiController.SetLevel(_currentLevelConfig.LevelNumber);
            uiController.AddCoins(_currentLevelConfig.InitialCoins);

            var availableColors = _currentLevelConfig.GetAvailableBoxColors();
            if (_boxConveyorBelt != null && availableColors != null)
            {
                uiController.InitializeColorPanel(_boxConveyorBelt, availableColors);
                uiController.OnColorSelected -= OnColorButtonSelected;
                uiController.OnColorSelected += OnColorButtonSelected;
            }
        }

        private static Sprite _panelBoxSprite;

        private void OnColorButtonSelected(Core.ColorGroup colorGroup)
        {
            if (_boxConveyorBelt == null)
                return;

            var boxColor = (BoxConveyorBelt.ItemColorGroup)(int)colorGroup;

            if (!_boxConveyorBelt.CanAcceptMoreBoxes())
            {
                CheckLoseCondition();
                return;
            }

            GameObject boxObject = new GameObject($"Panel Box {colorGroup}");
            boxObject.transform.position = new Vector3(0f, -9.7f, 0f);

            SpriteRenderer renderer = boxObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPanelBoxSprite();
            colorGroup.ApplyTo(renderer);
            renderer.sortingOrder = 2;

            bool accepted = _boxConveyorBelt.TryAddBoxFromPanel(boxObject.transform, boxColor);

            if (!accepted)
                Destroy(boxObject);
        }

        private static Sprite GetPanelBoxSprite()
        {
            if (_panelBoxSprite != null)
                return _panelBoxSprite;

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _panelBoxSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

            return _panelBoxSprite;
        }

        public void OnBoxCompleted()
        {
            Debug.Log("[GameManager] Box completed! Check for win condition.");
            CheckWinCondition();
        }

        public void CheckWinCondition()
        {
            if (_conveyorBelt == null)
                return;

            if (_conveyorBelt.HasNoActiveItems())
            {
                EndGame(true);
            }
        }

        public void CheckLoseCondition()
        {
            if (_conveyorBelt == null || _boxConveyorBelt == null)
                return;

            if (!_boxConveyorBelt.CanAcceptMoreBoxes() && _conveyorBelt.HasActiveItems())
            {
                EndGame(false);
            }
        }

        public void EndGame(bool victory)
        {
            Debug.Log($"[GameManager] Game ended. Victory: {victory}");
            OnGameEnded?.Invoke(victory);
            MessageDispatcher.MessageDispatcher.Publish(new GameStateMessage(
                victory ? GameResult.Win : GameResult.Lose));
        }

        /// <summary>
        /// Reset and restart the current level WITHOUT scene reload.
        /// Just reset all systems in-place so UI references stay valid.
        /// </summary>
        public void RestartLevel()
        {
            Debug.Log("[GameManager] Restarting level in-place...");

            // Clear spawned items and boxes
            if (_levelSpawner != null)
            {
                _levelSpawner.ClearSpawned();
            }

            // Clear boxes on conveyor belt
            if (_boxConveyorBelt != null)
            {
                _boxConveyorBelt.ClearAllBoxes();
            }

            // Re-initialize everything
            InitializeLevel();
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