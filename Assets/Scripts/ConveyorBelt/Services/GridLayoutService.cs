using UnityEngine;

namespace ConveyorBelt.Services
{
    /// <summary>
    /// Handles grid layout calculations for items arranged in rows and columns along a path.
    /// Extracted to centralize grid offset and group length math used by ConveyorBelt.
    /// </summary>
    public class GridLayoutService
    {
        private readonly int _groupRows;
        private readonly int _groupColumns;
        private readonly Vector2 _cellSpacing;
        private readonly float _groupSpacing;

        public int GroupRows => _groupRows;
        public int GroupColumns => _groupColumns;
        public int Capacity => _groupRows * _groupColumns;

        public GridLayoutService(int groupRows, int groupColumns, Vector2 cellSpacing, float groupSpacing)
        {
            _groupRows = Mathf.Max(1, groupRows);
            _groupColumns = Mathf.Max(1, groupColumns);
            _cellSpacing = new Vector2(Mathf.Max(0.01f, cellSpacing.x), Mathf.Max(0.01f, cellSpacing.y));
            _groupSpacing = Mathf.Max(0f, groupSpacing);
        }

        /// <summary>
        /// Calculates the local grid offset for an item at a given index within a group.
        /// </summary>
        public Vector3 GetGridOffset(int index, int itemCount, Vector3 tangent, Vector3 side)
        {
            int row = index / _groupColumns;
            int column = index % _groupColumns;
            int usedRows = Mathf.Min(_groupRows, Mathf.CeilToInt(itemCount / (float)_groupColumns));
            int usedColumns = Mathf.Min(_groupColumns, itemCount);
            float x = (column - (usedColumns - 1) * 0.5f) * _cellSpacing.x;
            float y = ((usedRows - 1) * 0.5f - row) * _cellSpacing.y;
            return tangent * x + side * y;
        }

        /// <summary>
        /// Calculates the total length of a group based on item count.
        /// </summary>
        public float GetGroupLength(int itemCount)
        {
            int usedColumns = Mathf.Min(_groupColumns, Mathf.Max(1, itemCount));
            return Mathf.Max(_cellSpacing.x, (usedColumns - 1) * _cellSpacing.x + _groupSpacing);
        }
    }
}