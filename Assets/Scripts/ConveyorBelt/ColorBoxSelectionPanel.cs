using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ConveyorBelt
{
    public class ColorBoxSelectionPanel : MonoBehaviour
    {
        private static Sprite squareSprite;

        private BoxConveyorBelt boxConveyorBelt;
        private Vector2 buttonSpacing;
        private ConveyorBelt.ItemColorGroup[] colors;

        public static ColorBoxSelectionPanel Create(
            BoxConveyorBelt targetBelt,
            Vector3 center,
            Vector2 spacing,
            ConveyorBelt.ItemColorGroup[] colorOptions)
        {
            GameObject panelObject = new GameObject("Color Box Selection Panel");
            panelObject.transform.position = center;

            ColorBoxSelectionPanel panel = panelObject.AddComponent<ColorBoxSelectionPanel>();
            panel.boxConveyorBelt = targetBelt;
            panel.buttonSpacing = spacing;
            panel.colors = colorOptions;
            panel.Build();

            return panel;
        }

        private void Build()
        {
            if (boxConveyorBelt == null || colors == null || colors.Length == 0)
                return;

            SpriteRenderer background = CreateSprite("Panel Background", transform, new Color(0.16f, 0.2f, 0.55f, 0.9f), 0);
            background.transform.localScale = new Vector3(4.6f, 2.1f, 1f);
            background.sortingOrder = -2;

            int columns = Mathf.Min(3, colors.Length);
            int rows = Mathf.CeilToInt(colors.Length / (float)columns);

            for (int i = 0; i < colors.Length; i++)
            {
                int row = i / columns;
                int column = i % columns;
                float x = (column - (columns - 1) * 0.5f) * buttonSpacing.x;
                float y = ((rows - 1) * 0.5f - row) * buttonSpacing.y;

                GameObject buttonObject = new GameObject($"Panel Box {colors[i]}");
                buttonObject.transform.SetParent(transform, false);
                buttonObject.transform.localPosition = new Vector3(x, y, -0.1f);
                buttonObject.transform.localScale = new Vector3(0.72f, 0.52f, 1f);

                SpriteRenderer renderer = buttonObject.AddComponent<SpriteRenderer>();
                renderer.sprite = GetSquareSprite();
                renderer.color = ConveyorBelt.GroupColors[colors[i]];
                renderer.sortingOrder = 2;

                BoxCollider2D collider = buttonObject.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;

                ColorBoxButton button = buttonObject.AddComponent<ColorBoxButton>();
                button.Initialize(boxConveyorBelt, colors[i]);
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
            if (squareSprite != null)
                return squareSprite;

            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

            return squareSprite;
        }
    }

    public class ColorBoxButton : MonoBehaviour
    {
        private BoxConveyorBelt boxConveyorBelt;
        private ConveyorBelt.ItemColorGroup colorGroup;
        private Collider2D buttonCollider;
        private bool isLaunching;

        public void Initialize(BoxConveyorBelt targetBelt, ConveyorBelt.ItemColorGroup color)
        {
            boxConveyorBelt = targetBelt;
            colorGroup = color;
        }

        private void Awake()
        {
            buttonCollider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            if (!WasPressedThisFrame(out Vector2 screenPosition))
                return;

            Camera mainCamera = Camera.main;

            if (mainCamera == null || buttonCollider == null)
                return;

            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);

            if (!buttonCollider.OverlapPoint(worldPosition))
                return;

            LaunchBox();
        }

        private void LaunchBox()
        {
            if (isLaunching || boxConveyorBelt == null)
                return;

            isLaunching = true;

            Transform launchedBox = Instantiate(transform, transform.position, Quaternion.identity);
            launchedBox.name = $"Selected Box {colorGroup}";
            launchedBox.localScale = transform.lossyScale;
            launchedBox.DOPunchScale(Vector3.one * 0.12f, 0.18f, 1, 0.5f);

            bool accepted = boxConveyorBelt.TryAddBoxFromPanel(launchedBox, colorGroup);

            if (!accepted)
                Destroy(launchedBox.gameObject);

            DOVirtual.DelayedCall(0.2f, () => isLaunching = false);
        }

        private static bool WasPressedThisFrame(out Vector2 screenPosition)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null)
            {
                TouchControl touch = Touchscreen.current.primaryTouch;

                if (touch.press.wasPressedThisFrame)
                {
                    screenPosition = touch.position.ReadValue();
                    return true;
                }
            }

            screenPosition = default;
            return false;
        }
    }
}
