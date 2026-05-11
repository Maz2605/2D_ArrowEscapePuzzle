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

    public sealed class EscapeTraceResult
    {
        public string ArrowId;
        public bool CanEscape;
        public EscapeBlockReason BlockReason;
        public List<Vector2Int> VisitedCells = new List<Vector2Int>();
        public List<EscapeTraceWaypoint> RouteWaypoints = new List<EscapeTraceWaypoint>();
        public Direction4 FinalDirection;
        public int DistanceBeforeStop;
        public string BlockerId;

        public EscapeTraceResult(string arrowId, Direction4 initialDirection)
        {
            ArrowId = arrowId;
            FinalDirection = initialDirection;
            BlockReason = EscapeBlockReason.None;
        }

        public void AddWaypoint(Vector2Int position, float stepCost, bool isTeleportExit = false)
        {
            VisitedCells.Add(position);
            RouteWaypoints.Add(new EscapeTraceWaypoint(position, stepCost, isTeleportExit));
            DistanceBeforeStop += Mathf.RoundToInt(stepCost);
        }
    }
}
