using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    /// <summary>
    /// Logic cho ô Key (chìa khóa).
    /// Ô này không chặn đường di chuyển của mũi tên - mũi tên đi xuyên qua bình thường.
    /// Việc thu thập chìa khóa và mở Mystery Box được xử lý sau
    /// khi mũi tên thoát thành công trong BoardOutcomeProcessor.
    /// </summary>
    public class KeyLogic : ISpecialCellLogic
    {
        public SpecialCellStepResult Evaluate(TraceContext context, SpecialCellSaveData specialCell)
        {
            // Key không chặn đường đi, tiếp tục đi theo hướng hiện tại
            Vector2Int nextPosition = specialCell.Position + context.Direction;
            return SpecialCellStepResult.Continue(nextPosition, context.Direction, context.TravelDirection);
        }
    }
}
