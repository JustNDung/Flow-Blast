using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Generic object pool that reuses GameObjects to avoid Instantiate/Destroy overhead.
    /// Supports multiple pools indexed by a string key (e.g., "Item_Red", "Box_Blue").
    /// Pools are registered dynamically by LevelSpawner — no Inspector setup needed.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        // Key -> queue of available (idle) instances
        private readonly Dictionary<string, Queue<GameObject>> _availablePools = new();
        // Key -> prefab for creating new instances
        private readonly Dictionary<string, GameObject> _prefabMap = new();
        // Key -> list of active (in-use) instances for cleanup
        private readonly Dictionary<string, List<GameObject>> _activeInstances = new();

        /// <summary>
        /// Register a new pool with a key and prefab.
        /// Optionally prewarm with idle objects.
        /// </summary>
        public void RegisterPool(string key, GameObject prefab, int prewarmCount = 0)
        {
            if (_prefabMap.ContainsKey(key))
            {
                Debug.LogWarning($"[ObjectPool] Pool '{key}' already registered. Skipping.");
                return;
            }

            _prefabMap[key] = prefab;
            _availablePools[key] = new Queue<GameObject>();
            _activeInstances[key] = new List<GameObject>();

            for (int j = 0; j < prewarmCount; j++)
            {
                GameObject obj = CreateNewObject(key, prefab);
                obj.SetActive(false);
                _availablePools[key].Enqueue(obj);
            }

            if (prewarmCount > 0)
            {
                Debug.Log($"[ObjectPool] Registered pool '{key}' with {prewarmCount} prewarmed objects.");
            }
        }

        /// <summary>
        /// Get an object from the pool. Returns null if pool doesn't exist.
        /// The object is automatically activated.
        /// </summary>
        public GameObject Get(string key)
        {
            if (!_availablePools.TryGetValue(key, out Queue<GameObject> pool))
            {
                Debug.LogError($"[ObjectPool] No pool found for key '{key}'. Call RegisterPool() first.");
                return null;
            }

            // Try to get an idle object from the queue
            while (pool.Count > 0)
            {
                GameObject candidate = pool.Dequeue();
                if (candidate != null)
                {
                    // Found a valid idle object — activate it
                    candidate.SetActive(true);

                    if (_activeInstances.TryGetValue(key, out List<GameObject> activeList))
                    {
                        activeList.Add(candidate);
                    }

                    return candidate;
                }
                // candidate was destroyed externally — skip to next
            }

            // Pool exhausted — create a new instance on demand
            if (!_prefabMap.TryGetValue(key, out GameObject prefab))
            {
                Debug.LogError($"[ObjectPool] No prefab registered for key '{key}'.");
                return null;
            }

            GameObject newObj = CreateNewObject(key, prefab);
            newObj.SetActive(true);

            if (_activeInstances.TryGetValue(key, out List<GameObject> newActiveList))
            {
                newActiveList.Add(newObj);
            }

            return newObj;
        }

        /// <summary>
        /// Return an object to the pool. The object is deactivated.
        /// </summary>
        public void Return(string key, GameObject obj)
        {
            if (obj == null) return;

            if (!_availablePools.TryGetValue(key, out Queue<GameObject> pool))
            {
                Debug.LogWarning($"[ObjectPool] No pool found for key '{key}'. Destroying object.");
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(transform, false);

            if (_activeInstances.TryGetValue(key, out List<GameObject> activeList))
            {
                activeList.Remove(obj);
            }

            pool.Enqueue(obj);
        }

        /// <summary>
        /// Return all active objects in a specific pool back to available.
        /// </summary>
        public void ReturnAll(string key)
        {
            if (!_activeInstances.TryGetValue(key, out List<GameObject> activeList))
                return;

            for (int i = activeList.Count - 1; i >= 0; i--)
            {
                if (activeList[i] != null)
                    Return(key, activeList[i]);
                else
                    activeList.RemoveAt(i);
            }

            activeList.Clear();
        }

        /// <summary>
        /// Return ALL active objects across all pools back to available.
        /// </summary>
        public void ReturnAllPools()
        {
            var keys = new List<string>(_activeInstances.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                ReturnAll(keys[i]);
            }
        }

        /// <summary>
        /// Destroy all objects in a specific pool (both active and available).
        /// </summary>
        public void ClearPool(string key)
        {
            ReturnAll(key);

            if (_availablePools.TryGetValue(key, out Queue<GameObject> pool))
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    if (obj != null)
                        Destroy(obj);
                }
            }
        }

        /// <summary>
        /// Destroy all objects across all pools.
        /// </summary>
        public void ClearAllPools()
        {
            var keys = new List<string>(_availablePools.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                ClearPool(keys[i]);
            }
        }

        /// <summary>
        /// Get the number of available (idle) objects in a pool.
        /// </summary>
        public int GetAvailableCount(string key)
        {
            return _availablePools.TryGetValue(key, out Queue<GameObject> pool) ? pool.Count : 0;
        }

        /// <summary>
        /// Get the number of active (in-use) objects in a pool.
        /// </summary>
        public int GetActiveCount(string key)
        {
            return _activeInstances.TryGetValue(key, out List<GameObject> list) ? list.Count : 0;
        }

        private GameObject CreateNewObject(string key, GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, transform, false);
            obj.name = $"{prefab.name} (Pooled)";
            return obj;
        }

        private void OnDestroy()
        {
            _availablePools.Clear();
            _activeInstances.Clear();
            _prefabMap.Clear();
        }
    }
}