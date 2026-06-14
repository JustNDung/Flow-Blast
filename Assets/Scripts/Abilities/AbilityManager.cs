using System;
using System.Collections.Generic;
using System.Linq;
using Abilities.Strategies;
using UI;
using UnityEngine;

namespace Abilities
{
    /// <summary>
    /// Central runtime manager for all abilities.
    /// Handles ability registration, count management, and execution.
    /// Attach this to a persistent GameObject or access via singleton Instance.
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        private static AbilityManager _instance;
        public static AbilityManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (Application.isPlaying)
                    {
                        var go = new GameObject("AbilityManager");
                        _instance = go.AddComponent<AbilityManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<AbilityType, AbilityDefinition> _definitions = new();
        private readonly Dictionary<AbilityType, AbilityStrategy> _strategies = new();
        private readonly Dictionary<AbilityType, int> _counts = new();
        private AbilityExecutionContext _executionContext;

        /// <summary>
        /// Fired when any ability count changes.
        /// </summary>
        public event Action<AbilityType, int> OnCountChanged;

        /// <summary>
        /// Fired when an ability is used.
        /// </summary>
        public event Action<AbilityType> OnAbilityUsed;

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

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Initialize the manager with a set of ability definitions.
        /// Call this once at game start (e.g., from a GameManager or bootstrapper).
        /// </summary>
        public void Initialize(IEnumerable<AbilityDefinition> definitions, AbilityExecutionContext context = null)
        {
            _definitions.Clear();
            _strategies.Clear();
            _counts.Clear();
            _executionContext = context ?? new AbilityExecutionContext();

            foreach (var definition in definitions)
            {
                RegisterAbility(definition);
            }
        }

        private void RegisterAbility(AbilityDefinition definition)
        {
            if (definition == null) return;

            var type = definition.AbilityType;
            _definitions[type] = definition;
            _counts[type] = definition.DefaultCount;

            // Register the strategy based on ability type
            _strategies[type] = CreateStrategy(type);

            Debug.Log($"[AbilityManager] Registered ability: {definition.AbilityName} ({type})");
        }

        /// <summary>
        /// Get all registered ability definitions.
        /// </summary>
        public IReadOnlyList<AbilityDefinition> GetAllDefinitions()
        {
            return _definitions.Values.ToList();
        }

        /// <summary>
        /// Get the definition for a specific ability type.
        /// </summary>
        public AbilityDefinition GetDefinition(AbilityType type)
        {
            _definitions.TryGetValue(type, out var definition);
            return definition;
        }

        /// <summary>
        /// Get the current count for an ability.
        /// </summary>
        public int GetCount(AbilityType type)
        {
            return _counts.TryGetValue(type, out var count) ? count : 0;
        }

        /// <summary>
        /// Check if an ability can be used (has at least 1 charge).
        /// </summary>
        public bool CanUseAbility(AbilityType type)
        {
            return _counts.TryGetValue(type, out var count) && count > 0;
        }

        /// <summary>
        /// Try to use an ability. Returns true if the ability was executed.
        /// </summary>
        public bool TryUseAbility(AbilityType type)
        {
            if (!CanUseAbility(type))
            {
                Debug.LogWarning($"[AbilityManager] Cannot use {type}: no charges remaining.");
                return false;
            }

            // Execute the strategy
            if (_strategies.TryGetValue(type, out var strategy))
            {
                try
                {
                    strategy.Execute(_executionContext);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AbilityManager] Error executing {type}: {ex.Message}");
                    strategy.OnExecutionFailed(_executionContext);
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"[AbilityManager] No strategy registered for {type}. Consuming charge anyway.");
            }

            // Consume one charge
            _counts[type] = Mathf.Max(0, _counts[type] - 1);

            // Notify listeners
            OnCountChanged?.Invoke(type, _counts[type]);
            OnAbilityUsed?.Invoke(type);
            MessageDispatcher.MessageDispatcher.Publish(new AbilityUsedMessage(type));

            return true;
        }

        /// <summary>
        /// Add charges to an ability.
        /// </summary>
        public void AddCharges(AbilityType type, int amount = 1)
        {
            if (!_counts.ContainsKey(type))
            {
                Debug.LogWarning($"[AbilityManager] Cannot add charges to unregistered ability: {type}");
                return;
            }

            _counts[type] += amount;
            OnCountChanged?.Invoke(type, _counts[type]);
        }

        /// <summary>
        /// Set the exact charge count for an ability.
        /// </summary>
        public void SetCharges(AbilityType type, int count)
        {
            if (!_counts.ContainsKey(type))
            {
                Debug.LogWarning($"[AbilityManager] Cannot set charges for unregistered ability: {type}");
                return;
            }

            _counts[type] = Mathf.Max(0, count);
            OnCountChanged?.Invoke(type, _counts[type]);
        }

        /// <summary>
        /// Factory method to create strategies by ability type.
        /// Extend this switch when adding new abilities.
        /// </summary>
        private static AbilityStrategy CreateStrategy(AbilityType type)
        {
            return type switch
            {
                AbilityType.Magnet => new MagnetAbility(),
                AbilityType.Hand => new HandAbility(),
                AbilityType.Shuffle => new ShuffleAbility(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, $"No strategy defined for {type}")
            };
        }
    }
}