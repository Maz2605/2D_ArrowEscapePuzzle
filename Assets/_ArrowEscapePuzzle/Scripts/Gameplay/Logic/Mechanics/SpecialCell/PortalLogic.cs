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

        public SpecialCellStepResult Evaluate(TraceContext context, SpecialCellSaveData specialCell)
        {
            // 1. Arrow can enter from the back or either side, but not through the portal's exit face.
            if (!CanEnterPortal(specialCell, context.Direction))
            {
                return SpecialCellStepResult.Stop(EscapeBlockReason.PortalDirectionMismatch,
                    context.TravelDirection);
            }

            // 2. Tìm cổng đích
            SpecialCellSaveData exitPortal = context.Board.ResolveExitPortal(specialCell);
            if (exitPortal == null)
            {
                return SpecialCellStepResult.Stop(EscapeBlockReason.InvalidPortal,
                    context.TravelDirection);
            }

            // 3. Ghi nhận Waypoint teleport
            Vector2Int exitPosition = exitPortal.Position;
            Direction4 portalExitDirection = GetPortalDirection(exitPortal);
            SpecialCellPortalJump portalJump = null;
            if (exitPosition != context.CurrentPosition)
            {
                portalJump = new SpecialCellPortalJump(context.EntryWaypointIndex, specialCell.Position, exitPosition,
                    context.TravelDirection, portalExitDirection);
            }

            // 4. Hướng xả: Arrow bay ra cùng PortalDirection của cổng đích.
            Vector2Int nextDirection = portalExitDirection.ToVector2Int();

            // 5. Cập nhật tọa độ để tiếp tục Trace theo hướng mới
            Vector2Int nextPosition = exitPosition + nextDirection;
            return SpecialCellStepResult.Continue(nextPosition, nextDirection, portalExitDirection, portalJump);
        }
    }
}
