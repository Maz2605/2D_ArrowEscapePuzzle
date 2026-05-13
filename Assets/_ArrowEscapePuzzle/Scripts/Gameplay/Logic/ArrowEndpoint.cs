using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class ArrowEndpoint
    {
        public string EndpointKey { get; }
        public int PathIndex { get; }
        public Vector2Int Position { get; }
        public Direction4 ExitDirection { get; }
        public bool IsPrimary { get; }

        public ArrowEndpoint(int pathIndex, Vector2Int position, Direction4 exitDirection, bool isPrimary)
        {
            EndpointKey = $"path:{pathIndex}";
            PathIndex = pathIndex;
            Position = position;
            ExitDirection = exitDirection;
            IsPrimary = isPrimary;
        }
    }
}
