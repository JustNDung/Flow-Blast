using System;
using System.Collections.Generic;
using ConveyorBelt.Services;
using DG.Tweening;
using UnityEngine;

namespace ConveyorBelt
{
    public class BoxConveyorBelt : MonoBehaviour
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
        public class ConveyorBox
        {
            public Transform box;
            public ItemColorGroup colorGroup;

            [HideInInspector] public float distanceAlongPath;
            [HideInInspector] public bool IsAbsorbing;
            [HideInInspector] public int StoredRowCount;
            [HideInInspector] public bool IsCompleting;

            public Transform BoxTransform => box;
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
        [SerializeField] private float fillPerItemRow = 0.05f;
        [SerializeField] private Vector2 storedBlockSpacing = new Vector2(0.08f, 0.08f);
        [SerializeField] private Vector3 storedBlockScale = new Vector3(0.12f, 0.12f, 1f);

        [Header("Panel")]
        [SerializeField] private bool autoCreateSelectionPanel = true;
        [SerializeField] private Vector3 panelCenter = new Vector3(0f, -9.7f, 0f);
        [SerializeField] private Vector2 panelSpacing = new Vector2(1.2f, 0.95f);
        [SerializeField] private ItemColorGroup[] panelColors =
        {
            ItemColorGroup.Red,
            ItemColorGroup.Blue,
            ItemColorGroup.Yellow,
            ItemColorGroup.Purple
        };

        // Services
        private PathSystem _pathSystem;

        private readonly List<Transform> _sceneBoxTemplates = new List<Transform>();
        private int _pendingBoxes;
        private bool _pathDirty = true;

        // Backward-compatible color mapping
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
            // Initialize services
            _pathSystem = new PathSystem();
            if (lineRenderer != null && lineRenderer.positionCount >= 2)
                _pathSystem.RebuildPath(lineRenderer);

            CacheSceneBoxTemplates();

            if (_pathSystem.IsPathDirty)
                _pathSystem.RebuildPath(lineRenderer);

            if (!startWithSceneBoxes)
                ClearSceneBoxes();

            ArrangeBoxes();
        }

        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            boxSpacing = Mathf.Max(0.01f, boxSpacing);
            maxBoxesOnBelt = Mathf.Max(1, maxBoxesOnBelt);
            fillPerItemRow = Mathf.Clamp(fillPerItemRow, 0.01f, 1f);
            storedBlockSpacing.x = Mathf.Max(0.01f, storedBlockSpacing.x);
            storedBlockSpacing.y = Mathf.Max(0.01f, storedBlockSpacing.y);
            _pathSystem?.MarkDirty();
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

