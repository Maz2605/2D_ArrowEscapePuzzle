using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public readonly struct SpecialCellStepResult
    {
        public bool Handled { get; }
        public bool ShouldStop { get; }
        public EscapeBlockReason BlockReason { get; }
        public Vector2Int NextPosition { get; }
        public Vector2Int NextDirection { get; }
        public Direction4 FinalDirection { get; }
        public bool RemoveCurrentWaypoint { get; }
        public SpecialCellPortalJump PortalJump { get; }

        private SpecialCellStepResult(bool handled, bool shouldStop, EscapeBlockReason blockReason,
            Vector2Int nextPosition, Vector2Int nextDirection, Direction4 finalDirection,
            bool removeCurrentWaypoint, SpecialCellPortalJump portalJump)
        {
            Handled = handled;
            ShouldStop = shouldStop;
            BlockReason = blockReason;
            NextPosition = nextPosition;
            NextDirection = nextDirection;
            FinalDirection = finalDirection;
            RemoveCurrentWaypoint = removeCurrentWaypoint;
            PortalJump = portalJump;
        }

        public static SpecialCellStepResult Continue(Vector2Int nextPosition, Vector2Int nextDirection,
            Direction4 finalDirection, SpecialCellPortalJump portalJump = null)
        {
            return new SpecialCellStepResult(true, false, EscapeBlockReason.None, nextPosition, nextDirection,
                finalDirection, false, portalJump);
        }

        public static SpecialCellStepResult Stop(EscapeBlockReason blockReason, Direction4 finalDirection,
            bool removeCurrentWaypoint = false)
        {
            return new SpecialCellStepResult(true, true, blockReason, Vector2Int.zero, Vector2Int.zero,
                finalDirection, removeCurrentWaypoint, null);
        }
    }
}
