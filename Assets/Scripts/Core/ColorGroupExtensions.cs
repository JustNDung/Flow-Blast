using Core;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Extension methods for ColorGroup to centralize color mapping.
    /// Single source of truth for color values, replacing duplicate dictionaries.
    /// </summary>
    public static class ColorGroupExtensions
    {
        private static readonly Color[] Colors =
        {
            new Color(1f, 0.18f, 0.15f),    // Red
            new Color(1f, 0.9f, 0.08f),     // Yellow
            new Color(0.1f, 0.55f, 1f),     // Blue
            new Color(0.2f, 0.9f, 0.2f),    // Green
            new Color(0.55f, 0.25f, 1f),    // Purple
            new Color(1f, 0.5f, 0.12f),     // Orange
            new Color(1f, 0.18f, 0.82f),    // Pink
            new Color(0.1f, 0.9f, 0.95f)    // Cyan
        };

        public static Color ToUnityColor(this ColorGroup colorGroup)
        {
            int index = (int)colorGroup;
            return index >= 0 && index < Colors.Length ? Colors[index] : Color.white;
        }

        /// <summary>
        /// Apply the color to a SpriteRenderer component.
        /// </summary>
        public static void ApplyTo(this ColorGroup colorGroup, SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = colorGroup.ToUnityColor();
        }

        /// <summary>
        /// Apply the color to a GameObject's SpriteRenderer.
        /// </summary>
        public static void ApplyTo(this ColorGroup colorGroup, GameObject gameObject)
        {
            if (gameObject != null)
                colorGroup.ApplyTo(gameObject.GetComponent<SpriteRenderer>());
        }
    }
}