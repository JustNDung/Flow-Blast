using System;
using System.Collections.Generic;
using DG.Tweening;
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
            [HideInInspector] public int StoredBlockCount;
            [HideInInspector] public bool IsCompleting;

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
        [SerializeField] private bool startWithSceneBoxes = false;
        [SerializeField] private int maxBoxesOnBelt = 5;

        [Header("Box Capacity")]
        [SerializeField] private int boxRows = 4;
        [SerializeField] private int boxColumns = 9;
        [SerializeField] private Vector2 storedBlockSpacing = new Vector2(0.08f, 0.08f);
        [SerializeField] private Vector3 storedBlockScale = new Vector3(0.12f, 0.12f, 1f);

        [Header("Panel")]
        [SerializeField] private bool autoCreateSelectionPanel = true;
        [SerializeField] private Vector3 panelCenter = new Vector3(0f, -4.25f, 0f);
        [SerializeField] private Vector2 panelSpacing = new Vector2(1.2f, 0.95f);
        [SerializeField] private ConveyorBelt.ItemColorGroup[] panelColors =
        {
            ConveyorBelt.ItemColorGroup.Red,
            ConveyorBelt.ItemColorGroup.Blue,
            ConveyorBelt.ItemColorGroup.Yellow,
            ConveyorBelt.ItemColorGroup.Purple,
            ConveyorBelt.ItemColorGroup.Yellow,
            ConveyorBelt.ItemColorGroup.Red
        };

        private readonly List<float> segmentLengths = new List<float>();
        private readonly List<float> segmentStartDistances = new List<float>();
        private readonly List<Transform> sceneBoxTemplates = new List<Transform>();
        private int pendingBoxes;
        private float pathLength;
        private bool pathDirty = true;

        private void Awake()
        {
            CacheSceneBoxTemplates();
            RebuildPath();

            if (!startWithSceneBoxes)
                ClearSceneBoxes();

            ArrangeBoxes();
        }

        private void Start()
        {
            if (autoCreateSelectionPanel && FindFirstObjectByType<ColorBoxSelectionPanel>() == null)
                ColorBoxSelectionPanel.Create(this, panelCenter, panelSpacing, panelColors);
        }

        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            boxSpacing = Mathf.Max(0.01f, boxSpacing);
            maxBoxesOnBelt = Mathf.Max(1, maxBoxesOnBelt);
            boxRows = Mathf.Max(1, boxRows);
            boxColumns = Mathf.Max(1, boxColumns);
            storedBlockSpacing.x = Mathf.Max(0.01f, storedBlockSpacing.x);
            storedBlockSpacing.y = Mathf.Max(0.01f, storedBlockSpacing.y);
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

                if (box == null || box.box == null || box.IsAbsorbing || box.IsCompleting || IsBoxFull(box) || box.colorGroup != colorGroup)
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

        public bool TryAddBoxFromPanel(Transform panelBox, ConveyorBelt.ItemColorGroup colorGroup)
        {
            if (panelBox == null || !CanAcceptMoreBoxes() || pathLength <= 0.0001f)
                return false;

            pendingBoxes++;
            ConveyorBox conveyorBox = new ConveyorBox
            {
                box = panelBox,
                colorGroup = colorGroup,
                distanceAlongPath = GetSpawnDistance()
            };

            ApplyColor(conveyorBox);
            panelBox.DOKill();

            ColorBoxButton button = panelBox.GetComponent<ColorBoxButton>();
            if (button != null)
                Destroy(button);

            Collider2D collider = panelBox.GetComponent<Collider2D>();
            if (collider != null)
                Destroy(collider);

            PathSample targetSample = GetPositionAtDistance(conveyorBox.distanceAlongPath);
            panelBox.DOJump(targetSample.Position, 1.15f, 1, 0.45f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    pendingBoxes = Mathf.Max(0, pendingBoxes - 1);

                    if (panelBox == null || boxes.Count >= maxBoxesOnBelt)
                    {
                        if (panelBox != null)
                            Destroy(panelBox.gameObject);

                        return;
                    }

                    boxes.Add(conveyorBox);
                    PlaceBox(conveyorBox);
                });

            panelBox.DORotate(new Vector3(0f, 0f, 360f), 0.45f, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad);

            return true;
        }

        public void StoreBlockInBox(ConveyorBox box, Transform block)
        {
            if (box == null || box.box == null || block == null || box.IsCompleting)
                return;

            int slot = Mathf.Clamp(box.StoredBlockCount, 0, GetBoxCapacity() - 1);
            box.StoredBlockCount++;

            block.SetParent(box.box, true);
            block.localRotation = Quaternion.identity;
            block.localPosition = GetStoredBlockLocalPosition(slot);
            block.localScale = storedBlockScale;
            block.gameObject.SetActive(true);

            if (IsBoxFull(box))
                CompleteBox(box);
        }

        public int GetRemainingCapacity(ConveyorBox box)
        {
            if (box == null)
                return 0;

            return Mathf.Max(0, GetBoxCapacity() - box.StoredBlockCount);
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

                if (box == null || box.box == null || box.IsAbsorbing || box.IsCompleting)
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
        
                if (box == null || box.box == null || box.IsAbsorbing || box.IsCompleting || IsBoxFull(box) || box.colorGroup != colorGroup)
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

        private void CacheSceneBoxTemplates()
        {
            sceneBoxTemplates.Clear();

            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] != null && boxes[i].box != null && !sceneBoxTemplates.Contains(boxes[i].box))
                    sceneBoxTemplates.Add(boxes[i].box);
            }
        }

        private void ClearSceneBoxes()
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] != null && boxes[i].box != null)
                    boxes[i].box.gameObject.SetActive(false);
            }

            boxes.Clear();
        }

        private bool CanAcceptMoreBoxes()
        {
            return boxes.Count + pendingBoxes < maxBoxesOnBelt;
        }

        private int GetBoxCapacity()
        {
            return boxRows * boxColumns;
        }

        private bool IsBoxFull(ConveyorBox box)
        {
            return box != null && box.StoredBlockCount >= GetBoxCapacity();
        }

        private float GetSpawnDistance()
        {
            if (boxes.Count == 0)
                return 0f;

            ConveyorBox lastBox = boxes[boxes.Count - 1];
            float direction = reverseDirection ? 1f : -1f;
            return WrapDistance(lastBox.distanceAlongPath + boxSpacing * direction);
        }

        private Vector3 GetStoredBlockLocalPosition(int slot)
        {
            int row = slot / boxColumns;
            int column = slot % boxColumns;
            float x = (column - (boxColumns - 1) * 0.5f) * storedBlockSpacing.x;
            float y = ((boxRows - 1) * 0.5f - row) * storedBlockSpacing.y;
            return new Vector3(x, y, -0.01f - slot * 0.001f);
        }

        private void CompleteBox(ConveyorBox box)
        {
            box.IsCompleting = true;
            boxes.Remove(box);

            Transform boxTransform = box.box;
            boxTransform.DOKill();

            Sequence sequence = DOTween.Sequence();
            sequence.Join(boxTransform.DOMove(boxTransform.position + Vector3.up * 2.2f, 0.65f).SetEase(Ease.InBack));
            sequence.Join(boxTransform.DORotate(new Vector3(0f, 0f, 360f), 0.65f, RotateMode.FastBeyond360));
            sequence.Join(boxTransform.DOScale(Vector3.zero, 0.65f).SetEase(Ease.InBack));
            sequence.OnComplete(() =>
            {
                if (boxTransform != null)
                    Destroy(boxTransform.gameObject);
            });
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
