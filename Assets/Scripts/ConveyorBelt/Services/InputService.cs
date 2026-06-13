using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ConveyorBelt.Services
{
    /// <summary>
    /// Centralizes input detection for mouse and touch input.
    /// Extracted from ColorBoxButton to allow reuse and testing.
    /// </summary>
    public static class InputService
    {
        /// <summary>
        /// Checks if the primary input (mouse click or touch) was pressed this frame.
        /// Returns the screen position of the press.
        /// </summary>
        public static bool WasPressedThisFrame(out Vector2 screenPosition)
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

        /// <summary>
        /// Converts a screen position to a world position using the main camera.
        /// </summary>
        public static Vector2? ScreenToWorldPoint(Vector2 screenPosition)
        {
            Camera mainCamera = Camera.main;
            return mainCamera != null ? (Vector2?)mainCamera.ScreenToWorldPoint(screenPosition) : null;
        }
    }
}