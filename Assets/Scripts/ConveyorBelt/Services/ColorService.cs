using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConveyorBelt.Services
{
    /// <summary>
    /// Centralizes color mapping logic for conveyor belt items.
    /// Works with any enum-based color group through unified color definitions.
    /// </summary>
    public static class ColorService
    {
        // Internal color definitions using integers (matches both ConveyorBelt.ItemColorGroup and BoxConveyorBelt.ItemColorGroup values)
        private static readonly Dictionary<int, Color> ColorMap = new Dictionary<int, Color>
        {
            { 0, new Color(1f, 0.18f, 0.15f) },     // Red
            { 1, new Color(1f, 0.9f, 0.08f) },       // Yellow
            { 2, new Color(0.1f, 0.55f, 1f) },       // Blue
            { 3, new Color(0.2f, 0.9f, 0.2f) },      // Green
            { 4, new Color(0.55f, 0.25f, 1f) },      // Purple
            { 5, new Color(1f, 0.5f, 0.12f) },       // Orange
            { 6, new Color(1f, 0.18f, 0.82f) },      // Pink
            { 7, new Color(0.1f, 0.9f, 0.95f) }      // Cyan
        };

        /// <summary>
        /// Applies the color for a given enum-based color group to a GameObject's SpriteRenderer.
        /// </summary>
        public static bool TryApplyColor(GameObject target, Enum colorGroup)
        {
            if (target == null || colorGroup == null)
                return false;

            SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
            return spriteRenderer != null && TryApplyColor(spriteRenderer, Convert.ToInt32(colorGroup));
        }

        /// <summary>
        /// Applies the color for a given enum-based color group to a SpriteRenderer.
        /// </summary>
        public static bool TryApplyColor(SpriteRenderer spriteRenderer, Enum colorGroup)
        {
            return spriteRenderer != null && colorGroup != null && TryApplyColor(spriteRenderer, Convert.ToInt32(colorGroup));
        }

        /// <summary>
        /// Applies a color by integer key (matching enum value).
        /// </summary>
        public static bool TryApplyColor(GameObject target, int colorKey)
        {
            if (target == null)
                return false;

            SpriteRenderer spriteRenderer = target.GetComponent<SpriteRenderer>();
            return spriteRenderer != null && TryApplyColor(spriteRenderer, colorKey);
        }

        private static bool TryApplyColor(SpriteRenderer spriteRenderer, int colorKey)
        {
            if (spriteRenderer == null)
                return false;

            if (ColorMap.TryGetValue(colorKey, out Color color))
            {
                spriteRenderer.color = color;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a color by enum-based color group.
        /// </summary>
        public static Color GetColor(Enum colorGroup)
        {
            return colorGroup != null && ColorMap.TryGetValue(Convert.ToInt32(colorGroup), out Color color) ? color : Color.white;
        }

        /// <summary>
        /// Gets a color by integer key.
        /// </summary>
        public static Color GetColor(int colorKey)
        {
            return ColorMap.TryGetValue(colorKey, out Color color) ? color : Color.white;
        }
    }
}