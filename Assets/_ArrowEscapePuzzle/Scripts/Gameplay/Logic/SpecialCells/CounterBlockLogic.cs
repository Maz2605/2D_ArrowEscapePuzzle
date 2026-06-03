using UnityEngine;
using ShareCore.Scripts.Data;
using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public class CounterBlockLogic : ISpecialCellLogic
    {
        public void OnSteppedOn(ref int checkX, ref int checkY, ref Vector2Int direction, EscapeTraceResult result, GridSystem grid, SpecialCellSaveData specialCell)
        {
            result.BlockReason = EscapeBlockReason.CounterBlock;
            result.FinalDirection = Direction4Extensions.FromVector(direction);
            
            // Xóa waypoint vừa thêm (vì đó là ô chứa Blocker)
            // Giúp mũi tên dừng lại ở ô trước đó, giống như khi bị chặn bởi mũi tên khác
            if (result.RouteWaypoints.Count > 0)
            {
                result.VisitedCells.RemoveAt(result.VisitedCells.Count - 1);
                result.RouteWaypoints.RemoveAt(result.RouteWaypoints.Count - 1);
                result.DistanceBeforeStop -= 1;
            }
            
            checkX = -1;
            checkY = -1;
        }
    }
}
