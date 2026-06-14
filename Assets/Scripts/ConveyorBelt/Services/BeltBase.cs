using System;
using ConveyorBelt.Services;
using Core;
using UnityEngine;

namespace ConveyorBelt
{
    /// <summary>
    /// Abstract base class for conveyor belt systems (ConveyorBelt and BoxConveyorBelt).
    /// Consolidates shared path infrastructure, template caching, and common fields
    /// to eliminate code duplication.
    /// 
    /// NOTE: Derived classes must call SetupBeltBase() from their own Awake() method.
    /// The base class does NOT declare Awake() to avoid Unity message hiding issues.
    /// Update() is declared here and calls UpdateBelt() — no derived Update() needed.
    /// OnValidate() is virtual so derived classes can extend it.
    /// </summary>
    public abstract class BeltBase : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] protected LineRenderer lineRenderer;
        [SerializeField] protected float speed = 2f;
        [SerializeField] protected bool faceDirection = true;

        // Shared services
        protected PathSystem pathSystem;
        protected bool pathDirty = true;

        /// <summary>
        /// Initializes shared belt infrastructure. Must be called from derived Awake().
        /// Sets up PathSystem, caches scene templates, and rebuilds the path if needed.
        /// </summary>
        protected void SetupBeltBase()
        {
            pathSystem = new PathSystem();
            if (lineRenderer != null && lineRenderer.positionCount >= 2)
                pathSystem.RebuildPath(lineRenderer);

            CacheSceneTemplates();

            if (pathSystem.IsPathDirty)
                pathSystem.RebuildPath(lineRenderer);
        }

        protected virtual void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            pathDirty = true;
        }

        protected virtual void Update()
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return;

            if (pathDirty || pathSystem.IsPathDirty)
            {
                pathSystem.RebuildPath(lineRenderer);
                pathDirty = false;
            }

            if (pathSystem.PathLength <= 0.0001f)
                return;

            UpdateBelt();
        }

        /// <summary>
        /// Derived classes implement per-frame movement/update logic here.
        /// Called after path validation in Update().
        /// </summary>
        protected abstract void UpdateBelt();

        /// <summary>
        /// Derived classes implement template caching for scene-placed items/boxes.
        /// </summary>
        protected abstract void CacheSceneTemplates();

        /// <summary>
        /// Applies the rotation to face movement direction if faceDirection is enabled.
        /// </summary>
        protected void ApplyDirectionalRotation(Transform target, Vector3 direction)
        {
            if (!faceDirection || target == null || direction.sqrMagnitude <= 0.0001f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            target.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// Rebuilds the path system from the configured LineRenderer.
        /// </summary>
        protected void RebuildPathIfNeeded()
        {
            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return;

            if (pathDirty || pathSystem.IsPathDirty)
            {
                pathSystem.RebuildPath(lineRenderer);
                pathDirty = false;
            }
        }

        /// <summary>
        /// Applies color to a GameObject via ColorService using the Core.ColorGroup mapping.
        /// Accepts any Enum (backward compatible with nested ItemColorGroup enums).
        /// </summary>
        protected static void ApplyColorTo(GameObject target, Enum colorGroup)
        {
            ColorService.TryApplyColor(target, colorGroup);
        }

        #region Color Group Conversion

        /// <summary>
        /// Converts a nested ItemColorGroup enum (from ConveyorBelt or BoxConveyorBelt)
        /// to Core.ColorGroup via integer casting.
        /// </summary>
        protected static ColorGroup ToColorGroup(System.Enum itemColorGroup)
        {
            return (ColorGroup)(int)(object)itemColorGroup;
        }

        /// <summary>
        /// Converts Core.ColorGroup to the target enum type T (e.g., ConveyorBelt.ItemColorGroup)
        /// via integer casting. Useful when passing values to systems expecting the nested enum.
        /// </summary>
        protected static T FromColorGroup<T>(ColorGroup colorGroup) where T : System.Enum
        {
            return (T)(object)(int)colorGroup;
        }

        #endregion
    }
}