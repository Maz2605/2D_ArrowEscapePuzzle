using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class TraceContext
    {
        public string ArrowId { get; }
        public ArrowModel MovingModel { get; }
        public ArrowEndpoint ActiveEndpoint { get; }
        public Vector2Int CurrentPosition { get; }
        public Vector2Int Direction { get; }
        public Direction4 TravelDirection { get; }
        public int EntryWaypointIndex { get; }
        public EscapeTraceResult Result { get; }
        public ITraceBoardView Board { get; }

        public TraceContext(string arrowId, ArrowModel movingModel, ArrowEndpoint activeEndpoint,
            Vector2Int currentPosition, Vector2Int direction, int entryWaypointIndex,
            EscapeTraceResult result, ITraceBoardView board)
        {
            ArrowId = arrowId ?? string.Empty;
            MovingModel = movingModel;
            ActiveEndpoint = activeEndpoint;
            CurrentPosition = currentPosition;
            Direction = direction;
            TravelDirection = Direction4Extensions.FromVector(direction);
            EntryWaypointIndex = entryWaypointIndex;
            Result = result;
            Board = board;
        }
    }
}
