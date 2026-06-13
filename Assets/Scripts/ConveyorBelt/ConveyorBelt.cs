using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        public sealed class ItemGroup
        {
            public ItemColorGroup ColorGroup;
            public readonly List<ConveyorBeltItem> Items = new List<ConveyorBeltItem>();
            public float DistanceAlongPath;
            public bool IsAbsorbing;
        }

        private readonly struct PathSample
        {
            public readonly Vector3 Position;
            public readonly Vector3 Direction;

            public PathSample(Vector3 position, Vector3 direction)
            {
                Position = position;
                Direction = direction;
            }
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
        [SerializeField] private bool generateRuntimeLevelGroups = true;
        [SerializeField] private List<ItemColorGroup> generatedColorSequence = new List<ItemColorGroup>
        {
            ItemColorGroup.Red,
            ItemColorGroup.Yellow,
            ItemColorGroup.Blue,
            ItemColorGroup.Purple,
            ItemColorGroup.Yellow,
            ItemColorGroup.Red,
            ItemColorGroup.Blue
        };

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

        private readonly List<float> segmentLengths = new List<float>();
        private readonly List<float> segmentStartDistances = new List<float>();
        private readonly List<ItemGroup> groups = new List<ItemGroup>();
        private readonly List<Transform> sceneItemTemplates = new List<Transform>();
        private bool runtimeItemsGenerated;
        private float pathLength;
        private bool pathDirty = true;

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
            CacheSceneItemTemplates();
            EnsureRuntimeLevelGroups();
            RebuildPath();
            BuildGroups();
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
            pathDirty = true;
        }

        private void Update()
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return;

            if (pathDirty)
                RebuildPath();

            if (pathLength <= 0.0001f)
                return;

            MoveGroups();
            TryAbsorbAtDoor();
        }

        [ContextMenu("Rebuild Groups")]
        public void BuildGroups()
        {
            groups.Clear();

            if (items == null || items.Count == 0)
                return;

            if (pathDirty)
                RebuildPath();

            if (sortItemsByColor && !runtimeItemsGenerated)
                items = items.OrderBy(item => item == null ? ItemColorGroup.Red : item.colorGroup).ToList();

            int capacity = groupRows * groupColumns;
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
                        cursor -= GetGroupLength(activeGroup.Items.Count) + groupSpacing;

                    activeGroup = new ItemGroup
                    {
                        ColorGroup = beltItem.colorGroup,
                        DistanceAlongPath = WrapDistance(cursor)
                    };
                    activeColor = beltItem.colorGroup;
                    groups.Add(activeGroup);
                }

                activeGroup.Items.Add(beltItem);
                beltItem.distanceAlongPath = activeGroup.DistanceAlongPath;
            }

            PlaceGroups();
        }

        private void MoveGroups()
        {
            float delta = speed * Time.deltaTime;

            for (int i = 0; i < groups.Count; i++)
            {
                ItemGroup group = groups[i];

                group.DistanceAlongPath = WrapDistance(group.DistanceAlongPath + delta);

                for (int j = 0; j < group.Items.Count; j++)
                    group.Items[j].distanceAlongPath = group.DistanceAlongPath;
            }

            PlaceGroups();
        }

        private void PlaceGroups()
        {
            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                ItemGroup group = groups[groupIndex];

                PathSample sample = GetPositionAtDistance(group.DistanceAlongPath);
                Vector3 tangent = sample.Direction.sqrMagnitude > 0.0001f ? sample.Direction.normalized : Vector3.right;
                Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);

                for (int itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
                {
                    ConveyorBeltItem beltItem = group.Items[itemIndex];

                    if (beltItem == null || beltItem.item == null || beltItem.IsAbsorbing || beltItem.IsAbsorbed)
                        continue;

                    beltItem.item.position = sample.Position + GetGridOffset(itemIndex, group.Items.Count, tangent, side);

                    if (faceDirection)
                    {
                        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                        beltItem.item.rotation = Quaternion.Euler(0f, 0f, angle);
                    }
                }
            }
        }

        private void TryAbsorbAtDoor()
        {
            if (door1 == null || door2 == null || boxConveyorBelt == null)
                return;

            float doorMinY = Mathf.Min(door1.position.y, door2.position.y);
            float doorMaxY = Mathf.Max(door1.position.y, door2.position.y);
            float doorCenterX = (door1.position.x + door2.position.x) * 0.5f;

            for (int i = 0; i < groups.Count; i++)
            {
                ItemGroup group = groups[i];

                if (group.IsAbsorbing || group.Items.Count == 0 || GetActiveItemCount(group) == 0)
                    continue;

                bool groupAtDoor = IsGroupAtDoor(group, doorMinY, doorMaxY, doorCenterX);

                // Kiểm tra itemGroup có trong khoảng door1-door2 không
                if (!groupAtDoor)
                    continue;

                // Tìm box cùng màu trong khoảng door
                if (!boxConveyorBelt.TryGetAvailableBox(
                        group.ColorGroup,
                        out BoxConveyorBelt.ConveyorBox box))
                {
                    continue;
                }

                AbsorbGroup(group, box);
                break;
            }
        }

        private bool IsGroupAtDoor(ItemGroup group)
        {
            if (door1 == null || door2 == null || group == null)
                return false;

            float doorMinY = Mathf.Min(door1.position.y, door2.position.y);
            float doorMaxY = Mathf.Max(door1.position.y, door2.position.y);
            float doorCenterX = (door1.position.x + door2.position.x) * 0.5f;

            return IsGroupAtDoor(group, doorMinY, doorMaxY, doorCenterX);
        }

        private bool IsGroupAtDoor(ItemGroup group, float doorMinY, float doorMaxY, float doorCenterX)
        {
            if (group == null)
                return false;

            PathSample sample = GetPositionAtDistance(group.DistanceAlongPath);
            float itemY = sample.Position.y;
            bool itemInsideDoorY = itemY >= doorMinY && itemY <= doorMaxY;
            bool itemAtGateX = Mathf.Abs(sample.Position.x - doorCenterX) <= itemDoorXTolerance;

            return itemInsideDoorY && itemAtGateX;
        }


        private void AbsorbGroup(ItemGroup group, BoxConveyorBelt.ConveyorBox box)
        {
            group.IsAbsorbing = true;
            box.IsAbsorbing = true;

            Sequence sequence = DOTween.Sequence();
            bool absorptionCanceled = false;
            ConveyorBeltItem activeAbsorbingItem = null;
            Transform activeAbsorbingTransform = null;
            Vector3 activeAbsorbingStartScale = Vector3.one;
            int remainingBoxSlots = boxConveyorBelt.GetRemainingCapacity(box);
            List<ConveyorBeltItem> absorbedItems = group.Items
                .Where(item => item != null && item.item != null && !item.IsAbsorbed && !item.IsAbsorbing)
                .Take(remainingBoxSlots)
                .ToList();

            if (absorbedItems.Count == 0)
            {
                group.IsAbsorbing = false;
                box.IsAbsorbing = false;
                return;
            }

            for (int i = 0; i < absorbedItems.Count; i++)
            {
                ConveyorBeltItem beltItem = absorbedItems[i];

                if (beltItem == null || beltItem.item == null)
                    continue;

                Transform itemTransform = beltItem.item;
                itemTransform.DOKill();

                Vector3 localStartPos = default;
                Vector3 localStartScale = default;

                sequence.AppendCallback(() =>
                {
                    if (!IsGroupAtDoor(group))
                    {
                        absorptionCanceled = true;
                        sequence.Kill(false);
                        return;
                    }

                    if (itemTransform != null)
                    {
                        beltItem.IsAbsorbing = true;
                        activeAbsorbingItem = beltItem;
                        activeAbsorbingTransform = itemTransform;
                        localStartPos = itemTransform.position;
                        localStartScale = itemTransform.localScale;
                        activeAbsorbingStartScale = localStartScale;
                    }
                });

                sequence.Append(DOVirtual.Float(0f, 1f, absorbDuration, t =>
                {
                    if (itemTransform == null || box == null || box.BoxTransform == null)
                        return;

                    if (!IsGroupAtDoor(group))
                    {
                        absorptionCanceled = true;
                        sequence.Kill(false);
                        return;
                    }

                    itemTransform.position = Vector3.LerpUnclamped(localStartPos, box.BoxTransform.position, t);
                    itemTransform.localScale = Vector3.LerpUnclamped(localStartScale, Vector3.zero, t);
                }).SetEase(absorbEase));
                sequence.AppendCallback(() =>
                {
                    if (absorptionCanceled)
                        return;

                    beltItem.IsAbsorbing = false;
                    beltItem.IsAbsorbed = true;
                    activeAbsorbingItem = null;
                    activeAbsorbingTransform = null;
                    boxConveyorBelt.StoreBlockInBox(box, itemTransform);

                    if (hideAbsorbedItems)
                        itemTransform.gameObject.SetActive(false);
                });

                if (absorbInterval > 0f)
                    sequence.AppendInterval(absorbInterval);
            }

            sequence.OnComplete(() =>
            {
                groups.RemoveAll(candidate => GetActiveItemCount(candidate) == 0);

                if (!box.IsCompleting)
                    box.IsAbsorbing = false;

                group.IsAbsorbing = false;
            });
            sequence.OnKill(() =>
            {
                if (!absorptionCanceled)
                    return;

                if (activeAbsorbingItem != null)
                    activeAbsorbingItem.IsAbsorbing = false;

                if (activeAbsorbingTransform != null)
                    activeAbsorbingTransform.localScale = activeAbsorbingStartScale;

                if (!box.IsCompleting)
                    box.IsAbsorbing = false;

                group.IsAbsorbing = false;
                PlaceGroups();
            });
        }

        private int GetActiveItemCount(ItemGroup group)
        {
            if (group == null)
                return 0;

            int count = 0;

            for (int i = 0; i < group.Items.Count; i++)
            {
                ConveyorBeltItem item = group.Items[i];

                if (item != null && item.item != null && !item.IsAbsorbed)
                    count++;
            }

            return count;
        }

        private void CacheSceneItemTemplates()
        {
            sceneItemTemplates.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].item != null && !sceneItemTemplates.Contains(items[i].item))
                    sceneItemTemplates.Add(items[i].item);
            }
        }

        private void EnsureRuntimeLevelGroups()
        {
            if (!generateRuntimeLevelGroups || runtimeItemsGenerated || sceneItemTemplates.Count == 0)
                return;

            Transform prototype = sceneItemTemplates[0];

            if (prototype == null)
                return;

            for (int i = 0; i < sceneItemTemplates.Count; i++)
            {
                if (sceneItemTemplates[i] != null)
                    sceneItemTemplates[i].gameObject.SetActive(false);
            }

            items = new List<ConveyorBeltItem>();
            int blocksPerGroup = groupRows * groupColumns;
            Transform runtimeRoot = new GameObject("Runtime Item Groups").transform;
            runtimeRoot.SetParent(transform, true);

            for (int groupIndex = 0; groupIndex < generatedColorSequence.Count; groupIndex++)
            {
                ItemColorGroup colorGroup = generatedColorSequence[groupIndex];

                for (int blockIndex = 0; blockIndex < blocksPerGroup; blockIndex++)
                {
                    Transform clone = Instantiate(prototype, prototype.position, prototype.rotation, runtimeRoot);
                    clone.name = $"Item {colorGroup} {groupIndex + 1}-{blockIndex + 1}";
                    clone.localScale = prototype.localScale;
                    clone.gameObject.SetActive(true);

                    items.Add(new ConveyorBeltItem
                    {
                        item = clone,
                        colorGroup = colorGroup,
                        startPoint = 1
                    });
                }
            }

            runtimeItemsGenerated = true;
        }

        private void RebuildPath()
        {
            segmentLengths.Clear();
            segmentStartDistances.Clear();
            pathLength = 0f;

            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return;

            int segmentCount = lineRenderer.loop ? lineRenderer.positionCount : lineRenderer.positionCount - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 start = GetWorldPosition(i);
                Vector3 end = GetWorldPosition((i + 1) % lineRenderer.positionCount);
                float length = Vector3.Distance(start, end);

                segmentStartDistances.Add(pathLength);
                segmentLengths.Add(length);
                pathLength += length;
            }

            pathDirty = false;
        }

        private PathSample GetPositionAtDistance(float distance)
        {
            distance = WrapDistance(distance);

            for (int i = 0; i < segmentLengths.Count; i++)
            {
                float segmentLength = segmentLengths[i];

                if (segmentLength <= 0.0001f)
                    continue;

                float segmentStartDistance = segmentStartDistances[i];

                if (distance > segmentStartDistance + segmentLength && i < segmentLengths.Count - 1)
                    continue;

                Vector3 start = GetWorldPosition(i);
                Vector3 end = GetWorldPosition((i + 1) % lineRenderer.positionCount);
                Vector3 direction = (end - start).normalized;
                float t = Mathf.Clamp01((distance - segmentStartDistance) / segmentLength);
                return new PathSample(Vector3.LerpUnclamped(start, end, t), direction);
            }

            Vector3 fallbackStart = GetWorldPosition(0);
            Vector3 fallbackEnd = GetWorldPosition(1);
            return new PathSample(fallbackStart, (fallbackEnd - fallbackStart).normalized);
        }

        private Vector3 GetWorldPosition(int index)
        {
            Vector3 position = lineRenderer.GetPosition(index);
            return lineRenderer.useWorldSpace ? position : lineRenderer.transform.TransformPoint(position);
        }

        private Vector3 GetGridOffset(int index, int itemCount, Vector3 tangent, Vector3 side)
        {
            int row = index / groupColumns;
            int column = index % groupColumns;
            int usedRows = Mathf.Min(groupRows, Mathf.CeilToInt(itemCount / (float)groupColumns));
            int usedColumns = Mathf.Min(groupColumns, itemCount);
            float x = (column - (usedColumns - 1) * 0.5f) * groupCellSpacing.x;
            float y = ((usedRows - 1) * 0.5f - row) * groupCellSpacing.y;
            return tangent * x + side * y;
        }

        private float GetGroupLength(int itemCount)
        {
            int usedColumns = Mathf.Min(groupColumns, Mathf.Max(1, itemCount));
            return Mathf.Max(groupCellSpacing.x, (usedColumns - 1) * groupCellSpacing.x + groupSpacing);
        }

        private float GetDistanceFromLegacyFields(ConveyorBeltItem beltItem)
        {
            if (pathLength <= 0.0001f || beltItem == null || segmentStartDistances.Count == 0)
                return 0f;

            int point = Mathf.Clamp(beltItem.startPoint, 0, Mathf.Max(0, segmentStartDistances.Count - 1));
            float segmentLength = segmentLengths.Count > point ? segmentLengths[point] : 0f;
            return WrapDistance(segmentStartDistances[point] + segmentLength * Mathf.Clamp01(beltItem.currentLerp));
        }

        private float WrapDistance(float distance)
        {
            if (pathLength <= 0.0001f)
                return 0f;

            distance %= pathLength;

            if (distance < 0f)
                distance += pathLength;

            return distance;
        }

        private void ApplyColor(ConveyorBeltItem beltItem)
        {
            if (!applyItemColors || beltItem == null || beltItem.item == null)
                return;

            SpriteRenderer spriteRenderer = beltItem.item.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null && GroupColors.TryGetValue(beltItem.colorGroup, out Color color))
                spriteRenderer.color = color;
        }
    }
}
