using System.Collections.Generic;
using UnityEngine;

namespace ConveyorBelt.Services
{
    /// <summary>
    /// Handles door zone detection and absorption calculations for conveyor belt items.
    /// Operates on generic data types through callbacks to avoid coupling with MonoBehaviour types.
    /// </summary>
    public class AbsorptionService
    {
        private Transform _door1;
        private Transform _door2;
        private float _itemDoorXTolerance;
        private PathSystem _pathSystem;

        public void Initialize(
            Transform door1,
            Transform door2,
            float itemDoorXTolerance,
            PathSystem pathSystem)
        {
            _door1 = door1;
            _door2 = door2;
            _itemDoorXTolerance = itemDoorXTolerance;
            _pathSystem = pathSystem;
        }

        /// <summary>
        /// Checks if a position on the path is within the door absorption zone.
        /// </summary>
        public bool IsPositionAtDoor(float distanceAlongPath)
        {
            if (_door1 == null || _door2 == null || _pathSystem == null)
                return false;

            float doorMinY = Mathf.Min(_door1.position.y, _door2.position.y);
            float doorMaxY = Mathf.Max(_door1.position.y, _door2.position.y);
            float doorCenterX = (_door1.position.x + _door2.position.x) * 0.5f;

            PathSample sample = _pathSystem.GetPositionAtDistance(distanceAlongPath);
            float itemY = sample.Position.y;
            bool itemInsideDoorY = itemY >= doorMinY && itemY <= doorMaxY;
            bool itemAtGateX = Mathf.Abs(sample.Position.x - doorCenterX) <= _itemDoorXTolerance;

            return itemInsideDoorY && itemAtGateX;
        }

        /// <summary>
        /// Builds a list of absorbable rows from an item list given a column count and max rows.
        /// </summary>
        public static List<List<T>> GetAbsorbableRows<T>(
            IReadOnlyList<T> items,
            int groupColumns,
            int maxRows,
            System.Func<T, bool> isAvailable)
        {
            List<List<T>> rows = new List<List<T>>();

            if (items == null || maxRows <= 0)
                return rows;

            int rowCount = Mathf.CeilToInt(items.Count / (float)groupColumns);

            for (int row = 0; row < rowCount && rows.Count < maxRows; row++)
            {
                List<T> rowItems = new List<T>();
                int rowStart = row * groupColumns;
                int rowEnd = Mathf.Min(rowStart + groupColumns, items.Count);

                for (int i = rowStart; i < rowEnd; i++)
                {
                    T item = items[i];
                    if (isAvailable(item))
                        rowItems.Add(item);
                }

                if (rowItems.Count > 0)
                    rows.Add(rowItems);
            }

            return rows;
        }

        /// <summary>
        /// Counts active items from a list using a predicate.
        /// </summary>
        public static int GetActiveItemCount<T>(IReadOnlyList<T> items, System.Func<T, bool> isActive)
        {
            if (items == null)
                return 0;

            int count = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (isActive(items[i]))
                    count++;
            }

            return count;
        }
    }
}