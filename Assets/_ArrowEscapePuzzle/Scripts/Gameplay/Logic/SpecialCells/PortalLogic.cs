using UnityEngine;
using ShareCore.Scripts.Data;
using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public class PortalLogic : ISpecialCellLogic
    {
        public void OnSteppedOn(ref int checkX, ref int checkY, ref Vector2Int direction, EscapeTraceResult result, GridSystem grid, SpecialCellSaveData specialCell)
        {
            SpecialCellSaveData exitPortal = grid.ResolveExitPortal(specialCell);
            if (exitPortal == null)
            {
                result.BlockReason = EscapeBlockReason.InvalidPortal;
                result.FinalDirection = Direction4Extensions.FromVector(direction);
                // Đánh dấu dừng lại bằng cách đưa tọa độ ra ngoài biên
                checkX = -1;
                checkY = -1;
                return;
            }

            Vector2Int exitPosition = exitPortal.Position;
            if (exitPosition != new Vector2Int(checkX, checkY))
            {
                result.AddWaypoint(exitPosition, 0f, true);
            }

            direction = exitPortal.ExitDirection.ToVector2Int();
            result.FinalDirection = exitPortal.ExitDirection;
            checkX = exitPosition.x + direction.x;
            checkY = exitPosition.y + direction.y;
        }
    }
}
