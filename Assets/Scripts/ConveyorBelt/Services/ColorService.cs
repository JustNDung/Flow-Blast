using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace ConveyorBelt.Services
{
    /// <summary>
    /// Centralizes color mapping logic for conveyor belt items.
    /// Now delegates to Core.ColorGroupExtensions as the single source of truth.
    /// Maintains backward compatibility with int-based and Enum-based lookups.
    /// </summary>
    public static class ColorService
    {
        /// <summary>
        /// Applies the color for a given enum-based color group to a GameObject's SpriteRenderer.
        /// </summary>
        public static bool TryApplyColor(GameObject target, Enum colorGroup)
        {
            if (target == null || colorGroup == null)
                return false;

            return TryApplyColor(target, Convert.ToInt32(colorGroup));
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

            if (Enum.IsDefined(typeof(ColorGroup), colorKey))
            {
                ColorGroup group = (ColorGroup)colorKey;
                group.ApplyTo(spriteRenderer);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a color by enum-based color group.
        /// </summary>
        public static Color GetColor(Enum colorGroup)
        {
            return GetColor(Convert.ToInt32(colorGroup));
        }

        /// <summary>
        /// Gets a color by integer key.
        /// </summary>
        public static Color GetColor(int colorKey)
        {
            if (Enum.IsDefined(typeof(ColorGroup), colorKey))
            {
                return ((ColorGroup)colorKey).ToUnityColor();
            }
            return Color.white;
        }
    }
}