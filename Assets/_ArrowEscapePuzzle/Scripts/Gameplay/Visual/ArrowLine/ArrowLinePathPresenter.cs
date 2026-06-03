using System;
using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    internal sealed class ArrowLinePathPresenter
    {
        private struct PathTrigger
        {
            public float Distance;
            public ArrowPathVisualTrigger Payload;
        }

        private readonly ArrowLinePathModel _pathModel = new ArrowLinePathModel();
        private readonly List<PathTrigger> _pathTriggers = new List<PathTrigger>();
        private int _currentTriggerIndex;

        public bool HasMovementPath => _pathModel.HasMovementPath;
        public bool HasPortalSegments => _pathModel.HasPortalSegments;
        public float BodyLength => _pathModel.BodyLength;
        public float MovementLength => _pathModel.MovementLength;
        public int SegmentCount => _pathModel.SegmentCount;
        public int MovementPointCount => _pathModel.MovementPoints != null ? _pathModel.MovementPoints.Length : 0;
        public int MovementDistanceCount => _pathModel.MovementDistances != null ? _pathModel.MovementDistances.Length : 0;

        public void Clear()
        {
            _pathModel.Clear();
            _pathTriggers.Clear();
            _currentTriggerIndex = 0;
        }

        public void RebuildPath(Vector3[] bodyPoints, EscapeTraceResult traceResult,
            Func<Vector2Int, Vector3> gridToLocalPoint, float cellSize, Vector2Int activeEndpointGridPosition)
        {
            Clear();

            if (bodyPoints == null || bodyPoints.Length == 0)
            {
                return;
            }

            IReadOnlyList<EscapeTraceWaypoint> routeWaypoints = traceResult?.RouteWaypoints;
            _pathModel.Build(bodyPoints, routeWaypoints, traceResult?.PortalJumps, gridToLocalPoint, cellSize);

            if (routeWaypoints == null)
            {
                return;
            }

            for (int i = 0; i < routeWaypoints.Count; i++)
            {
                EscapeTraceWaypoint waypoint = routeWaypoints[i];
                int pointIndex = bodyPoints.Length + i;
                if (pointIndex < 0 || pointIndex >= MovementDistanceCount)
                {
                    continue;
                }

                if (!waypoint.IsTeleportExit)
                {
                    _pathTriggers.Add(new PathTrigger
                    {
                        Distance = _pathModel.MovementDistances[pointIndex],
                        Payload = new ArrowPathVisualTrigger(
                            waypoint.Position,
                            ArrowPathVisualTriggerType.Normal,
                            ResolveWaypointTravelDirection(routeWaypoints, i, traceResult, activeEndpointGridPosition))
                    });
                    continue;
                }

#if UNITY_EDITOR
                if ((traceResult?.PortalJumps == null || traceResult.PortalJumps.Count == 0) && i <= 0)
                {
                    Debug.LogWarning("[ArrowLineView] TeleportExit waypoint has no previous waypoint. Cannot resolve portal entry.");
                }
                else if (traceResult?.PortalJumps == null || traceResult.PortalJumps.Count == 0)
                {
                    Vector2Int assumedEntry = routeWaypoints[i - 1].Position;
                    Vector2Int exit = waypoint.Position;
                    if (assumedEntry == exit)
                    {
                        Debug.LogWarning($"[ArrowLineView] Portal entry and exit are the same position: {exit}. Check portal route data.");
                    }
                }
#endif
            }

            AddPortalPathTriggers(traceResult, bodyPoints.Length, activeEndpointGridPosition);
            _pathTriggers.Sort((a, b) =>
            {
                int distanceCompare = a.Distance.CompareTo(b.Distance);
                if (distanceCompare != 0) return distanceCompare;
                return GetTriggerOrder(a.Payload.TriggerType).CompareTo(GetTriggerOrder(b.Payload.TriggerType));
            });
        }

        public void DispatchReachedTriggers(float headDist, Action<ArrowPathVisualTrigger> callback)
        {
            while (_currentTriggerIndex < _pathTriggers.Count && headDist >= _pathTriggers[_currentTriggerIndex].Distance)
            {
                callback?.Invoke(_pathTriggers[_currentTriggerIndex].Payload);
                _currentTriggerIndex++;
            }
        }

        public Vector3 GetPointAtDistance(float distance, Vector3 escapeDirection)
        {
            return _pathModel.GetPointAtDistance(distance, escapeDirection);
        }

        public Vector3 GetDirectionAtDistance(float distance, Vector3 escapeDirection)
        {
            return _pathModel.GetDirectionAtDistance(distance, escapeDirection);
        }

        public int GetSegmentIndexForDistance(float distance, bool preferLaterOnBoundary)
        {
            return _pathModel.GetSegmentIndexForDistance(distance, preferLaterOnBoundary);
        }

        public ArrowLinePathModel.Segment GetSegment(int index)
        {
            return _pathModel.GetSegment(index);
        }

        public Vector3 GetPointAlongSegment(int segmentIndex, float distance, Vector3 escapeDirection,
            bool allowAfterEnd, bool allowBeforeStart)
        {
            return _pathModel.GetPointAlongSegment(segmentIndex, distance, allowAfterEnd, allowBeforeStart, escapeDirection);
        }

        public Vector3 GetDirectionForSegment(int segmentIndex, Vector3 escapeDirection)
        {
            return _pathModel.GetDirectionForSegment(segmentIndex, escapeDirection);
        }

        public float GetMovementDistance(int pointIndex)
        {
            return _pathModel.MovementDistances[pointIndex];
        }

        public void BuildVisibleBodyChunks(float tailDist, float headDist, List<ArrowLinePathModel.VisibleBodyChunk> cache)
        {
            cache.Clear();
            _pathModel.BuildVisibleBodyChunks(tailDist, headDist);
            for (int i = 0; i < _pathModel.VisibleBodyChunkCount; i++)
            {
                cache.Add(_pathModel.GetVisibleBodyChunk(i));
            }
        }

        public void BuildContinuousPathPoints(Vector3 tailPos, Vector3 headPos, float tailDist, float headDist,
            Vector3 escapeDirection, List<Vector3> rawPoints, List<Vector3> finalPoints, float minNodeDistance, float dotThreshold)
        {
            rawPoints.Clear();
            rawPoints.Add(tailPos);

            Vector3[] movementPoints = _pathModel.MovementPoints;
            float[] movementDistances = _pathModel.MovementDistances;
            for (int i = 0; i < movementPoints.Length; i++)
            {
                float nodeDist = movementDistances[i];
                if (nodeDist > tailDist + 0.01f && nodeDist < headDist - 0.01f)
                {
                    rawPoints.Add(movementPoints[i]);
                }
            }

            if (Vector3.Distance(rawPoints[rawPoints.Count - 1], headPos) > minNodeDistance)
            {
                rawPoints.Add(headPos);
            }

            SimplifyPath(rawPoints, finalPoints, dotThreshold);
        }

        public bool BuildVisibleSegmentPoints(ArrowLinePathModel.VisibleBodyChunk chunk, Vector3 escapeDirection,
            List<Vector3> rawPoints, List<Vector3> finalPoints, float minNodeDistance, float dotThreshold)
        {
            rawPoints.Clear();
            finalPoints.Clear();

            if (chunk.EndDistance <= chunk.StartDistance + ArrowLinePathModel.RenderEpsilon)
            {
                return false;
            }

            ArrowLinePathModel.Segment segment = _pathModel.GetSegment(chunk.SegmentIndex);
            bool isFirstSegment = segment.StartDistance <= ArrowLinePathModel.RenderEpsilon;
            bool isLastSegment = segment.EndDistance >= _pathModel.MovementLength - ArrowLinePathModel.RenderEpsilon;

            rawPoints.Add(_pathModel.GetPointAlongSegment(chunk.SegmentIndex, chunk.StartDistance,
                isLastSegment, isFirstSegment, escapeDirection));

            for (int i = segment.StartPointIndex + 1; i <= segment.EndPointIndex; i++)
            {
                float nodeDist = _pathModel.MovementDistances[i];
                if (nodeDist > chunk.StartDistance + ArrowLinePathModel.RenderEpsilon &&
                    nodeDist < chunk.EndDistance - ArrowLinePathModel.RenderEpsilon)
                {
                    rawPoints.Add(_pathModel.MovementPoints[i]);
                }
            }

            Vector3 endPoint = _pathModel.GetPointAlongSegment(chunk.SegmentIndex, chunk.EndDistance,
                isLastSegment, isFirstSegment, escapeDirection);

            if (rawPoints.Count == 0 || Vector3.Distance(rawPoints[rawPoints.Count - 1], endPoint) > minNodeDistance)
            {
                rawPoints.Add(endPoint);
            }
            else if (rawPoints.Count == 1)
            {
                rawPoints.Add(endPoint);
            }

            SimplifyPath(rawPoints, finalPoints, dotThreshold);
            return finalPoints.Count >= 2;
        }

        public bool TryBuildStaticSegmentPoints(int segmentIndex, Vector3 worldStart, bool overrideFirstPoint,
            Func<Vector3, Vector3> localToWorldPoint, List<Vector3> pointsCache)
        {
            pointsCache.Clear();
            ArrowLinePathModel.Segment segment = _pathModel.GetSegment(segmentIndex);

            for (int i = segment.StartPointIndex; i <= segment.EndPointIndex; i++)
            {
                Vector3 point = localToWorldPoint(_pathModel.MovementPoints[i]);
                if (pointsCache.Count == 0 && overrideFirstPoint)
                {
                    pointsCache.Add(worldStart);
                    if (Vector3.Distance(worldStart, point) <= 0.02f)
                    {
                        continue;
                    }
                }

                pointsCache.Add(point);
            }

            return pointsCache.Count >= 2;
        }

        public float CalculatePointListLength(List<Vector3> points)
        {
            float length = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                length += Vector3.Distance(points[i], points[i + 1]);
            }

            return length;
        }

        public void TrimPointListToLength(List<Vector3> points, float targetLength)
        {
            float currentLength = 0f;
            for (int i = 0; i < points.Count - 1; i++)
            {
                float segmentLength = Vector3.Distance(points[i], points[i + 1]);
                if (currentLength + segmentLength >= targetLength)
                {
                    float remaining = Mathf.Max(0f, targetLength - currentLength);
                    Vector3 direction = (points[i + 1] - points[i]).normalized;
                    points[i + 1] = points[i] + direction * remaining;
                    if (i + 2 < points.Count)
                    {
                        points.RemoveRange(i + 2, points.Count - (i + 2));
                    }
                    return;
                }

                currentLength += segmentLength;
            }
        }

        private void AddPortalPathTriggers(EscapeTraceResult traceResult, int bodyPointCount, Vector2Int activeEndpointGridPosition)
        {
            if (traceResult == null || traceResult.RouteWaypoints == null)
            {
                return;
            }

            if (traceResult.PortalJumps != null && traceResult.PortalJumps.Count > 0)
            {
                for (int i = 0; i < traceResult.PortalJumps.Count; i++)
                {
                    AddPortalPathTrigger(traceResult.PortalJumps[i], i, bodyPointCount);
                }

                return;
            }

            for (int i = 0; i < traceResult.RouteWaypoints.Count; i++)
            {
                if (!traceResult.RouteWaypoints[i].IsTeleportExit || i <= 0)
                {
                    continue;
                }

#if UNITY_EDITOR
                Debug.LogWarning("[ArrowLineView] Trace has teleport exit without PortalJumps metadata. Falling back to waypoint inference.");
#endif
                AddPortalPathTrigger(new EscapeTracePortalJump(
                    i - 1,
                    i,
                    traceResult.RouteWaypoints[i - 1].Position,
                    traceResult.RouteWaypoints[i].Position,
                    ResolveWaypointTravelDirection(traceResult.RouteWaypoints, i - 1, traceResult, activeEndpointGridPosition),
                    ResolveTeleportExitDirection(traceResult.RouteWaypoints, i, traceResult)), i, bodyPointCount);
            }
        }

        private void AddPortalPathTrigger(EscapeTracePortalJump jump, int jumpIndex, int bodyPointCount)
        {
            int entryPointIndex = bodyPointCount + jump.EntryWaypointIndex;
            int exitPointIndex = bodyPointCount + jump.ExitWaypointIndex;

            if (entryPointIndex < 0 || exitPointIndex < 0 ||
                entryPointIndex >= MovementDistanceCount || exitPointIndex >= MovementDistanceCount)
            {
                return;
            }

            float entryDistance = _pathModel.MovementDistances[entryPointIndex];
            float exitDistance = _pathModel.MovementDistances[exitPointIndex];
            int entrySegmentIndex = GetSegmentIndexForDistance(entryDistance, false);
            int exitSegmentIndex = GetSegmentIndexForDistance(exitDistance, true);

            _pathTriggers.Add(new PathTrigger
            {
                Distance = entryDistance,
                Payload = new ArrowPathVisualTrigger(jump.EntryPosition, ArrowPathVisualTriggerType.PortalEntry,
                    jump.EntryTravelDirection, jumpIndex, entrySegmentIndex)
            });

            _pathTriggers.Add(new PathTrigger
            {
                Distance = exitDistance,
                Payload = new ArrowPathVisualTrigger(jump.ExitPosition, ArrowPathVisualTriggerType.PortalExit,
                    jump.ExitTravelDirection, jumpIndex, exitSegmentIndex)
            });
        }

        private static Direction4 ResolveWaypointTravelDirection(IReadOnlyList<EscapeTraceWaypoint> routeWaypoints, int index,
            EscapeTraceResult traceResult, Vector2Int activeEndpointGridPosition)
        {
            if (routeWaypoints == null || index < 0 || index >= routeWaypoints.Count)
            {
                return traceResult != null ? traceResult.FinalDirection : Direction4.Up;
            }

            Vector2Int previousPosition = index == 0 ? activeEndpointGridPosition : routeWaypoints[index - 1].Position;
            return Direction4Extensions.FromVector(routeWaypoints[index].Position - previousPosition);
        }

        private static Direction4 ResolveTeleportExitDirection(IReadOnlyList<EscapeTraceWaypoint> routeWaypoints, int index,
            EscapeTraceResult traceResult)
        {
            if (routeWaypoints == null || index < 0 || index >= routeWaypoints.Count)
            {
                return traceResult != null ? traceResult.FinalDirection : Direction4.Up;
            }

            if (index + 1 < routeWaypoints.Count)
            {
                return Direction4Extensions.FromVector(routeWaypoints[index + 1].Position - routeWaypoints[index].Position);
            }

            return traceResult != null ? traceResult.FinalDirection : Direction4.Up;
        }

        private static int GetTriggerOrder(ArrowPathVisualTriggerType triggerType)
        {
            return triggerType switch
            {
                ArrowPathVisualTriggerType.PortalEntry => 0,
                ArrowPathVisualTriggerType.PortalExit => 1,
                _ => 2
            };
        }

        private static void SimplifyPath(List<Vector3> rawPoints, List<Vector3> finalPoints, float dotThreshold)
        {
            finalPoints.Clear();
            if (rawPoints.Count < 2)
            {
                return;
            }

            finalPoints.Add(rawPoints[0]);
            for (int i = 1; i < rawPoints.Count - 1; i++)
            {
                Vector3 prev = finalPoints[finalPoints.Count - 1];
                Vector3 current = rawPoints[i];
                Vector3 next = rawPoints[i + 1];

                if (Vector3.Distance(prev, current) < 0.001f || Vector3.Distance(current, next) < 0.001f)
                {
                    continue;
                }

                Vector3 dir1 = (current - prev).normalized;
                Vector3 dir2 = (next - current).normalized;
                if (Vector3.Dot(dir1, dir2) < dotThreshold)
                {
                    finalPoints.Add(current);
                }
            }

            finalPoints.Add(rawPoints[rawPoints.Count - 1]);
        }
    }
}
