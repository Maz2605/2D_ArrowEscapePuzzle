using UnityEngine;
using ShareCore.Scripts.Data;
using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public class PortalLogic : ISpecialCellLogic
    {
        public static Direction4 GetPortalDirection(SpecialCellSaveData portal)
        {
            return portal != null ? portal.PortalDirection : Direction4.Up;
        }

        public static bool CanEnterPortal(SpecialCellSaveData portal, Vector2Int travelDirection)
        {
            return travelDirection != GetPortalDirection(portal).ToVector2Int();
        }

        public void OnSteppedOn(ref int checkX, ref int checkY, ref Vector2Int direction, EscapeTraceResult result, GridSystem grid, SpecialCellSaveData specialCell)
        {
            int entryWaypointIndex = result.RouteWaypoints.Count - 1;
            Direction4 entryTravelDirection = Direction4Extensions.FromVector(direction);

            // 1. Arrow can enter from the back or either side, but not through the portal's exit face.
            if (!CanEnterPortal(specialCell, direction))
            {
                result.BlockReason = EscapeBlockReason.PortalDirectionMismatch;
                result.FinalDirection = entryTravelDirection;
                checkX = -1;
                checkY = -1;
                return;
            }

            // 2. Tìm cổng đích
            SpecialCellSaveData exitPortal = grid.ResolveExitPortal(specialCell);
            if (exitPortal == null)
            {
                result.BlockReason = EscapeBlockReason.InvalidPortal;
                result.FinalDirection = Direction4Extensions.FromVector(direction);
                checkX = -1;
                checkY = -1;
                return;
            }

            // 3. Ghi nhận Waypoint teleport
            Vector2Int exitPosition = exitPortal.Position;
            Direction4 portalExitDirection = GetPortalDirection(exitPortal);
            if (exitPosition != new Vector2Int(checkX, checkY))
            {
                int exitWaypointIndex = result.AddWaypoint(exitPosition, 0f, true);
                result.AddPortalJump(entryWaypointIndex, exitWaypointIndex, specialCell.Position, exitPosition,
                    entryTravelDirection, portalExitDirection);
            }

            // 4. Hướng xả: Arrow bay ra cùng PortalDirection của cổng đích.
            direction = portalExitDirection.ToVector2Int();
            result.FinalDirection = portalExitDirection;
            
            // 5. Cập nhật tọa độ để tiếp tục Trace theo hướng mới
            checkX = exitPosition.x + direction.x;
            checkY = exitPosition.y + direction.y;
        }
    }
}
