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
        [SerializeField] private int groupRows = 2;
        [SerializeField] private int groupColumns = 2;
        [SerializeField] private Vector2 groupCellSpacing = new Vector2(0.55f, 0.55f);
        [SerializeField] private float groupSpacing = 0.6f;
        [SerializeField] private bool sortItemsByColor = true;
        [SerializeField] private bool applyItemColors = true;

        [Header("Door Match")]
        [SerializeField] private Transform itemDoor;
        [SerializeField] private float itemDoorRadius = 0.75f;
        [SerializeField] private BoxConveyorBelt boxConveyorBelt;
        [SerializeField] private Transform boxDoor;

        [Header("Absorb Animation")]
        [SerializeField] private float absorbDuration = 0.45f;
        [SerializeField] private Ease absorbEase = Ease.InBack;
        [SerializeField] private bool hideAbsorbedItems = true;

        private readonly List<float> segmentLengths = new List<float>();
        private readonly List<float> segmentStartDistances = new List<float>();
        private readonly List<ItemGroup> groups = new List<ItemGroup>();
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
            itemDoorRadius = Mathf.Max(0.01f, itemDoorRadius);
            absorbDuration = Mathf.Max(0.01f, absorbDuration);
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

            if (sortItemsByColor)
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

                if (group.IsAbsorbing)
                    continue;

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

                if (group.IsAbsorbing)
                    continue;

                PathSample sample = GetPositionAtDistance(group.DistanceAlongPath);
                Vector3 tangent = sample.Direction.sqrMagnitude > 0.0001f ? sample.Direction.normalized : Vector3.right;
                Vector3 side = new Vector3(-tangent.y, tangent.x, 0f);

                for (int itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
                {
                    ConveyorBeltItem beltItem = group.Items[itemIndex];

                    if (beltItem == null || beltItem.item == null)
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
            if (itemDoor == null || boxDoor == null || boxConveyorBelt == null)
                return;

            for (int i = 0; i < groups.Count; i++)
            {
                ItemGroup group = groups[i];

                if (group.IsAbsorbing || group.Items.Count == 0)
                    continue;

                PathSample sample = GetPositionAtDistance(group.DistanceAlongPath);

                if (Vector3.Distance(sample.Position, itemDoor.position) > itemDoorRadius)
                    continue;

                if (!boxConveyorBelt.TryGetMatchingBoxAtDoor(group.ColorGroup, boxDoor, out BoxConveyorBelt.ConveyorBox box))
                    continue;

                AbsorbGroup(group, box);
                break;
            }
        }

        private void AbsorbGroup(ItemGroup group, BoxConveyorBelt.ConveyorBox box)
        {
            group.IsAbsorbing = true;
            box.IsAbsorbing = true;

            Sequence sequence = DOTween.Sequence();
            List<ConveyorBeltItem> absorbedItems = new List<ConveyorBeltItem>(group.Items);

            for (int i = 0; i < absorbedItems.Count; i++)
            {
                ConveyorBeltItem beltItem = absorbedItems[i];

                if (beltItem == null || beltItem.item == null)
                    continue;

                Transform itemTransform = beltItem.item;
                itemTransform.DOKill();

                sequence.Join(itemTransform.DOMove(box.BoxTransform.position, absorbDuration).SetEase(absorbEase));
                sequence.Join(itemTransform.DOScale(Vector3.zero, absorbDuration).SetEase(absorbEase));
            }

            sequence.OnComplete(() =>
            {
                for (int i = 0; i < absorbedItems.Count; i++)
                {
                    ConveyorBeltItem beltItem = absorbedItems[i];

                    if (beltItem == null || beltItem.item == null)
                        continue;

                    beltItem.item.SetParent(box.BoxTransform, true);

                    if (hideAbsorbedItems)
                        beltItem.item.gameObject.SetActive(false);
                }

                group.Items.RemoveAll(item => absorbedItems.Contains(item));
                groups.RemoveAll(candidate => candidate.Items.Count == 0);
                box.IsAbsorbing = false;
            });
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
