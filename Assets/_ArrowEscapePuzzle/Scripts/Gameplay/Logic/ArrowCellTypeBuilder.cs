using System.Collections.Generic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public static class ArrowCellTypeBuilder
    {
        public static List<CellType> BuildLegacyCellTypes(ArrowModel arrowModel)
        {
            List<CellType> cellTypes = new List<CellType>();
            if (arrowModel == null || arrowModel.Path == null || arrowModel.Path.Count == 0) return cellTypes;

            ArrowEndpoint primaryEndpoint = arrowModel.PrimaryEndpoint;
            int headIndex = primaryEndpoint != null ? primaryEndpoint.PathIndex : 0;

            for (int i = 0; i < arrowModel.Path.Count; i++)
            {
                cellTypes.Add(CalculateLegacyCellType(i, arrowModel.Path, headIndex, primaryEndpoint));
            }

            return cellTypes;
        }

        public static CellType GetLegacyHeadCellType(ArrowModel arrowModel)
        {
            ArrowEndpoint primaryEndpoint = arrowModel?.PrimaryEndpoint;
            return primaryEndpoint != null ? ToHeadCellType(primaryEndpoint.ExitDirection) : CellType.ArrowHeadUp;
        }

        public static bool TryGetHeadDirection(CellType type, out Direction4 direction)
        {
            switch (type)
            {
                case CellType.ArrowHeadUp:
                    direction = Direction4.Up;
                    return true;
                case CellType.ArrowHeadDown:
                    direction = Direction4.Down;
                    return true;
                case CellType.ArrowHeadLeft:
                    direction = Direction4.Left;
                    return true;
                case CellType.ArrowHeadRight:
                    direction = Direction4.Right;
                    return true;
                default:
                    direction = Direction4.Up;
                    return false;
            }
        }

        public static bool IsHeadType(CellType type)
        {
            return type == CellType.ArrowHeadUp || type == CellType.ArrowHeadDown ||
                   type == CellType.ArrowHeadLeft || type == CellType.ArrowHeadRight;
        }

        public static CellType ToHeadCellType(Direction4 direction)
        {
            return direction switch
            {
                Direction4.Up => CellType.ArrowHeadUp,
                Direction4.Down => CellType.ArrowHeadDown,
                Direction4.Left => CellType.ArrowHeadLeft,
                Direction4.Right => CellType.ArrowHeadRight,
                _ => CellType.ArrowHeadUp
            };
        }

        private static CellType ToTailCellType(Direction4 direction)
        {
            return direction switch
            {
                Direction4.Up => CellType.ArrowTailUp,
                Direction4.Down => CellType.ArrowTailDown,
                Direction4.Left => CellType.ArrowTailLeft,
                Direction4.Right => CellType.ArrowTailRight,
                _ => CellType.ArrowTailUp
            };
        }

        private static CellType CalculateLegacyCellType(int index, IReadOnlyList<Vector2Int> path, int headIndex,
            ArrowEndpoint primaryEndpoint)
        {
            if (path.Count == 1)
            {
                Direction4 singleDirection = primaryEndpoint != null ? primaryEndpoint.ExitDirection : Direction4.Up;
                return ToHeadCellType(singleDirection);
            }

            int tailIndex = headIndex == 0 ? path.Count - 1 : 0;
            Vector2Int current = path[index];

            if (index == headIndex)
            {
                return ToHeadCellType(primaryEndpoint != null
                    ? primaryEndpoint.ExitDirection
                    : BuildDirectionFromNeighbor(path, index));
            }

            if (index == tailIndex)
            {
                return ToTailCellType(BuildDirectionFromNeighbor(path, index));
            }

            Vector2Int prev = path[index - 1];
            Vector2Int next = path[index + 1];

            if (prev.x == next.x) return CellType.ArrowBodyVertical;
            if (prev.y == next.y) return CellType.ArrowBodyHorizontal;

            bool hasUp = prev.y > current.y || next.y > current.y;
            bool hasDown = prev.y < current.y || next.y < current.y;
            bool hasLeft = prev.x < current.x || next.x < current.x;
            bool hasRight = prev.x > current.x || next.x > current.x;

            if (hasUp && hasRight) return CellType.ArrowCurveTopRight;
            if (hasUp && hasLeft) return CellType.ArrowCurveTopLeft;
            if (hasDown && hasRight) return CellType.ArrowCurveBottomRight;
            return CellType.ArrowCurveBottomLeft;
        }

        private static Direction4 BuildDirectionFromNeighbor(IReadOnlyList<Vector2Int> path, int endpointIndex)
        {
            Vector2Int current = path[endpointIndex];
            int neighborIndex = endpointIndex == 0 ? 1 : path.Count - 2;
            Vector2Int direction = current - path[neighborIndex];
            return Direction4Extensions.FromVector(direction);
        }
    }
}
