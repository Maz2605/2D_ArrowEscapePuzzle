using System.Collections.Generic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public enum EscapeBlockReason
    {
        None,
        OtherArrow,
        Loop,
        InvalidPortal,
        PortalDirectionMismatch,
        CounterBlock
    }

    public sealed class EscapeTraceWaypoint
    {
        public Vector2Int Position;
        public float StepCost;
        public bool IsTeleportExit;

        public EscapeTraceWaypoint(Vector2Int position, float stepCost, bool isTeleportExit = false)
        {
            Position = position;
            StepCost = stepCost;
            IsTeleportExit = isTeleportExit;
        }
    }

    public sealed class EscapeTracePortalJump
    {
        public int EntryWaypointIndex;
        public int ExitWaypointIndex;
        public Vector2Int EntryPosition;
        public Vector2Int ExitPosition;
        public Direction4 EntryTravelDirection;
        public Direction4 ExitTravelDirection;

        public EscapeTracePortalJump(int entryWaypointIndex, int exitWaypointIndex, Vector2Int entryPosition,
            Vector2Int exitPosition, Direction4 entryTravelDirection, Direction4 exitTravelDirection)
        {
            EntryWaypointIndex = entryWaypointIndex;
            ExitWaypointIndex = exitWaypointIndex;
            EntryPosition = entryPosition;
            ExitPosition = exitPosition;
            EntryTravelDirection = entryTravelDirection;
            ExitTravelDirection = exitTravelDirection;
        }
    }

    public sealed class EscapeTraceResult
    {
        public string ArrowId;
        public string StartEndpointKey;
        public int StartPathIndex;
        public string ActivationGroupKey;
        public bool CanEscape;
        public EscapeBlockReason BlockReason;
        public List<Vector2Int> VisitedCells = new List<Vector2Int>();
        public List<EscapeTraceWaypoint> RouteWaypoints = new List<EscapeTraceWaypoint>();
        public List<EscapeTracePortalJump> PortalJumps = new List<EscapeTracePortalJump>();
        public Direction4 FinalDirection;
        public int DistanceBeforeStop;
        public string BlockerId;

        public EscapeTraceResult(string arrowId, Direction4 initialDirection)
        {
            ArrowId = arrowId;
            FinalDirection = initialDirection;
            BlockReason = EscapeBlockReason.None;
            StartEndpointKey = string.Empty;
            StartPathIndex = -1;
            ActivationGroupKey = string.Empty;
        }

        public EscapeTraceResult(string arrowId, Direction4 initialDirection, string startEndpointKey, int startPathIndex)
            : this(arrowId, initialDirection)
        {
            StartEndpointKey = startEndpointKey ?? string.Empty;
            StartPathIndex = startPathIndex;
        }

        public int AddWaypoint(Vector2Int position, float stepCost, bool isTeleportExit = false)
        {
            VisitedCells.Add(position);
            RouteWaypoints.Add(new EscapeTraceWaypoint(position, stepCost, isTeleportExit));
            DistanceBeforeStop += Mathf.RoundToInt(stepCost);
            return RouteWaypoints.Count - 1;
        }

        public void AddPortalJump(int entryWaypointIndex, int exitWaypointIndex, Vector2Int entryPosition,
            Vector2Int exitPosition, Direction4 entryTravelDirection, Direction4 exitTravelDirection)
        {
            PortalJumps.Add(new EscapeTracePortalJump(entryWaypointIndex, exitWaypointIndex, entryPosition,
                exitPosition, entryTravelDirection, exitTravelDirection));
        }
    }
}
