using System.Collections.Generic;
using System.Linq;
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

        [System.Serializable]
        public class ConveyorBeltItem
        {
            public Transform item;
            public ItemColorGroup colorGroup;

            [HideInInspector]
            public float currentLerp;

            [HideInInspector]
            public int startPoint = 0;

            [HideInInspector]
            public float distanceAlongPath;
        }

        [SerializeField] private float itemSpacing = 1f;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float groupSpacing = 0.15f;
        [SerializeField] private bool sortItemsByColor = true;
        [SerializeField] private bool applyItemColors = true;
        [SerializeField] private bool faceDirection = true;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private List<ConveyorBeltItem> items;

        private readonly List<float> segmentLengths = new List<float>();
        private readonly List<float> segmentStartDistances = new List<float>();
        private float pathLength;
        private bool pathDirty = true;

        private static readonly Dictionary<ItemColorGroup, Color> GroupColors = new Dictionary<ItemColorGroup, Color>
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
            ArrangeItemsIntoColorGroups();
        }

        private void OnValidate()
        {
            itemSpacing = Mathf.Max(0.01f, itemSpacing);
            groupSpacing = Mathf.Max(0f, groupSpacing);
            pathDirty = true;
        }

        private void Update()
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return;

            if (items == null || items.Count == 0)
                return;

            if (pathDirty)
                RebuildPath();

            if (pathLength <= 0.0001f)
                return;

            MoveItems();
        }

        [ContextMenu("Arrange Items Into Color Groups")]
        private void ArrangeItemsIntoColorGroups()
        {
            if (items == null || items.Count == 0)
                return;

            if (pathDirty)
                RebuildPath();

            if (sortItemsByColor)
                items = items.OrderBy(item => item?.colorGroup ?? ItemColorGroup.Red).ToList();

            float cursor = GetDistanceFromLegacyFields(items[0]);

            for (int i = 0; i < items.Count; i++)
            {
                ConveyorBeltItem beltItem = items[i];

                if (beltItem == null)
                    continue;

                if (i > 0)
                {
                    cursor -= itemSpacing;

                    ConveyorBeltItem previousItem = items[i - 1];

                    if (previousItem != null && previousItem.colorGroup != beltItem.colorGroup)
                        cursor -= groupSpacing;
                }

                beltItem.distanceAlongPath = WrapDistance(cursor);
                ApplyColor(beltItem);
                PlaceItem(beltItem);
            }
        }

        private void MoveItems()
        {
            for (int i = 0; i < items.Count; i++)
            {
                ConveyorBeltItem beltItem = items[i];

                if (beltItem == null || beltItem.item == null)
                    continue;

                beltItem.distanceAlongPath = WrapDistance(beltItem.distanceAlongPath + speed * Time.deltaTime);
                PlaceItem(beltItem);
            }
        }

        private void PlaceItem(ConveyorBeltItem beltItem)
        {
            if (beltItem == null || beltItem.item == null || pathLength <= 0.0001f)
                return;

            Vector3 position = GetPositionAtDistance(beltItem.distanceAlongPath, out Vector3 direction);
            beltItem.item.position = position;

            if (faceDirection && direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                beltItem.item.rotation = Quaternion.Euler(0f, 0f, angle);
            }
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

        private Vector3 GetPositionAtDistance(float distance, out Vector3 direction)
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
                direction = (end - start).normalized;

                float t = Mathf.Clamp01((distance - segmentStartDistance) / segmentLength);
                return Vector3.LerpUnclamped(start, end, t);
            }

            Vector3 fallbackStart = GetWorldPosition(0);
            Vector3 fallbackEnd = GetWorldPosition(1);
            direction = (fallbackEnd - fallbackStart).normalized;
            return fallbackStart;
        }

        private Vector3 GetWorldPosition(int index)
        {
            Vector3 position = lineRenderer.GetPosition(index);
            return lineRenderer.useWorldSpace ? position : lineRenderer.transform.TransformPoint(position);
        }

        private float GetDistanceFromLegacyFields(ConveyorBeltItem beltItem)
        {
            if (pathLength <= 0.0001f || beltItem == null)
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
