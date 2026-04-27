using System;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Utils;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public partial class ArrowLineView
    {
        private void UpdateSnakeBody()
        {
            if (_basePoints == null || _basePoints.Length == 0) return;
            
            float totalBodyLength = (_basePoints.Length - 1) * _cellSize;
            float headDist = _travelDistance + totalBodyLength;
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
            
            for (int i = 0; i < _basePoints.Length; i++)
            {
                float nodeDist = i * _cellSize;
                if (nodeDist > tailDist + 0.01f && nodeDist < headDist - 0.01f)
                {
                    _rawPointsCache.Add(_basePoints[i]);
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

                Vector3 lookBackPos = GetPointAlongPathPrecise(Mathf.Max(0, headDist - 0.1f));
                Vector3 dir = (headPos - lookBackPos).normalized;

                if (dir == Vector3.zero) 
                {
                    dir = GetDirectionAtDistance(headDist);
                }
                
                float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle - 90f);

                if (_currentState == ArrowState.Spawning && headDist <= 0.05f)
                {
                    _currentHeadRotation = targetRotation;
                }
                else
                {
                    _currentHeadRotation = Quaternion.RotateTowards(_currentHeadRotation, targetRotation, 2000f * Time.deltaTime);
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
            int maxIndex = _basePoints.Length - 1;
            float maxPathDist = maxIndex * _cellSize;
            Vector3 safeDirection = GetDirectionAtDistance(maxPathDist);
            
            if (distance >= maxPathDist) 
                return _basePoints[maxIndex] + safeDirection * (distance - maxPathDist);
            
            if (distance <= 0)
            {
                Vector3 backDirection = GetDirectionAtDistance(0);
                return _basePoints[0] - backDirection * Mathf.Abs(distance);
            }

            int index = Mathf.FloorToInt(distance / _cellSize);
            float t = (distance % _cellSize) / _cellSize;
            return Vector3.Lerp(_basePoints[index], _basePoints[Mathf.Min(index + 1, maxIndex)], t);
        }

        private Vector3 GetDirectionAtDistance(float distance)
        {
            if (_basePoints == null || _basePoints.Length < 2) return _escapeDirection;
            
            if (distance <= 0) return (_basePoints[1] - _basePoints[0]).normalized;
            
            float maxPathDist = (_basePoints.Length - 1) * _cellSize;
            if (distance >= maxPathDist) return _escapeDirection;

            int index = Mathf.FloorToInt(distance / _cellSize);
            if (index >= _basePoints.Length - 1) return _escapeDirection;
            
            return (_basePoints[index + 1] - _basePoints[index]).normalized;
        }

        private void UpdateDirectionLine()
        {
            if (lineDirection == null) return;
            lineDirection.useWorldSpace = true;

            Vector3 worldStart = headTransform.position + _escapeDirection * (_cellSize * 0.1f);
            float distance = CameraUtils.GetDistanceToEdge(_mainCam, worldStart, _escapeDirection);

            lineDirection.positionCount = 2;
            lineDirection.SetPositions(new[] { worldStart, worldStart + _escapeDirection * distance });
        }

        private float GetHeadRotation(CellType type) => type switch
        {
            CellType.ArrowHeadUp => 0f, CellType.ArrowHeadRight => -90f,
            CellType.ArrowHeadDown => 180f, CellType.ArrowHeadLeft => 90f, _ => 0f
        };

        private Vector3 GetDirectionVector(CellType type) => type switch
        {
            CellType.ArrowHeadUp => Vector3.up, CellType.ArrowHeadRight => Vector3.right,
            CellType.ArrowHeadDown => Vector3.down, CellType.ArrowHeadLeft => Vector3.left, _ => Vector3.zero
        };
    }
}