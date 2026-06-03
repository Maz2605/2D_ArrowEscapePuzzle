using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Data.Events
{
    public enum ArrowPathVisualTriggerType
    {
        Normal,
        PortalEntry,
        PortalExit
    }

    public readonly struct ArrowPathVisualTrigger
    {
        public Vector2Int GridPos { get; }
        public ArrowPathVisualTriggerType TriggerType { get; }
        public Direction4 TravelDirection { get; }
        public int JumpIndex { get; }
        public int SegmentIndex { get; }

        public ArrowPathVisualTrigger(Vector2Int gridPos, ArrowPathVisualTriggerType triggerType, Direction4 travelDirection,
            int jumpIndex = -1, int segmentIndex = -1)
        {
            GridPos = gridPos;
            TriggerType = triggerType;
            TravelDirection = travelDirection;
            JumpIndex = jumpIndex;
            SegmentIndex = segmentIndex;
        }
    }
}
