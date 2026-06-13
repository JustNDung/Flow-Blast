using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConveyorBelt
{
    public class BoxConveyorBelt : MonoBehaviour
    {
        [Serializable]
        public class ConveyorBox
        {
            public Transform box;
            public ConveyorBelt.ItemColorGroup colorGroup;

            [HideInInspector] public float distanceAlongPath;
            [HideInInspector] public bool IsAbsorbing;

            public Transform BoxTransform => box;
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
        [SerializeField] private bool reverseDirection = true;
        [SerializeField] private bool faceDirection = true;

        [Header("Boxes")]
        [SerializeField] private List<ConveyorBox> boxes = new List<ConveyorBox>();
        [SerializeField] private float boxSpacing = 2f;
        [SerializeField] private bool applyBoxColors = true;

        private readonly List<float> segmentLengths = new List<float>();
        private readonly List<float> segmentStartDistances = new List<float>();
        private float pathLength;
        private bool pathDirty = true;

        private void Awake()
        {
            RebuildPath();
            ArrangeBoxes();
        }

        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            boxSpacing = Mathf.Max(0.01f, boxSpacing);
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

            MoveBoxes();
        }

        public bool TryGetAlignedBox(
            ConveyorBelt.ItemColorGroup colorGroup,
            Vector3 doorPosition,
            float alignmentTolerance,
            out ConveyorBox matchingBox)
        {
            matchingBox = null;

            if (boxes == null || pathLength <= 0.0001f)
                return false;

            alignmentTolerance = Mathf.Max(0.01f, alignmentTolerance);

            for (int i = 0; i < boxes.Count; i++)
            {
                ConveyorBox box = boxes[i];

                if (box == null || box.box == null || box.IsAbsorbing || box.colorGroup != colorGroup)
                    continue;

                PathSample sample = GetPositionAtDistance(box.distanceAlongPath);
                bool sharesDoorX = Mathf.Abs(sample.Position.x - doorPosition.x) <= alignmentTolerance;
                bool sharesDoorY = Mathf.Abs(sample.Position.y - doorPosition.y) <= alignmentTolerance;

                if (!sharesDoorX && !sharesDoorY)
                    continue;

                matchingBox = box;
                return true;
            }

            return false;
        }

        [ContextMenu("Arrange Boxes")]
        public void ArrangeBoxes()
        {
            if (boxes == null || boxes.Count == 0)
                return;

            if (pathDirty)
                RebuildPath();

            if (pathLength <= 0.0001f)
                return;

            for (int i = 0; i < boxes.Count; i++)
            {
                ConveyorBox box = boxes[i];

                if (box == null || box.box == null)
                    continue;

                box.distanceAlongPath = WrapDistance(i * boxSpacing);
                ApplyColor(box);
                PlaceBox(box);
            }
        }

        private void MoveBoxes()
        {
            float direction = reverseDirection ? -1f : 1f;
            float delta = speed * direction * Time.deltaTime;

            for (int i = 0; i < boxes.Count; i++)
            {
                ConveyorBox box = boxes[i];

                if (box == null || box.box == null || box.IsAbsorbing)
                    continue;

                box.distanceAlongPath = WrapDistance(box.distanceAlongPath + delta);
                PlaceBox(box);
            }
        }

        private void PlaceBox(ConveyorBox box)
        {
            PathSample sample = GetPositionAtDistance(box.distanceAlongPath);
            box.box.position = sample.Position;

            if (faceDirection && sample.Direction.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = reverseDirection ? -sample.Direction : sample.Direction;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                box.box.rotation = Quaternion.Euler(0f, 0f, angle);
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
        
        public bool TryGetAlignedBoxInDoorRange(
            ConveyorBelt.ItemColorGroup colorGroup,
            float doorMinX,
            float doorMaxX,
            float alignmentTolerance,
            out ConveyorBox matchingBox)
        {
            matchingBox = null;
        
            if (boxes == null || pathLength <= 0.0001f)
                return false;
        
            alignmentTolerance = Mathf.Max(0.01f, alignmentTolerance);
        
            for (int i = 0; i < boxes.Count; i++)
            {
                ConveyorBox box = boxes[i];
        
                if (box == null || box.box == null || box.IsAbsorbing || box.colorGroup != colorGroup)
                    continue;
        
                PathSample sample = GetPositionAtDistance(box.distanceAlongPath);
                float boxX = sample.Position.y;
        
                // Kiểm tra box có trong khoảng door và căn chỉnh Y
                bool inDoorRange = boxX >= doorMinX - alignmentTolerance && boxX <= doorMaxX + alignmentTolerance;
        
                if (!inDoorRange)
                    continue;
        
                matchingBox = box;
                return true;
            }
        
            return false;
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

        private float WrapDistance(float distance)
        {
            if (pathLength <= 0.0001f)
                return 0f;

            distance %= pathLength;

            if (distance < 0f)
                distance += pathLength;

            return distance;
        }

        private void ApplyColor(ConveyorBox box)
        {
            if (!applyBoxColors || box == null || box.box == null)
                return;

            SpriteRenderer spriteRenderer = box.box.GetComponent<SpriteRenderer>();

            if (spriteRenderer != null && ConveyorBelt.GroupColors.TryGetValue(box.colorGroup, out Color color))
                spriteRenderer.color = color;
        }
    }
}
