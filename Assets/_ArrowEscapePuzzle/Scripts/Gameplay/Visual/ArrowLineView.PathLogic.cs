using System;
using ArrowGame.Utils;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public partial class ArrowLineView
    {
        private void UpdateSnakeBody()
        {
            if (_movementPoints == null || _movementPoints.Length == 0) return;

            float headDist = _travelDistance + _bodyLength;
            Vector3 headPos = GetPointAlongPathPrecise(headDist);

            float tailDist;
            if (_currentState == ArrowState.Spawning)
            {
                tailDist = 0f;
            }
            else if (_currentState == ArrowState.Escaping)
            {
                tailDist = Mathf.Max(_travelDistance, pullbackOffset);
            }
            else
            {
                tailDist = Mathf.Max(_travelDistance, 0f);
            }

            Vector3 tailPos = GetPointAlongPathPrecise(tailDist);

            CollectPathNodes(tailPos, headPos, headDist, tailDist);
            SimplifyPath();
            ApplyPathToRenderer(headPos, headDist, tailPos);
        }

        private void CollectPathNodes(Vector3 tailPos, Vector3 headPos, float headDist, float tailDist)
        {
            _rawPointsCache.Clear();
            _rawPointsCache.Add(tailPos);

            for (int i = 0; i < _movementPoints.Length; i++)
            {
                float nodeDist = _movementDistances[i];
                if (nodeDist > tailDist + 0.01f && nodeDist < headDist - 0.01f)
                {
                    _rawPointsCache.Add(_movementPoints[i]);
                }
            }

            if (Vector3.Distance(_rawPointsCache[_rawPointsCache.Count - 1], headPos) > MIN_NODE_DISTANCE)
            {
                _rawPointsCache.Add(headPos);
            }
        }

        private void SimplifyPath()
        {
            _finalPointsCache.Clear();
            if (_rawPointsCache.Count < 2) return;

            _finalPointsCache.Add(_rawPointsCache[0]);
            for (int i = 1; i < _rawPointsCache.Count - 1; i++)
            {
                Vector3 prev = _finalPointsCache[_finalPointsCache.Count - 1];
                Vector3 curr = _rawPointsCache[i];
                Vector3 next = _rawPointsCache[i + 1];
                Vector3 dir1 = (curr - prev).normalized;
                Vector3 dir2 = (next - curr).normalized;
                if (Vector3.Dot(dir1, dir2) < DOT_THRESHOLD) _finalPointsCache.Add(curr);
            }

            _finalPointsCache.Add(_rawPointsCache[_rawPointsCache.Count - 1]);
        }

        private void ApplyPathToRenderer(Vector3 headPos, float headDist, Vector3 tailPos)
        {
            if (_finalPointsCache.Count >= 2)
            {
                if (_finalPointsCache.Count > _renderPositionsCache.Length)
                    Array.Resize(ref _renderPositionsCache, _finalPointsCache.Count * 2);

                for (int i = 0; i < _finalPointsCache.Count; i++)
                    _renderPositionsCache[i] = _finalPointsCache[i];

                lineRenderer.positionCount = _finalPointsCache.Count;
                lineRenderer.SetPositions(_renderPositionsCache);

                headTransform.localPosition = headPos;

                Vector3 lookBackPos = GetPointAlongPathPrecise(Mathf.Max(0f, headDist - 0.1f));
                Vector3 dir = (headPos - lookBackPos).normalized;
                if (dir == Vector3.zero)
                {
                    dir = GetDirectionAtDistance(headDist);
                }

                float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle - 90f);

                if (_currentState == ArrowState.Spawning && headDist <= 0.05f)
                {
                    _currentHeadRotation = targetRotation;
                }
                else
                {
                    _currentHeadRotation =
                        Quaternion.RotateTowards(_currentHeadRotation, targetRotation, 2000f * Time.deltaTime);
                }

                headTransform.localRotation = _currentHeadRotation;
            }
            else
            {
                lineRenderer.positionCount = 0;
                headTransform.localPosition = headPos;
            }

            if (escapeTrail != null)
            {
                escapeTrail.transform.localPosition = tailPos;
            }
        }

        private Vector3 GetPointAlongPathPrecise(float distance)
        {
            if (_movementPoints == null || _movementPoints.Length == 0) return Vector3.zero;

            float maxPathDist = _movementLength;
            Vector3 safeDirection = GetDirectionAtDistance(maxPathDist);

            if (distance >= maxPathDist)
            {
                return _movementPoints[_movementPoints.Length - 1] + safeDirection * (distance - maxPathDist);
            }

            if (distance <= 0f)
            {
                Vector3 backDirection = GetDirectionAtDistance(0f);
                return _movementPoints[0] - backDirection * Mathf.Abs(distance);
            }

            for (int i = 1; i < _movementPoints.Length; i++)
            {
                if (distance > _movementDistances[i]) continue;

                float segmentDistance = _movementDistances[i] - _movementDistances[i - 1];
                if (segmentDistance <= Mathf.Epsilon)
                {
                    return _movementPoints[i];
                }

                float t = Mathf.InverseLerp(_movementDistances[i - 1], _movementDistances[i], distance);
                return Vector3.Lerp(_movementPoints[i - 1], _movementPoints[i], t);
            }

            return _movementPoints[_movementPoints.Length - 1];
        }

        private Vector3 GetDirectionAtDistance(float distance)
        {
            if (_movementPoints == null || _movementPoints.Length < 2) return _escapeDirection;

            if (distance <= 0f)
            {
                return GetDirectionForSegment(0);
            }

            if (distance >= _movementLength)
            {
                return _escapeDirection.sqrMagnitude > 0f ? _escapeDirection.normalized : GetDirectionForSegment(_movementPoints.Length - 2);
            }

            for (int i = 1; i < _movementPoints.Length; i++)
            {
                if (distance <= _movementDistances[i])
                {
                    return GetDirectionForSegment(i - 1);
                }
            }

            return _escapeDirection.sqrMagnitude > 0f ? _escapeDirection.normalized : Vector3.up;
        }

        private Vector3 GetDirectionForSegment(int startIndex)
        {
            if (_movementPoints == null || _movementPoints.Length < 2) return _escapeDirection;

            int clampedIndex = Mathf.Clamp(startIndex, 0, _movementPoints.Length - 2);
            for (int i = clampedIndex; i < _movementPoints.Length - 1; i++)
            {
                Vector3 segment = _movementPoints[i + 1] - _movementPoints[i];
                if (segment.sqrMagnitude > 0.0001f) return segment.normalized;
            }

            return _escapeDirection.sqrMagnitude > 0f ? _escapeDirection.normalized : Vector3.up;
        }

        private void UpdateDirectionLine()
        {
            if (lineDirection == null) return;

            lineDirection.useWorldSpace = true;
            _directionPointsCache.Clear();

            Vector3 worldStart = headTransform.position;
            _directionPointsCache.Add(worldStart);

            if (_movementPoints != null && _movementPoints.Length > _bodyPoints.Length)
            {
                for (int i = _bodyPoints.Length; i < _movementPoints.Length; i++)
                {
                    _directionPointsCache.Add(transform.TransformPoint(_movementPoints[i]));
                }

                Vector3 lastPoint = _directionPointsCache[_directionPointsCache.Count - 1];
                Vector3 exitDirection = _escapeDirection.sqrMagnitude > 0f ? _escapeDirection.normalized : Vector3.up;
                float distanceToEdge = CameraUtils.GetDistanceToEdge(_mainCam, lastPoint, exitDirection);
                _directionPointsCache.Add(lastPoint + exitDirection * distanceToEdge);
            }
            else
            {
                Vector3 straightDirection = _escapeDirection.sqrMagnitude > 0f ? _escapeDirection.normalized : Vector3.up;
                Vector3 offsetStart = worldStart + straightDirection * (_cellSize * 0.1f);
                float distance = CameraUtils.GetDistanceToEdge(_mainCam, offsetStart, straightDirection);
                _directionPointsCache[0] = offsetStart;
                _directionPointsCache.Add(offsetStart + straightDirection * distance);
            }

            // NEW: Áp dụng progress để tạo hiệu ứng mọc theo chiều dài
            if (_directionLineProgress < 0.999f && _directionPointsCache.Count >= 2)
            {
                float totalLength = 0f;
                for (int i = 0; i < _directionPointsCache.Count - 1; i++)
                {
                    totalLength += Vector3.Distance(_directionPointsCache[i], _directionPointsCache[i + 1]);
                }

                float targetLength = totalLength * _directionLineProgress;
                float currentLength = 0f;
                int finalCount = _directionPointsCache.Count;

                for (int i = 0; i < _directionPointsCache.Count - 1; i++)
                {
                    float segmentLength = Vector3.Distance(_directionPointsCache[i], _directionPointsCache[i + 1]);
                    if (currentLength + segmentLength >= targetLength)
                    {
                        float remaining = targetLength - currentLength;
                        Vector3 dir = (_directionPointsCache[i + 1] - _directionPointsCache[i]).normalized;
                        _directionPointsCache[i + 1] = _directionPointsCache[i] + dir * remaining;
                        finalCount = i + 2;
                        break;
                    }
                    currentLength += segmentLength;
                }

                if (finalCount < _directionPointsCache.Count)
                {
                    _directionPointsCache.RemoveRange(finalCount, _directionPointsCache.Count - finalCount);
                }
            }

            if (_directionPointsCache.Count > _renderPositionsCache.Length)
                Array.Resize(ref _renderPositionsCache, _directionPointsCache.Count * 2);

            for (int i = 0; i < _directionPointsCache.Count; i++)
                _renderPositionsCache[i] = _directionPointsCache[i];

            lineDirection.positionCount = _directionPointsCache.Count;
            lineDirection.SetPositions(_renderPositionsCache);
        }

        private float GetHeadRotation(CellType type) => type switch
        {
            CellType.ArrowHeadUp => 0f,
            CellType.ArrowHeadRight => -90f,
            CellType.ArrowHeadDown => 180f,
            CellType.ArrowHeadLeft => 90f,
            _ => 0f
        };

        private Vector3 GetDirectionVector(CellType type) => type switch
        {
            CellType.ArrowHeadUp => Vector3.up,
            CellType.ArrowHeadRight => Vector3.right,
            CellType.ArrowHeadDown => Vector3.down,
            CellType.ArrowHeadLeft => Vector3.left,
            _ => Vector3.zero
        };
    }
}
