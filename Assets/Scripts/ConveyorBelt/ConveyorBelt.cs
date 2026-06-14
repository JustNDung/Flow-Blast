using System;
using System.Collections.Generic;
using System.Linq;
using ConveyorBelt.Services;
using DG.Tweening;
using UnityEngine;

namespace ConveyorBelt
{
    public class ConveyorBelt : MonoBehaviour
    {
        public enum ItemColorGroup
        {
            Red,
            Yellow,
            Blue,
            Green,
            Purple,
            Orange,
            Pink,
            Cyan
        }

        [Serializable]
        public class ConveyorBeltItem
        {
            public Transform item;
            public ItemColorGroup colorGroup;

            [HideInInspector] public float currentLerp;
            [HideInInspector] public int startPoint;
            [HideInInspector] public float distanceAlongPath;
            [HideInInspector] public bool IsAbsorbing;
            [HideInInspector] public bool IsAbsorbed;

            public bool IsAvailableForAbsorption => item != null && !IsAbsorbed && !IsAbsorbing;
            public bool IsActive => item != null && !IsAbsorbed;
        }

        public sealed class ItemGroup
        {
            public ItemColorGroup ColorGroup;
            public readonly List<ConveyorBeltItem> Items = new List<ConveyorBeltItem>();
            public float DistanceAlongPath;
            public bool IsAbsorbing;
        }

        [Header("Path")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float speed = 2f;
        [SerializeField] private bool faceDirection = true;

        [Header("Groups")]
        [SerializeField] private List<ConveyorBeltItem> items = new List<ConveyorBeltItem>();
        [SerializeField] private int groupRows = 4;
        [SerializeField] private int groupColumns = 9;
        [SerializeField] private Vector2 groupCellSpacing = new Vector2(0.55f, 0.55f);
        [SerializeField] private float groupSpacing = 0.6f;
        [SerializeField] private bool sortItemsByColor = true;
        [SerializeField] private bool applyItemColors = true;

        [Header("Door Match")]
        [SerializeField] private Transform door1;
        [SerializeField] private Transform door2;
        [SerializeField] private float itemDoorXTolerance = 0.75f;
        [SerializeField] private BoxConveyorBelt boxConveyorBelt;

        [Header("Absorb Animation")]
        [SerializeField] private float absorbDuration = 0.16f;
        [SerializeField] private float absorbInterval = 0.01f;
        [SerializeField] private Ease absorbEase = Ease.InBack;
        [SerializeField] private bool hideAbsorbedItems = false;

        // Services
        private PathSystem _pathSystem;
        private GridLayoutService _gridLayout;
        private AbsorptionService _absorptionService;

        private readonly List<ItemGroup> _groups = new List<ItemGroup>();
        private readonly List<Transform> _sceneItemTemplates = new List<Transform>();
        private bool _runtimeItemsGenerated;
        private bool _pathDirty = true;

        // Backward-compatible static accessor for external references (e.g., ColorBoxSelectionPanel)
        public static readonly Dictionary<ItemColorGroup, Color> GroupColors = new Dictionary<ItemColorGroup, Color>
        {
            { ItemColorGroup.Red, new Color(1f, 0.18f, 0.15f) },
            { ItemColorGroup.Yellow, new Color(1f, 0.9f, 0.08f) },
            { ItemColorGroup.Blue, new Color(0.1f, 0.55f, 1f) },
            { ItemColorGroup.Green, new Color(0.2f, 0.9f, 0.2f) },
            { ItemColorGroup.Purple, new Color(0.55f, 0.25f, 1f) },
            { ItemColorGroup.Orange, new Color(1f, 0.5f, 0.12f) },
            { ItemColorGroup.Pink, new Color(1f, 0.18f, 0.82f) },
            { ItemColorGroup.Cyan, new Color(0.1f, 0.9f, 0.95f) }
        };

        private void Awake()
        {
            InitializeServices();
            CacheSceneItemTemplates();
            EnsureRuntimeLevelGroups();

            if (_pathDirty)
                _pathSystem.RebuildPath(lineRenderer);

            BuildGroups();
        }

        private void InitializeServices()
        {
            // Initialize PathSystem
            _pathSystem = new PathSystem();
            if (lineRenderer != null && lineRenderer.positionCount >= 2)
                _pathSystem.RebuildPath(lineRenderer);

            // Initialize GridLayoutService
            _gridLayout = new GridLayoutService(groupRows, groupColumns, groupCellSpacing, groupSpacing);

            // Initialize AbsorptionService
            _absorptionService = new AbsorptionService();
            _absorptionService.Initialize(door1, door2, itemDoorXTolerance, _pathSystem);

            _pathDirty = _pathSystem.IsPathDirty;
        }

        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            groupRows = Mathf.Max(1, groupRows);
            groupColumns = Mathf.Max(1, groupColumns);
            groupCellSpacing.x = Mathf.Max(0.01f, groupCellSpacing.x);
            groupCellSpacing.y = Mathf.Max(0.01f, groupCellSpacing.y);
            groupSpacing = Mathf.Max(0f, groupSpacing);
            itemDoorXTolerance = Mathf.Max(0.01f, itemDoorXTolerance);
            absorbDuration = Mathf.Max(0.01f, absorbDuration);
            absorbInterval = Mathf.Max(0f, absorbInterval);
            _pathDirty = true;
        }

