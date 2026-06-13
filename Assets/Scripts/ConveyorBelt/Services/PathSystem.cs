using System.Collections.Generic;
using UnityEngine;

namespace ConveyorBelt.Services
{
    /// <summary>
    /// Handles path calculation and position sampling along a LineRenderer-defined path.
    /// Extracted to eliminate duplication between ConveyorBelt and BoxConveyorBelt.
    /// </summary>
    public class PathSystem
    {
        private readonly List<float> _segmentLengths = new List<float>();
        private readonly List<float> _segmentStartDistances = new List<float>();
        private readonly List<Vector3> _segmentStartPositions = new List<Vector3>();
        private readonly List<Vector3> _segmentEndPositions = new List<Vector3>();
        private float _pathLength;
        private bool _pathDirty = true;

        public bool IsPathDirty => _pathDirty;
        public float PathLength => _pathLength;

        public void MarkDirty()
        {
            _pathDirty = true;
        }

        public void RebuildPath(LineRenderer lineRenderer)
        {
            _segmentLengths.Clear();
            _segmentStartDistances.Clear();
            _segmentStartPositions.Clear();
            _segmentEndPositions.Clear();
            _pathLength = 0f;

            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return;

            int segmentCount = lineRenderer.loop ? lineRenderer.positionCount : lineRenderer.positionCount - 1;

            for (int i = 0; i < segmentCount; i++)
            {
                Vector3 start = GetWorldPosition(lineRenderer, i);
                Vector3 end = GetWorldPosition(lineRenderer, (i + 1) % lineRenderer.positionCount);
                float length = Vector3.Distance(start, end);

                _segmentStartDistances.Add(_pathLength);
                _segmentLengths.Add(length);
                _segmentStartPositions.Add(start);
                _segmentEndPositions.Add(end);
                _pathLength += length;
            }

            _pathDirty = false;
        }

        public PathSample GetPositionAtDistance(float distance)
        {
            distance = WrapDistance(distance);

            for (int i = 0; i < _segmentLengths.Count; i++)
            {
                float segmentLength = _segmentLengths[i];

                if (segmentLength <= 0.0001f)
                    continue;

                float segmentStartDistance = _segmentStartDistances[i];

                if (distance > segmentStartDistance + segmentLength && i < _segmentLengths.Count - 1)
                    continue;

                Vector3 start = _segmentStartPositions[i];
                Vector3 end = _segmentEndPositions[i];
                Vector3 direction = (end - start).normalized;
                float t = Mathf.Clamp01((distance - segmentStartDistance) / segmentLength);
                return new PathSample(Vector3.LerpUnclamped(start, end, t), direction);
            }

            if (_segmentStartPositions.Count > 0 && _segmentEndPositions.Count > 0)
            {
                return new PathSample(
                    _segmentStartPositions[0],
                    (_segmentEndPositions[0] - _segmentStartPositions[0]).normalized);
            }

            return new PathSample(Vector3.zero, Vector3.right);
        }

        public float WrapDistance(float distance)
        {
            if (_pathLength <= 0.0001f)
                return 0f;

            distance %= _pathLength;

            if (distance < 0f)
                distance += _pathLength;

            return distance;
        }

        private static Vector3 GetWorldPosition(LineRenderer lineRenderer, int index)
        {
            Vector3 position = lineRenderer.GetPosition(index);
            return lineRenderer.useWorldSpace ? position : lineRenderer.transform.TransformPoint(position);
        }
    }

    public readonly struct PathSample
    {
        public readonly Vector3 Position;
        public readonly Vector3 Direction;

        public PathSample(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction;
        }
    }
}