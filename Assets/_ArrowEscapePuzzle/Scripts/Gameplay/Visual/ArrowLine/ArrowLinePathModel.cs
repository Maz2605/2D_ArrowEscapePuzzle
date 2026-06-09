using System;
using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    internal sealed class ArrowLinePathModel
    {
        internal const float SampleEpsilon = 0.0001f;
        internal const float RenderEpsilon = 0.001f;

        internal struct Segment
        {
            public int StartPointIndex;
            public int EndPointIndex;
            public float StartDistance;
            public float EndDistance;
        }

        internal struct VisibleBodyChunk
        {
            public int SegmentIndex;
            public float StartDistance;
            public float EndDistance;
            public bool ContainsHead;
        }

        private readonly List<Vector3> _pointBuildCache = new List<Vector3>();
        private readonly List<float> _distanceBuildCache = new List<float>();
        private readonly List<Segment> _segments = new List<Segment>();
        private readonly List<VisibleBodyChunk> _visibleBodyChunks = new List<VisibleBodyChunk>();
        private readonly List<EscapeTracePortalJump> _fallbackPortalJumps = new List<EscapeTracePortalJump>();

        public Vector3[] MovementPoints { get; private set; }
        public float[] MovementDistances { get; private set; }
        public float BodyLength { get; private set; }
        public float MovementLength { get; private set; }
        public int SegmentCount => _segments.Count;
        public int VisibleBodyChunkCount => _visibleBodyChunks.Count;
        public bool HasMovementPath => MovementPoints != null && MovementPoints.Length > 0;
        public bool HasPortalSegments => _segments.Count > 1;

        public void Clear()
        {
            _pointBuildCache.Clear();
            _distanceBuildCache.Clear();
            _segments.Clear();
            _visibleBodyChunks.Clear();
            _fallbackPortalJumps.Clear();
            MovementPoints = null;
            MovementDistances = null;
            BodyLength = 0f;
            MovementLength = 0f;
        }

        public void Build(Vector3[] bodyPoints, IReadOnlyList<EscapeTraceWaypoint> routeWaypoints,
            IReadOnlyList<EscapeTracePortalJump> portalJumps, Func<Vector2Int, Vector3> gridToLocalPoint, float cellSize)
        {
            Clear();

            if (bodyPoints == null || bodyPoints.Length == 0)
            {
                return;
            }

            for (int i = 0; i < bodyPoints.Length; i++)
            {
                _pointBuildCache.Add(bodyPoints[i]);
                _distanceBuildCache.Add(i * cellSize);
            }

            BodyLength = (bodyPoints.Length - 1) * cellSize;
            float currentDistance = BodyLength;

            if (routeWaypoints != null)
            {
                for (int i = 0; i < routeWaypoints.Count; i++)
                {
                    EscapeTraceWaypoint waypoint = routeWaypoints[i];
                    float stepCost = waypoint.IsTeleportExit ? 0f : waypoint.StepCost;
                    currentDistance += stepCost * cellSize;

                    _pointBuildCache.Add(gridToLocalPoint(waypoint.Position));
                    _distanceBuildCache.Add(currentDistance);
                }
            }

            MovementPoints = _pointBuildCache.ToArray();
            MovementDistances = _distanceBuildCache.ToArray();
            MovementLength = currentDistance;

            IReadOnlyList<EscapeTracePortalJump> jumps = portalJumps;
            if ((jumps == null || jumps.Count == 0) && routeWaypoints != null)
            {
                BuildFallbackPortalJumps(routeWaypoints);
                jumps = _fallbackPortalJumps;
            }

            BuildSegments(bodyPoints.Length, jumps);
        }

        public Segment GetSegment(int index)
        {
            return _segments[index];
        }

        public VisibleBodyChunk GetVisibleBodyChunk(int index)
        {
            return _visibleBodyChunks[index];
        }

        public Vector3 GetPointAtDistance(float distance, Vector3 escapeDirection)
        {
            if (!HasMovementPath)
            {
                return Vector3.zero;
            }

            if (distance >= MovementLength)
            {
                Vector3 direction = GetDirectionAtDistance(MovementLength, escapeDirection);
                return MovementPoints[MovementPoints.Length - 1] + direction * (distance - MovementLength);
            }

            if (distance <= 0f)
            {
                Vector3 direction = GetDirectionAtDistance(0f, escapeDirection);
                return MovementPoints[0] - direction * Mathf.Abs(distance);
            }

            int segmentIndex = GetSegmentIndexForDistance(distance, true);
            return segmentIndex >= 0
                ? GetPointAlongSegment(_segments[segmentIndex], distance, false, false, escapeDirection)
                : MovementPoints[0];
        }

        public Vector3 GetDirectionAtDistance(float distance, Vector3 escapeDirection)
        {
            if (MovementPoints == null || MovementPoints.Length < 2)
            {
                return GetFallbackDirection(escapeDirection);
            }

            if (distance <= 0f)
            {
                return _segments.Count > 0 ? GetDirectionForSegment(_segments[0], escapeDirection, false) : GetFallbackDirection(escapeDirection);
            }

            if (distance >= MovementLength)
            {
                return GetFallbackDirection(escapeDirection);
            }

            // Find the sub-segment containing this distance
            for (int i = 1; i < MovementPoints.Length; i++)
            {
                if (distance <= MovementDistances[i] + SampleEpsilon)
                {
                    Vector3 dir = MovementPoints[i] - MovementPoints[i - 1];
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        return dir.normalized;
                    }
                }
            }

            int segmentIndex = GetSegmentIndexForDistance(distance, true);
            return segmentIndex >= 0
                ? GetDirectionForSegment(_segments[segmentIndex], escapeDirection, false)
                : GetFallbackDirection(escapeDirection);
        }

        public int GetSegmentIndexForDistance(float distance, bool preferLaterOnBoundary)
        {
            if (_segments.Count == 0)
            {
                return -1;
            }

            int resolvedIndex = -1;
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment segment = _segments[i];
                if (distance + SampleEpsilon < segment.StartDistance)
                {
                    if (!preferLaterOnBoundary && resolvedIndex < 0)
                    {
                        return i;
                    }

                    break;
                }

                if (distance <= segment.EndDistance + SampleEpsilon)
                {
                    resolvedIndex = i;
                    if (!preferLaterOnBoundary)
                    {
                        break;
                    }
                }
            }

            if (resolvedIndex >= 0)
            {
                return resolvedIndex;
            }

            return distance < _segments[0].StartDistance ? 0 : _segments.Count - 1;
        }

        public void BuildVisibleBodyChunks(float tailDist, float headDist)
        {
            _visibleBodyChunks.Clear();

            if (_segments.Count == 0)
            {
                return;
            }

            int headSegmentIndex = GetSegmentIndexForDistance(headDist, true);
            for (int i = 0; i < _segments.Count; i++)
            {
                Segment segment = _segments[i];
                bool isFirstSegment = segment.StartDistance <= RenderEpsilon;
                bool isLastSegment = segment.EndDistance >= MovementLength - RenderEpsilon;

                float visibleStart = isFirstSegment
                    ? tailDist
                    : Mathf.Max(tailDist, segment.StartDistance);

                float visibleEnd = isLastSegment
                    ? headDist
                    : Mathf.Min(headDist, segment.EndDistance);

                if (visibleEnd <= visibleStart + RenderEpsilon)
                {
                    continue;
                }

                _visibleBodyChunks.Add(new VisibleBodyChunk
                {
                    SegmentIndex = i,
                    StartDistance = visibleStart,
                    EndDistance = visibleEnd,
                    ContainsHead = i == headSegmentIndex
                });
            }

            _visibleBodyChunks.Sort(CompareVisibleBodyChunkOrder);
        }

        public Vector3 GetPointAlongSegment(int segmentIndex, float distance, bool allowAfterEnd,
            bool allowBeforeStart, Vector3 escapeDirection)
        {
            return GetPointAlongSegment(_segments[segmentIndex], distance, allowAfterEnd, allowBeforeStart, escapeDirection);
        }

        public Vector3 GetDirectionForSegment(int segmentIndex, Vector3 escapeDirection)
        {
            return GetDirectionForSegment(_segments[segmentIndex], escapeDirection);
        }

        private void BuildFallbackPortalJumps(IReadOnlyList<EscapeTraceWaypoint> routeWaypoints)
        {
            _fallbackPortalJumps.Clear();

            for (int i = 0; i < routeWaypoints.Count; i++)
            {
                if (!routeWaypoints[i].IsTeleportExit || i <= 0)
                {
                    continue;
                }

                _fallbackPortalJumps.Add(new EscapeTracePortalJump(
                    i - 1,
                    i,
                    routeWaypoints[i - 1].Position,
                    routeWaypoints[i].Position,
                    Direction4.Up,
                    Direction4.Up));
            }
        }

        private void BuildSegments(int bodyPointCount, IReadOnlyList<EscapeTracePortalJump> portalJumps)
        {
            _segments.Clear();

            if (MovementPoints == null || MovementPoints.Length == 0)
            {
                return;
            }

            int segmentStartIndex = 0;
            float segmentStartDistance = MovementDistances[0];

            if (portalJumps != null)
            {
                for (int i = 0; i < portalJumps.Count; i++)
                {
                    EscapeTracePortalJump jump = portalJumps[i];
                    int entryPointIndex = bodyPointCount + jump.EntryWaypointIndex;
                    int exitPointIndex = bodyPointCount + jump.ExitWaypointIndex;

                    if (entryPointIndex < segmentStartIndex ||
                        entryPointIndex < 0 ||
                        exitPointIndex < 0 ||
                        entryPointIndex >= MovementPoints.Length ||
                        exitPointIndex >= MovementPoints.Length)
                    {
                        continue;
                    }

                    AddSegment(segmentStartIndex, entryPointIndex, segmentStartDistance, MovementDistances[entryPointIndex]);
                    segmentStartIndex = exitPointIndex;
                    segmentStartDistance = MovementDistances[exitPointIndex];
                }
            }

            AddSegment(segmentStartIndex, MovementPoints.Length - 1, segmentStartDistance,
                MovementDistances[MovementDistances.Length - 1]);
        }

        private void AddSegment(int startPointIndex, int endPointIndex, float startDistance, float endDistance)
        {
            if (startPointIndex < 0 || endPointIndex < startPointIndex)
            {
                return;
            }

            _segments.Add(new Segment
            {
                StartPointIndex = startPointIndex,
                EndPointIndex = endPointIndex,
                StartDistance = startDistance,
                EndDistance = endDistance
            });
        }

        private Vector3 GetPointAlongSegment(Segment segment, float distance, bool allowAfterEnd,
            bool allowBeforeStart, Vector3 escapeDirection)
        {
            if (allowBeforeStart && distance < segment.StartDistance)
            {
                Vector3 direction = GetDirectionForSegment(segment, escapeDirection, false);
                return MovementPoints[segment.StartPointIndex] - direction * (segment.StartDistance - distance);
            }

            if (allowAfterEnd && distance > segment.EndDistance)
            {
                Vector3 direction = GetFallbackDirection(escapeDirection);
                return MovementPoints[segment.EndPointIndex] + direction * (distance - segment.EndDistance);
            }

            if (distance <= segment.StartDistance)
            {
                return MovementPoints[segment.StartPointIndex];
            }

            if (distance >= segment.EndDistance)
            {
                return MovementPoints[segment.EndPointIndex];
            }

            for (int i = segment.StartPointIndex + 1; i <= segment.EndPointIndex; i++)
            {
                if (distance > MovementDistances[i])
                {
                    continue;
                }

                float segmentDistance = MovementDistances[i] - MovementDistances[i - 1];
                if (segmentDistance <= Mathf.Epsilon)
                {
                    return MovementPoints[i];
                }

                float t = Mathf.InverseLerp(MovementDistances[i - 1], MovementDistances[i], distance);
                return Vector3.Lerp(MovementPoints[i - 1], MovementPoints[i], t);
            }

            return MovementPoints[segment.EndPointIndex];
        }

        private Vector3 GetDirectionForSegment(Segment segment, Vector3 escapeDirection, bool atEnd = false)
        {
            if (atEnd)
            {
                for (int i = segment.EndPointIndex - 1; i >= segment.StartPointIndex; i--)
                {
                    Vector3 direction = MovementPoints[i + 1] - MovementPoints[i];
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        return direction.normalized;
                    }
                }
            }
            else
            {
                for (int i = segment.StartPointIndex; i < segment.EndPointIndex; i++)
                {
                    Vector3 direction = MovementPoints[i + 1] - MovementPoints[i];
                    if (direction.sqrMagnitude > 0.0001f)
                    {
                        return direction.normalized;
                    }
                }
            }

            return GetFallbackDirection(escapeDirection);
        }

        private static Vector3 GetFallbackDirection(Vector3 escapeDirection)
        {
            return escapeDirection.sqrMagnitude > 0f ? escapeDirection.normalized : Vector3.up;
        }

        private static int CompareVisibleBodyChunkOrder(VisibleBodyChunk a, VisibleBodyChunk b)
        {
            if (a.ContainsHead != b.ContainsHead)
            {
                return a.ContainsHead ? -1 : 1;
            }

            return b.SegmentIndex.CompareTo(a.SegmentIndex);
        }
    }
}
