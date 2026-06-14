using System.Collections.Generic;
using ConveyorBelt;
using Game;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles runtime spawning and cleanup of conveyor belt items based on LevelConfig.
    /// Uses ObjectPool for optimal performance — no Instantiate/Destroy overhead.
    /// 
    /// Boxes are NOT spawned here — they are player-driven via ColorBoxSelectionPanel.
    /// Only items on the conveyor belt are spawned from the color sequence in LevelConfig.
    /// </summary>
    public class LevelSpawner : MonoBehaviour
    {
        [Header("Prefab References (Item)")]
        [SerializeField] private GameObject _redItemPrefab;
        [SerializeField] private GameObject _yellowItemPrefab;
        [SerializeField] private GameObject _blueItemPrefab;
        [SerializeField] private GameObject _greenItemPrefab;
        [SerializeField] private GameObject _purpleItemPrefab;
        [SerializeField] private GameObject _orangeItemPrefab;
        [SerializeField] private GameObject _pinkItemPrefab;
        [SerializeField] private GameObject _cyanItemPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _prewarmCountPerColor = 10;

        [Header("Runtime Parent")]
        [SerializeField] private Transform _itemParent;

        // Object Pool
        private ObjectPool _objectPool;

        // Conveyor Belt reference (found via Awake)
        private ConveyorBelt.ConveyorBelt _conveyorBelt;

        // Track spawned items for the conveyor belt system
        private List<ConveyorBelt.ConveyorBelt.ConveyorBeltItem> _currentItems;

        // Color-to-prefab mapping for easy lookup
        private Dictionary<ColorGroup, GameObject> _itemPrefabs = new();

        // Pool key prefix
        private const string ITEM_POOL_PREFIX = "Item_";

        public IReadOnlyList<ConveyorBelt.ConveyorBelt.ConveyorBeltItem> SpawnedItems => _currentItems;

        private void Awake()
        {
            _conveyorBelt = FindFirstObjectByType<ConveyorBelt.ConveyorBelt>();
            _objectPool = FindFirstObjectByType<ObjectPool>();

            if (_objectPool == null)
            {
                Debug.LogError("[LevelSpawner] No ObjectPool found in scene! Pooling disabled.");
            }

            BuildPrefabMappings();
            RegisterPools();
        }

        private void BuildPrefabMappings()
        {
            _itemPrefabs[ColorGroup.Red] = _redItemPrefab;
            _itemPrefabs[ColorGroup.Yellow] = _yellowItemPrefab;
            _itemPrefabs[ColorGroup.Blue] = _blueItemPrefab;
            _itemPrefabs[ColorGroup.Green] = _greenItemPrefab;
            _itemPrefabs[ColorGroup.Purple] = _purpleItemPrefab;
            _itemPrefabs[ColorGroup.Orange] = _orangeItemPrefab;
            _itemPrefabs[ColorGroup.Pink] = _pinkItemPrefab;
            _itemPrefabs[ColorGroup.Cyan] = _cyanItemPrefab;
        }

        private void RegisterPools()
        {
            if (_objectPool == null) return;

            // Register 8 item pools (one per color)
            foreach (var kvp in _itemPrefabs)
            {
                if (kvp.Value != null)
                {
                    _objectPool.RegisterPool(GetItemPoolKey(kvp.Key), kvp.Value, _prewarmCountPerColor);
                }
            }
        }

        /// <summary>
        /// Spawn items for the given level configuration using object pooling.
        /// Called by GameManager during level initialization.
        /// Boxes are not spawned here — they are player-driven via the panel buttons.
        /// </summary>
        public void SpawnLevel(LevelConfig config)
        {
            ClearSpawned();
            SpawnItems(config);
        }

        /// <summary>
        /// Return all spawned items to their respective pools.
        /// </summary>
        public void ClearSpawned()
        {
            if (_currentItems == null) return;

            for (int i = 0; i < _currentItems.Count; i++)
            {
                if (_currentItems[i] != null && _currentItems[i].item != null)
                {
                    ReturnItemToPool(_currentItems[i].item.gameObject);
                }
            }
            _currentItems.Clear();
        }

        /// <summary>
        /// Fully clear all pools and destroy all pooled objects.
        /// Use this only when resetting the entire game, not between levels.
        /// </summary>
        public void ClearAllPools()
        {
            _objectPool?.ClearAllPools();
            _currentItems?.Clear();
        }

        private void SpawnItems(LevelConfig config)
        {
            if (_conveyorBelt == null)
            {
                Debug.LogWarning("[LevelSpawner] No ConveyorBelt found. Cannot spawn items.");
                return;
            }

            var colorSequence = config.GetColorSequence();
            if (colorSequence == null || colorSequence.Count == 0)
                return;

            _currentItems = new List<ConveyorBelt.ConveyorBelt.ConveyorBeltItem>();

            for (int groupIndex = 0; groupIndex < colorSequence.Count; groupIndex++)
            {
                ColorGroup colorGroup = colorSequence[groupIndex];
                int blocksPerGroup = _conveyorBelt.GroupCapacity;

                for (int blockIndex = 0; blockIndex < blocksPerGroup; blockIndex++)
                {
                    // Get from pool or create new
                    GameObject instance = GetItemFromPool(colorGroup);
                    if (instance == null)
                    {
                        Debug.LogError($"[LevelSpawner] Failed to spawn item for color {colorGroup}.");
                        continue;
                    }

                    instance.name = $"Item_{colorGroup}_{groupIndex + 1}_{blockIndex + 1}";

                    // Apply correct color using ColorGroupExtensions
                    SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.color = colorGroup.ToUnityColor();
                    }

                    // Use integer cast to convert ColorGroup to nested ItemColorGroup
                    var itemColorGroup = (ConveyorBelt.ConveyorBelt.ItemColorGroup)(int)colorGroup;

                    _currentItems.Add(new ConveyorBelt.ConveyorBelt.ConveyorBeltItem
                    {
                        item = instance.transform,
                        colorGroup = itemColorGroup,
                        startPoint = 1,
                        IsAbsorbing = false,
                        IsAbsorbed = false
                    });
                }
            }

            Debug.Log($"[LevelSpawner] Spawned {_currentItems.Count} items using object pools.");
            _conveyorBelt.SetRuntimeItems(_currentItems);
        }

        #region Pool Helpers

        private static string GetItemPoolKey(ColorGroup color) => $"{ITEM_POOL_PREFIX}{color}";

        private GameObject GetItemFromPool(ColorGroup color)
        {
            if (_objectPool != null)
            {
                GameObject pooled = _objectPool.Get(GetItemPoolKey(color));
                if (pooled != null)
                {
                    if (_itemParent != null)
                        pooled.transform.SetParent(_itemParent, false);
                    return pooled;
                }
            }

            // Fallback: instantiate directly if no pool
            if (_itemPrefabs.TryGetValue(color, out GameObject prefab) && prefab != null)
            {
                Transform parent = _itemParent != null ? _itemParent : transform;
                GameObject instance = Instantiate(prefab, parent);
                instance.name = $"{prefab.name} (Fallback)";
                return instance;
            }

            return null;
        }

        private void ReturnItemToPool(GameObject obj)
        {
            if (_objectPool != null)
            {
                foreach (var kvp in _itemPrefabs)
                {
                    if (obj.name.Contains(kvp.Key.ToString()))
                    {
                        _objectPool.Return(GetItemPoolKey(kvp.Key), obj);
                        return;
                    }
                }
                _objectPool.Return(GetItemPoolKey(ColorGroup.Red), obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        #endregion
    }
}