        private void Update()
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return;

            if (_pathDirty || _pathSystem.IsPathDirty)
            {
                _pathSystem.RebuildPath(lineRenderer);
                _pathDirty = false;
            }

            if (_pathSystem.PathLength <= 0.0001f)
                return;

            MoveGroups();
            TryAbsorbAtDoor();
        }

        #region Group Management

        [ContextMenu("Rebuild Groups")]
        public void BuildGroups()
        {
            _groups.Clear();

            if (items == null || items.Count == 0)
                return;

            if (_pathDirty || _pathSystem.IsPathDirty)
            {
                _pathSystem.RebuildPath(lineRenderer);
                _pathDirty = false;
            }

            // Sort items by color if applicable (only for scene-placed items, not runtime generated)
            if (sortItemsByColor && !_runtimeItemsGenerated)
                items = items.OrderBy(item => item == null ? ItemColorGroup.Red : item.colorGroup).ToList();

            int capacity = _gridLayout.Capacity;
            float cursor = GetDistanceFromLegacyFields(items[0]);
            ItemColorGroup? activeColor = null;
            ItemGroup activeGroup = null;

            for (int i = 0; i < items.Count; i++)
            {
                ConveyorBeltItem beltItem = items[i];

                if (beltItem == null || beltItem.item == null)
                    continue;

                beltItem.IsAbsorbing = false;
                beltItem.IsAbsorbed = false;
                ApplyColor(beltItem);

                bool needsNewGroup =
                    activeGroup == null ||
                    activeColor != beltItem.colorGroup ||
                    activeGroup.Items.Count >= capacity;

                if (needsNewGroup)
                {
                    if (activeGroup != null)
                        cursor -= _gridLayout.GetGroupLength(activeGroup.Items.Count) + groupSpacing;

                    activeGroup = new ItemGroup
                    {
                        ColorGroup = beltItem.colorGroup,
                        DistanceAlongPath = _pathSystem.WrapDistance(cursor)
                    };
                    activeColor = beltItem.colorGroup;
                    _groups.Add(activeGroup);
                }

                activeGroup.Items.Add(beltItem);
                beltItem.distanceAlongPath = activeGroup.DistanceAlongPath;
            }

            PlaceGroups();
        }

        private void MoveGroups()
        {
            float delta = speed * Time.deltaTime;

            for (int i = 0; i < _groups.Count; i++)
            {
                ItemGroup group = _groups[i];

                group.DistanceAlongPath = _pathSystem.WrapDistance(group.DistanceAlongPath + delta);

                for (int j = 0; j < group.Items.Count; j++)
                    group.Items[j].distanceAlongPath = group.DistanceAlongPath;
            }

            PlaceGroups();
        }

        private void PlaceGroups()
        {
            for (int groupIndex = 0; groupIndex < _groups.Count; groupIndex++)
            {
                ItemGroup group = _groups[groupIndex];

                PathSample sample = _pathSystem.GetPositionAtDistance(group.DistanceAlongPath);
                Vector3 tangent = sample.Direction.sqrMagnitude > 0.0001f ? sample.Direction.normalized : Vector3.right;
                Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);

                for (int itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
                {
                    ConveyorBeltItem beltItem = group.Items[itemIndex];

                    if (beltItem == null || beltItem.item == null || beltItem.IsAbsorbing || beltItem.IsAbsorbed)
                        continue;

                    beltItem.item.position = sample.Position + _gridLayout.GetGridOffset(itemIndex, group.Items.Count, tangent, side);

                    if (faceDirection)
                    {
                        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                        beltItem.item.rotation = Quaternion.Euler(0f, 0f, angle);
                    }
                }
            }
        }

