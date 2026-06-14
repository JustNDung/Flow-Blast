using ConveyorBelt.Services;
using DG.Tweening;
using UnityEngine;

namespace ConveyorBelt
{
    public class ColorBoxSelectionPanel : MonoBehaviour
    {
        private static Sprite _squareSprite;

        private BoxConveyorBelt _boxConveyorBelt;
        private Vector2 _buttonSpacing;
        private BoxConveyorBelt.ItemColorGroup[] _colors;

        public static ColorBoxSelectionPanel Create(
            BoxConveyorBelt targetBelt,
            Vector3 center,
            Vector2 spacing,
            BoxConveyorBelt.ItemColorGroup[] colorOptions)
        {
            GameObject panelObject = new GameObject("Color Box Selection Panel");
            panelObject.transform.position = center;

            ColorBoxSelectionPanel panel = panelObject.AddComponent<ColorBoxSelectionPanel>();
            panel._boxConveyorBelt = targetBelt;
            panel._buttonSpacing = spacing;
            panel._colors = colorOptions;
            panel.Build();

            return panel;
        }

        private void Build()
        {
            if (_boxConveyorBelt == null || _colors == null || _colors.Length == 0)
                return;

            SpriteRenderer background = CreateSprite("Panel Background", transform, new Color(0.16f, 0.2f, 0.55f, 0.9f), 0);
            background.transform.localScale = new Vector3(4.6f, 2.1f, 1f);
            background.sortingOrder = -2;

            int columns = Mathf.Min(3, _colors.Length);
            int rows = Mathf.CeilToInt(_colors.Length / (float)columns);

            for (int i = 0; i < _colors.Length; i++)
            {
                int row = i / columns;
                int column = i % columns;
                float x = (column - (columns - 1) * 0.5f) * _buttonSpacing.x;
                float y = ((rows - 1) * 0.5f - row) * _buttonSpacing.y;

                GameObject buttonObject = new GameObject($"Panel Box {_colors[i]}");
                buttonObject.transform.SetParent(transform, false);
                buttonObject.transform.localPosition = new Vector3(x, y, -0.1f);
                buttonObject.transform.localScale = new Vector3(0.72f, 0.52f, 1f);

                SpriteRenderer renderer = buttonObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSquareSprite();
                ColorService.TryApplyColor(renderer, _colors[i]);
                renderer.sortingOrder = 2;

                BoxCollider2D collider = buttonObject.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;

                ColorBoxButton button = buttonObject.AddComponent<ColorBoxButton>();
                button.Initialize(_boxConveyorBelt, _colors[i]);
            }
        }

        private static SpriteRenderer CreateSprite(string name, Transform parent, Color color, int sortingOrder)
        {
            GameObject spriteObject = new GameObject(name);
            spriteObject.transform.SetParent(parent, false);

            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            return renderer;
        }

        private static Sprite GetSquareSprite()
        {
            if (_squareSprite != null)
                return _squareSprite;

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

            return _squareSprite;
        }
    }

    public class ColorBoxButton : MonoBehaviour
    {
        private BoxConveyorBelt _boxConveyorBelt;
        private BoxConveyorBelt.ItemColorGroup _colorGroup;
        private Collider2D _buttonCollider;
        private bool _isLaunching;

        public void Initialize(BoxConveyorBelt targetBelt, BoxConveyorBelt.ItemColorGroup color)
        {
            _boxConveyorBelt = targetBelt;
            _colorGroup = color;
        }

        private void Awake()
        {
            _buttonCollider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (!InputService.WasPressedThisFrame(out Vector2 screenPosition))
                return;

            Vector2? worldPositionNullable = InputService.ScreenToWorldPoint(screenPosition);

            if (worldPositionNullable == null || _buttonCollider == null)
                return;

            Vector2 worldPosition = worldPositionNullable.Value;

            if (!_buttonCollider.OverlapPoint(worldPosition))
                return;

            LaunchBox();
        }

        private void LaunchBox()
        {
            if (_isLaunching || _boxConveyorBelt == null)
                return;

            _isLaunching = true;

            Transform launchedBox = Instantiate(transform, transform.position, Quaternion.identity);
            launchedBox.name = $"Selected Box {_colorGroup}";
            launchedBox.localScale = transform.lossyScale;
            launchedBox.DOPunchScale(Vector3.one * 0.12f, 0.18f, 1, 0.5f);

            bool accepted = _boxConveyorBelt.TryAddBoxFromPanel(launchedBox, _colorGroup);

            if (!accepted)
                Destroy(launchedBox.gameObject);

            DOVirtual.DelayedCall(0.2f, () => _isLaunching = false);
        }
    }
}