            MoveBoxes();
        }

        #region Public API

        public bool TryGetAvailableBox(ItemColorGroup colorGroup, out ConveyorBox matchingBox)
        {
            matchingBox = null;

            if (boxes == null)
                return false;

            for (int i = 0; i < boxes.Count; i++)
            {
                ConveyorBox box = boxes[i];

                if (box == null || box.box == null || box.IsAbsorbing || box.IsCompleting || IsBoxFull(box) || box.colorGroup != colorGroup)
                    continue;

                matchingBox = box;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the first box of a given color group (used by AbsorptionService).
        /// </summary>
        public ConveyorBox GetBoxByGroup(ItemColorGroup colorGroup)
        {
            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] != null && boxes[i].colorGroup == colorGroup && !boxes[i].IsAbsorbing && !boxes[i].IsCompleting && !IsBoxFull(boxes[i]))
                    return boxes[i];
            }
            return null;
        }

        public bool TryAddBoxFromPanel(Transform panelBox, ItemColorGroup colorGroup)
        {
            if (panelBox == null || !CanAcceptMoreBoxes() || _pathSystem.PathLength <= 0.0001f)
                return false;

            _pendingBoxes++;
            PathSample targetSample = _pathSystem.GetPositionAtDistance(GetSpawnDistance());

            ConveyorBox conveyorBox = new ConveyorBox
            {
                box = panelBox,
                colorGroup = colorGroup,
                distanceAlongPath = GetSpawnDistance()
            };

            ApplyColor(conveyorBox);
            panelBox.DOKill();

            // Remove existing components
            RemoveComponentsFromPanelBox(panelBox);

            panelBox.DOJump(targetSample.Position, 1.15f, 1, 0.45f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _pendingBoxes = Mathf.Max(0, _pendingBoxes - 1);

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

        public void StoreRowInBox(ConveyorBox box, IReadOnlyList<Transform> rowBlocks)
        {
            if (box == null || box.box == null || rowBlocks == null || rowBlocks.Count == 0 || box.IsCompleting)
                return;

            int rowSlot = box.StoredRowCount;
            box.StoredRowCount++;

            for (int i = 0; i < rowBlocks.Count; i++)
            {
                Transform block = rowBlocks[i];

                if (block == null)
                    continue;

                block.SetParent(box.box, true);
                block.localRotation = Quaternion.identity;
                block.localPosition = GetStoredRowBlockLocalPosition(rowSlot, i, rowBlocks.Count);
                block.localScale = storedBlockScale;
                block.gameObject.SetActive(true);
            }

            if (IsBoxFull(box))
                CompleteBox(box);
        }

        public int GetRemainingRowCapacity(ConveyorBox box)
        {
            if (box == null)
                return 0;

            return Mathf.Max(0, GetRowCapacity() - box.StoredRowCount);
        }

        public float GetFillPercent(ConveyorBox box)
        {
            if (box == null)
                return 0f;

            return Mathf.Clamp01(box.StoredRowCount * fillPerItemRow);
        }

        #endregion

        #region Box Management

        [ContextMenu("Arrange Boxes")]
        public void ArrangeBoxes()
        {
            if (boxes == null || boxes.Count == 0)
                return;

            if (_pathDirty || _pathSystem.IsPathDirty)
                _pathSystem.RebuildPath(lineRenderer);

            if (_pathSystem.PathLength <= 0.0001f)
                return;

            for (int i = 0; i < boxes.Count; i++)
            {
                ConveyorBox box = boxes[i];

                if (box == null || box.box == null)
                    continue;

                box.distanceAlongPath = _pathSystem.WrapDistance(i * boxSpacing);
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

                if (box == null || box.box == null || box.IsCompleting)
                    continue;

                box.distanceAlongPath = _pathSystem.WrapDistance(box.distanceAlongPath + delta);
                PlaceBox(box);
            }
        }

        private void PlaceBox(ConveyorBox box)
        {
            PathSample sample = _pathSystem.GetPositionAtDistance(box.distanceAlongPath);
            box.box.position = sample.Position;

            if (faceDirection && sample.Direction.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = reverseDirection ? -sample.Direction : sample.Direction;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                box.box.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>
        /// Set the runtime boxes list from an external spawner (LevelSpawner).
        /// Clears existing boxes and replaces them with spawned ones.
        /// </summary>
        public void SetRuntimeBoxes(IReadOnlyList<ConveyorBox> runtimeBoxes)
        {
            // Deactivate any existing scene box templates
            for (int i = 0; i < _sceneBoxTemplates.Count; i++)
            {
                if (_sceneBoxTemplates[i] != null)
                    _sceneBoxTemplates[i].gameObject.SetActive(false);
            }

            boxes = new List<ConveyorBox>(runtimeBoxes);
            _pathDirty = true;

            // Arrange the new boxes on the path
            if (isActiveAndEnabled && Application.isPlaying)
            {
                ArrangeBoxes();
            }
        }

        #endregion

        #region Helpers

        private void CacheSceneBoxTemplates()
        {
            _sceneBoxTemplates.Clear();

            for (int i = 0; i < boxes.Count; i++)
            {
                if (boxes[i] != null && boxes[i].box != null && !_sceneBoxTemplates.Contains(boxes[i].box))
                    _sceneBoxTemplates.Add(boxes[i].box);
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
            return boxes.Count + _pendingBoxes < maxBoxesOnBelt;
        }

        private int GetRowCapacity()
        {
            return Mathf.Max(1, Mathf.CeilToInt(1f / fillPerItemRow));
        }

        private bool IsBoxFull(ConveyorBox box)
        {
            return GetFillPercent(box) >= 1f;
        }

        private float GetSpawnDistance()
        {
            if (boxes.Count == 0)
                return 0f;

            ConveyorBox lastBox = boxes[boxes.Count - 1];
            float direction = reverseDirection ? 1f : -1f;
            return _pathSystem.WrapDistance(lastBox.distanceAlongPath + boxSpacing * direction);
        }

        private Vector3 GetStoredRowBlockLocalPosition(int row, int column, int columnCount)
        {
            int usedColumns = Mathf.Max(1, columnCount);
            float x = (column - (usedColumns - 1) * 0.5f) * storedBlockSpacing.x;
            float y = ((GetRowCapacity() - 1) * 0.5f - row) * storedBlockSpacing.y;
            int slot = row * usedColumns + column;
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

        private void ApplyColor(ConveyorBox box)
        {
            if (!applyBoxColors || box == null || box.box == null)
                return;

            ColorService.TryApplyColor(box.box.gameObject, box.colorGroup);
        }

        private static void RemoveComponentsFromPanelBox(Transform panelBox)
        {
            ColorBoxButton button = panelBox.GetComponent<ColorBoxButton>();
            if (button != null)
                Destroy(button);

            Collider2D collider = panelBox.GetComponent<Collider2D>();
            if (collider != null)
                Destroy(collider);
        }

        #endregion
    }
}