        #endregion

        #region Absorption

        private void TryAbsorbAtDoor()
        {
            if (door1 == null || door2 == null || boxConveyorBelt == null)
                return;

            for (int i = 0; i < _groups.Count; i++)
            {
                ItemGroup group = _groups[i];

                if (group.IsAbsorbing || group.Items.Count == 0)
                    continue;

                int activeCount = AbsorptionService.GetActiveItemCount(group.Items, item => item.IsActive);
                if (activeCount == 0)
                    continue;

                if (!_absorptionService.IsPositionAtDoor(group.DistanceAlongPath))
                    continue;

                // Find matching color box
                if (!boxConveyorBelt.TryGetAvailableBox(
                        (BoxConveyorBelt.ItemColorGroup)(int)group.ColorGroup,
                        out BoxConveyorBelt.ConveyorBox box))
                {
                    continue;
                }

                AbsorbGroup(group, box);
                break;
            }
        }

        private void AbsorbGroup(ItemGroup group, BoxConveyorBelt.ConveyorBox box)
        {
            group.IsAbsorbing = true;
            box.IsAbsorbing = true;

            Sequence sequence = DOTween.Sequence();
            bool absorptionCanceled = false;
            List<ConveyorBeltItem> activeAbsorbingItems = new List<ConveyorBeltItem>();
            List<Transform> activeAbsorbingTransforms = new List<Transform>();
            List<Vector3> activeAbsorbingStartScales = new List<Vector3>();
            int remainingBoxRows = boxConveyorBelt.GetRemainingRowCapacity(box);
            List<List<ConveyorBeltItem>> absorbedRows = AbsorptionService.GetAbsorbableRows(
                group.Items,
                groupColumns,
                remainingBoxRows,
                item => item.IsAvailableForAbsorption);

            if (absorbedRows.Count == 0)
            {
                group.IsAbsorbing = false;
                box.IsAbsorbing = false;
                return;
            }

            for (int rowIndex = 0; rowIndex < absorbedRows.Count; rowIndex++)
            {
                List<ConveyorBeltItem> rowItems = absorbedRows[rowIndex];

                if (rowItems == null || rowItems.Count == 0)
                    continue;

                List<Transform> rowTransforms = rowItems
                    .Where(item => item != null && item.item != null)
                    .Select(item => item.item)
                    .ToList();
                List<Vector3> rowStartPositions = new List<Vector3>();
                List<Vector3> rowStartScales = new List<Vector3>();

                sequence.AppendCallback(() =>
                {
                    if (!_absorptionService.IsPositionAtDoor(group.DistanceAlongPath))
                    {
                        absorptionCanceled = true;
                        sequence.Kill(false);
                        return;
                    }

                    activeAbsorbingItems.Clear();
                    activeAbsorbingTransforms.Clear();
                    activeAbsorbingStartScales.Clear();
                    rowStartPositions.Clear();
                    rowStartScales.Clear();

                    for (int i = 0; i < rowItems.Count; i++)
                    {
                        ConveyorBeltItem rowItem = rowItems[i];

                        if (rowItem == null || rowItem.item == null)
                            continue;

                        Transform rowTransform = rowItem.item;
                        rowTransform.DOKill();
                        rowItem.IsAbsorbing = true;

                        activeAbsorbingItems.Add(rowItem);
                        activeAbsorbingTransforms.Add(rowTransform);
                        activeAbsorbingStartScales.Add(rowTransform.localScale);
                        rowStartPositions.Add(rowTransform.position);
                        rowStartScales.Add(rowTransform.localScale);
                    }
                });

                sequence.Append(DOVirtual.Float(0f, 1f, absorbDuration, t =>
                {
                    if (box == null || box.BoxTransform == null)
                        return;

                    if (!_absorptionService.IsPositionAtDoor(group.DistanceAlongPath))
                    {
                        absorptionCanceled = true;
                        sequence.Kill(false);
                        return;
                    }

                    for (int i = 0; i < activeAbsorbingTransforms.Count; i++)
                    {
                        Transform itemTransform = activeAbsorbingTransforms[i];

                        if (itemTransform == null)
                            continue;

                        itemTransform.position = Vector3.LerpUnclamped(rowStartPositions[i], box.BoxTransform.position, t);
                        itemTransform.localScale = Vector3.LerpUnclamped(rowStartScales[i], Vector3.zero, t);
                    }
                }).SetEase(absorbEase));

                sequence.AppendCallback(() =>
                {
                    if (absorptionCanceled)
                        return;

                    for (int i = 0; i < rowItems.Count; i++)
                    {
                        ConveyorBeltItem rowItem = rowItems[i];

                        if (rowItem == null || rowItem.item == null)
                            continue;

                        rowItem.IsAbsorbing = false;
                        rowItem.IsAbsorbed = true;
                    }

                    activeAbsorbingItems.Clear();
                    activeAbsorbingTransforms.Clear();
                    activeAbsorbingStartScales.Clear();
                    boxConveyorBelt.StoreRowInBox(box, rowTransforms);

                    if (hideAbsorbedItems)
                    {
                        for (int i = 0; i < rowTransforms.Count; i++)
                        {
                            if (rowTransforms[i] != null)
                                rowTransforms[i].gameObject.SetActive(false);
                        }
                    }
                });

                if (absorbInterval > 0f)
                    sequence.AppendInterval(absorbInterval);
            }

            sequence.OnComplete(() =>
            {
                _groups.RemoveAll(candidate =>
                    AbsorptionService.GetActiveItemCount(candidate.Items, item => item.IsActive) == 0);

                if (!box.IsCompleting)
                    box.IsAbsorbing = false;

                group.IsAbsorbing = false;
            });

            sequence.OnKill(() =>
            {
                if (!absorptionCanceled)
                    return;

                for (int i = 0; i < activeAbsorbingItems.Count; i++)
                {
                    if (activeAbsorbingItems[i] != null)
                        activeAbsorbingItems[i].IsAbsorbing = false;
                }

                for (int i = 0; i < activeAbsorbingTransforms.Count; i++)
                {
                    if (activeAbsorbingTransforms[i] != null && i < activeAbsorbingStartScales.Count)
                        activeAbsorbingTransforms[i].localScale = activeAbsorbingStartScales[i];
                }

                if (!box.IsCompleting)
                    box.IsAbsorbing = false;

                group.IsAbsorbing = false;
                PlaceGroups();
            });
        }

