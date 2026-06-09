using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class SpecialCellPortalJump
    {
        public int EntryWaypointIndex { get; }
        public Vector2Int EntryPosition { get; }
        public Vector2Int ExitPosition { get; }
        public Direction4 EntryTravelDirection { get; }
        public Direction4 ExitTravelDirection { get; }

        public SpecialCellPortalJump(int entryWaypointIndex, Vector2Int entryPosition, Vector2Int exitPosition,
            Direction4 entryTravelDirection, Direction4 exitTravelDirection)
        {
            EntryWaypointIndex = entryWaypointIndex;
            EntryPosition = entryPosition;
            ExitPosition = exitPosition;
            EntryTravelDirection = entryTravelDirection;
            ExitTravelDirection = exitTravelDirection;
        }
    }
}
