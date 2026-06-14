using System.Collections.Generic;
using ConveyorBelt;
using Game;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles runtime spawning and cleanup of conveyor belt items and boxes based on LevelConfig.
    /// Replaces the inline generation logic in ConveyorBelt.Awake with a clean, decoupled system.
    /// Now fully utilizes both ItemPrefab and BoxPrefab for prefab-based spawning.
    /// </summary>
    public class LevelSpawner : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject _itemPrefab;
        [SerializeField] private GameObject _boxPrefab;
        [SerializeField] private GameObject _panelBoxPrefab;

        [Header("Runtime Parent Transforms")]
        [SerializeField] private Transform _itemParent;
        [SerializeField] private Transform _boxParent;

        private ConveyorBelt.ConveyorBelt _conveyorBelt;
        private BoxConveyorBelt _boxConveyorBelt;
        private List<ConveyorBelt.ConveyorBelt.ConveyorBeltItem> _spawnedItems;
        private List<BoxConveyorBelt.ConveyorBox> _spawnedBoxes;

        public IReadOnlyList<ConveyorBelt.ConveyorBelt.ConveyorBeltItem> SpawnedItems => _spawnedItems;
        public IReadOnlyList<BoxConveyorBelt.ConveyorBox> SpawnedBoxes => _spawnedBoxes;

        private void Awake()
        {
            _conveyorBelt = FindFirstObjectByType<ConveyorBelt.ConveyorBelt>();
            _boxConveyorBelt = FindFirstObjectByType<BoxConveyorBelt>();
        }

        /// <summary>
        /// Spawn items and boxes for the given level configuration.
        /// Called by GameManager during level initialization.
        /// </summary>
        public void SpawnLevel(LevelConfig config)
        {
            ClearSpawned();
            SpawnItems(config);
            SpawnBoxes(config);
        }

        /// <summary>
        /// Destroy all previously spawned runtime objects.
        /// </summary>
        public void ClearSpawned()
        {
            if (_spawnedItems != null)
            {
                for (int i = 0; i < _spawnedItems.Count; i++)
                {
                    if (_spawnedItems[i] != null && _spawnedItems[i].item != null)
                        Destroy(_spawnedItems[i].item.gameObject);
                }
                _spawnedItems.Clear();
            }

            if (_spawnedBoxes != null)
            {
                for (int i = 0; i < _spawnedBoxes.Count; i++)
                {
                    if (_spawnedBoxes[i] != null && _spawnedBoxes[i].box != null)
                        Destroy(_spawnedBoxes[i].box.gameObject);
                }
                _spawnedBoxes.Clear();
            }
        }

        private void SpawnItems(LevelConfig config)
        {
            if (_conveyorBelt == null || _itemPrefab == null)
                return;

            var colorSequence = config.GetColorSequence();
            if (colorSequence == null || colorSequence.Count == 0)
                return;

            Transform parent = _itemParent != null ? _itemParent : transform;
            _spawnedItems = new List<ConveyorBelt.ConveyorBelt.ConveyorBeltItem>();

            for (int groupIndex = 0; groupIndex < colorSequence.Count; groupIndex++)
            {
                ColorGroup colorGroup = colorSequence[groupIndex];
                int blocksPerGroup = _conveyorBelt.GroupCapacity;

                for (int blockIndex = 0; blockIndex < blocksPerGroup; blockIndex++)
                {
                    GameObject instance = Instantiate(_itemPrefab, parent);
                    instance.name = $"Item_{colorGroup}_{groupIndex + 1}_{blockIndex + 1}";

                    // Apply ItemPrefab color if component exists
                    ItemPrefab itemComponent = instance.GetComponent<ItemPrefab>();
                    if (itemComponent != null)
                    {
                        // Override the prefab's default color with the level config color
                        instance.GetComponent<SpriteRenderer>().color = colorGroup.ToUnityColor();
                    }

                    _spawnedItems.Add(new ConveyorBelt.ConveyorBelt.ConveyorBeltItem
                    {
                        item = instance.transform,
                        colorGroup = (ConveyorBelt.ConveyorBelt.ItemColorGroup)(int)colorGroup,
                        startPoint = 1
                    });
                }
            }

            // Assign spawned items to the conveyor belt
            _conveyorBelt.SetRuntimeItems(_spawnedItems);
        }

        private void SpawnBoxes(LevelConfig config)
        {
            if (_boxConveyorBelt == null || _boxPrefab == null)
                return;

            var availableColors = config.GetAvailableBoxColors();
            if (availableColors == null || availableColors.Count == 0)
                return;

            Transform parent = _boxParent != null ? _boxParent : transform;
            _spawnedBoxes = new List<BoxConveyorBelt.ConveyorBox>();

            for (int i = 0; i < availableColors.Count; i++)
            {
                ColorGroup colorGroup = availableColors[i];

                GameObject instance = Instantiate(_boxPrefab, parent);
                instance.name = $"Box_{colorGroup}";

                // Apply BoxPrefab color if component exists
                BoxPrefab boxComponent = instance.GetComponent<BoxPrefab>();
                if (boxComponent != null)
                {
                    instance.GetComponent<SpriteRenderer>().color = colorGroup.ToUnityColor();
                }

                _spawnedBoxes.Add(new BoxConveyorBelt.ConveyorBox
                {
                    box = instance.transform,
                    colorGroup = (BoxConveyorBelt.ItemColorGroup)(int)colorGroup
                });
            }

            // Assign spawned boxes to the box conveyor belt
            _boxConveyorBelt.SetRuntimeBoxes(_spawnedBoxes);
        }

        /// <summary>
        /// Spawn a box from the panel selection using the panel box prefab.
        /// Used by ColorBoxButton when player selects a color.
        /// </summary>
        public GameObject SpawnPanelBox(ColorGroup colorGroup)
        {
            if (_panelBoxPrefab == null)
                return null;

            Transform parent = _boxParent != null ? _boxParent : transform;
            GameObject instance = Instantiate(_panelBoxPrefab, parent);
            instance.name = $"PanelBox_{colorGroup}";

            // Apply BoxPrefab color if the panel box has one
            BoxPrefab boxComponent = instance.GetComponent<BoxPrefab>();
            if (boxComponent != null)
            {
                instance.GetComponent<SpriteRenderer>().color = colorGroup.ToUnityColor();
            }
            else
            {
                // Fallback: apply directly to SpriteRenderer
                colorGroup.ApplyTo(instance);
            }

            return instance;
        }
    }
}