        #endregion

        #region Public API (for LevelSpawner)

        /// <summary>
        /// Number of blocks per group (grid capacity). Used by LevelSpawner.
        /// </summary>
        public int GroupCapacity => _gridLayout != null ? _gridLayout.Capacity : groupRows * groupColumns;

        /// <summary>
        /// Set the runtime items list from an external spawner (LevelSpawner).
        /// Clears any existing items and replaces them with the spawned ones.
        /// </summary>
        public void SetRuntimeItems(IReadOnlyList<ConveyorBeltItem> runtimeItems)
        {
            // Deactivate any existing scene templates
            for (int i = 0; i < _sceneItemTemplates.Count; i++)
            {
                if (_sceneItemTemplates[i] != null)
                    _sceneItemTemplates[i].gameObject.SetActive(false);
            }

            items = new List<ConveyorBeltItem>(runtimeItems);
            _runtimeItemsGenerated = true;
            _pathDirty = true;

            // Rebuild groups with new items
            if (isActiveAndEnabled && Application.isPlaying)
            {
                BuildGroups();
            }
        }

        #endregion

        #region Item Generation

        private void CacheSceneItemTemplates()
        {
            _sceneItemTemplates.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].item != null && !_sceneItemTemplates.Contains(items[i].item))
                    _sceneItemTemplates.Add(items[i].item);
            }
        }

        private void EnsureRuntimeLevelGroups()
        {
            // Delegate runtime generation to LevelSpawner.
            // This legacy method is kept as a no-op for backward compatibility.
            // LevelSpawner calls SetRuntimeItems() to populate the belt.
            if (_sceneItemTemplates.Count > 0)
            {
                // Deactivate scene templates since they're just placeholders
                for (int i = 0; i < _sceneItemTemplates.Count; i++)
                {
                    if (_sceneItemTemplates[i] != null)
                        _sceneItemTemplates[i].gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Helpers

        private float GetDistanceFromLegacyFields(ConveyorBeltItem beltItem)
        {
            if (_pathSystem.PathLength <= 0.0001f || beltItem == null)
                return 0f;

            return _pathSystem.WrapDistance(0f);
        }

        private void ApplyColor(ConveyorBeltItem beltItem)
        {
            if (!applyItemColors || beltItem == null || beltItem.item == null)
                return;

            ColorService.TryApplyColor(beltItem.item.gameObject, beltItem.colorGroup);
        }

        #endregion
    }
}