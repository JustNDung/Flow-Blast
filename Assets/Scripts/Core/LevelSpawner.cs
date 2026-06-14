using System.Collections.Generic;
using ConveyorBelt;
using Game;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles runtime spawning and cleanup of conveyor belt items and boxes based on LevelConfig.
    /// Uses ObjectPool for optimal performance - no Instantiate/Destroy overhead.
    /// All 8 colored prefabs are referenced directly for maximum flexibility.
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

        [Header("Prefab References (Box)")]
        [SerializeField] private GameObject _redBoxPrefab;
        [SerializeField] private GameObject _yellowBoxPrefab;
        [SerializeField] private GameObject _blueBoxPrefab;
        [SerializeField] private GameObject _greenBoxPrefab;
        [SerializeField] private GameObject _purpleBoxPrefab;
        [SerializeField] private GameObject _orangeBoxPrefab;
        [SerializeField] private GameObject _pinkBoxPrefab;
        [SerializeField] private GameObject _cyanBoxPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _prewarmCountPerColor = 10;

        [Header("Runtime Parent Transforms")]
        [SerializeField] private Transform _itemParent;
        [SerializeField] private Transform _boxParent;

        // Object Pool
        private ObjectPool _objectPool;

        // Conveyor Belt references (found via Awake)
        private ConveyorBelt.ConveyorBelt _conveyorBelt;
        private BoxConveyorBelt _boxConveyorBelt;

        // Track spawned items for the conveyor belt system
        private List<ConveyorBelt.ConveyorBelt.ConveyorBeltItem> _currentItems;
        private List<BoxConveyorBelt.ConveyorBox> _currentBoxes;

        // Color-to-prefab mapping for easy lookup
        private Dictionary<ColorGroup, GameObject> _itemPrefabs = new();
        private Dictionary<ColorGroup, GameObject> _boxPrefabs = new();

        // Pool keys
        private const string ITEM_POOL_PREFIX = "Item_";
        private const string BOX_POOL_PREFIX = "Box_";

        public IReadOnlyList<ConveyorBelt.ConveyorBelt.ConveyorBeltItem> SpawnedItems => _currentItems;
        public IReadOnlyList<BoxConveyorBelt.ConveyorBox> SpawnedBoxes => _currentBoxes;

        private void Awake()
        {
            _conveyorBelt = FindFirstObjectByType<ConveyorBelt.ConveyorBelt>();
            _boxConveyorBelt = FindFirstObjectByType<BoxConveyorBelt>();
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
            // Item prefabs
            _itemPrefabs[ColorGroup.Red] = _redItemPrefab;
            _itemPrefabs[ColorGroup.Yellow] = _yellowItemPrefab;
            _itemPrefabs[ColorGroup.Blue] = _blueItemPrefab;
            _itemPrefabs[ColorGroup.Green] = _greenItemPrefab;
            _itemPrefabs[ColorGroup.Purple] = _purpleItemPrefab;
            _itemPrefabs[ColorGroup.Orange] = _orangeItemPrefab;
            _itemPrefabs[ColorGroup.Pink] = _pinkItemPrefab;
            _itemPrefabs[ColorGroup.Cyan] = _cyanItemPrefab;

            // Box prefabs
            _boxPrefabs[ColorGroup.Red] = _redBoxPrefab;
            _boxPrefabs[ColorGroup.Yellow] = _yellowBoxPrefab;
            _boxPrefabs[ColorGroup.Blue] = _blueBoxPrefab;
            _boxPrefabs[ColorGroup.Green] = _greenBoxPrefab;
            _boxPrefabs[ColorGroup.Purple] = _purpleBoxPrefab;
            _boxPrefabs[ColorGroup.Orange] = _orangeBoxPrefab;
            _boxPrefabs[ColorGroup.Pink] = _pinkBoxPrefab;
            _boxPrefabs[ColorGroup.Cyan] = _cyanBoxPrefab;
        }

        private void RegisterPools()
        {
            if (_objectPool == null) return;

            Transform itemParent = _itemParent != null ? _itemParent : transform;
            Transform boxParent = _boxParent != null ? _boxParent : transform;

            // Register all 8 item pools
            foreach (var kvp in _itemPrefabs)
            {
                if (kvp.Value != null)
                {
                    _objectPool.RegisterPool(GetItemPoolKey(kvp.Key), kvp.Value, _prewarmCountPerColor);
                }
            }

            // Register all 8 box pools
            foreach (var kvp in _boxPrefabs)
            {
                if (kvp.Value != null)
                {
                    _objectPool.RegisterPool(GetBoxPoolKey(kvp.Key), kvp.Value, _prewarmCountPerColor);
                }
            }
        }

        /// <summary>
        /// Spawn items and boxes for the given level configuration using object pooling.
        /// Called by GameManager during level initialization.
        /// </summary>
        public void SpawnLevel(LevelConfig config)
        {
            ClearSpawned();
            SpawnItems(config);
            SpawnBoxes(config);
        }

        /// <summary>
        /// Return all spawned objects to their respective pools.
        /// </summary>
        public void ClearSpawned()
        {
            // Return items to pool
            if (_currentItems != null)
            {
                for (int i = 0; i < _currentItems.Count; i++)
                {
                    if (_currentItems[i] != null && _currentItems[i].item != null)
                    {
                        ReturnItemToPool(_currentItems[i].item.gameObject);
                    }
                }
                _currentItems.Clear();
            }

            // Return boxes to pool
            if (_currentBoxes != null)
            {
                for (int i = 0; i < _currentBoxes.Count; i++)
                {
                    if (_currentBoxes[i] != null && _currentBoxes[i].box != null)
                    {
                        ReturnBoxToPool(_currentBoxes[i].box.gameObject);
                    }
                }
                _currentBoxes.Clear();
            }
        }

        /// <summary>
        /// Fully clear all pools and destroy all pooled objects.
        /// Use this only when resetting the entire game, not between levels.
        /// </summary>
        public void ClearAllPools()
        {
            _objectPool?.ClearAllPools();
            _currentItems?.Clear();
            _currentBoxes?.Clear();
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
                    // Get from pool or create
                    GameObject instance = GetItemFromPool(colorGroup);
                    if (instance == null)
                    {
                        Debug.LogError($"[LevelSpawner] Failed to spawn item for color {colorGroup}.");
                        continue;
                    }

                    instance.name = $"Item_{colorGroup}_{groupIndex + 1}_{blockIndex + 1}";

                    // Ensure correct color is applied (in case pool returns a differently-colored object)
                    SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.color = colorGroup.ToUnityColor();
                    }

                    _currentItems.Add(new ConveyorBelt.ConveyorBelt.ConveyorBeltItem
                    {
                        item = instance.transform,
                        colorGroup = (ConveyorBelt.ConveyorBelt.ItemColorGroup)(int)colorGroup,
                        startPoint = 1,
                        IsAbsorbing = false,
                        IsAbsorbed = false
                    });
                }
            }

            Debug.Log($"[LevelSpawner] Spawned {_currentItems.Count} items using object pools.");
            _conveyorBelt.SetRuntimeItems(_currentItems);
        }

        private void SpawnBoxes(LevelConfig config)
        {
            if (_boxConveyorBelt == null)
            {
                Debug.LogWarning("[LevelSpawner] No BoxConveyorBelt found. Cannot spawn boxes.");
                return;
            }

            var availableColors = config.GetAvailableBoxColors();
            if (availableColors == null || availableColors.Count == 0)
                return;

            _currentBoxes = new List<BoxConveyorBelt.ConveyorBox>();

            for (int i = 0; i < availableColors.Count; i++)
            {
                ColorGroup colorGroup = availableColors[i];

                // Get from pool or create
                GameObject instance = GetBoxFromPool(colorGroup);
                if (instance == null)
                {
                    Debug.LogError($"[LevelSpawner] Failed to spawn box for color {colorGroup}.");
                    continue;
                }

                instance.name = $"Box_{colorGroup}_{i + 1}";

                // Ensure correct color
                SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = colorGroup.ToUnityColor();
                }

                _currentBoxes.Add(new BoxConveyorBelt.ConveyorBox
                {
                    box = instance.transform,
                    colorGroup = (BoxConveyorBelt.ItemColorGroup)(int)colorGroup
                });
            }

            Debug.Log($"[LevelSpawner] Spawned {_currentBoxes.Count} boxes using object pools.");
            _boxConveyorBelt.SetRuntimeBoxes(_currentBoxes);
        }

        /// <summary>
        /// Spawn a box from the panel selection (when player clicks a color button).
        /// Uses the box object pool for reuse.
        /// </summary>
        public GameObject SpawnPanelBox(ColorGroup colorGroup)
        {
            GameObject instance = GetBoxFromPool(colorGroup);
            if (instance == null) return null;

            instance.name = $"PanelBox_{colorGroup}";

            // Ensure correct color
            SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = colorGroup.ToUnityColor();
            }

            return instance;
        }

        #region Pool Helpers

        private static string GetItemPoolKey(ColorGroup color) => $"{ITEM_POOL_PREFIX}{color}";
        private static string GetBoxPoolKey(ColorGroup color) => $"{BOX_POOL_PREFIX}{color}";

        private GameObject GetItemFromPool(ColorGroup color)
        {
            if (_objectPool != null)
            {
                GameObject pooled = _objectPool.Get(GetItemPoolKey(color));
                if (pooled != null)
                {
                    // Parent to item parent for clean hierarchy
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

        private GameObject GetBoxFromPool(ColorGroup color)
        {
            if (_objectPool != null)
            {
                GameObject pooled = _objectPool.Get(GetBoxPoolKey(color));
                if (pooled != null)
                {
                    if (_boxParent != null)
                        pooled.transform.SetParent(_boxParent, false);
                    return pooled;
                }
            }

            // Fallback: instantiate directly if no pool
            if (_boxPrefabs.TryGetValue(color, out GameObject prefab) && prefab != null)
            {
                Transform parent = _boxParent != null ? _boxParent : transform;
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
                // Try to figure out which pool this object belongs to by its color
                foreach (var kvp in _itemPrefabs)
                {
                    if (obj.name.Contains(kvp.Key.ToString()))
                    {
                        _objectPool.Return(GetItemPoolKey(kvp.Key), obj);
                        return;
                    }
                }
                // Fallback: return to first pool (will be deactivated)
                _objectPool.Return(GetItemPoolKey(ColorGroup.Red), obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        private void ReturnBoxToPool(GameObject obj)
        {
            if (_objectPool != null)
            {
                foreach (var kvp in _boxPrefabs)
                {
                    if (obj.name.Contains(kvp.Key.ToString()))
                    {
                        _objectPool.Return(GetBoxPoolKey(kvp.Key), obj);
                        return;
                    }
                }
                _objectPool.Return(GetBoxPoolKey(ColorGroup.Red), obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        #endregion
    }
}