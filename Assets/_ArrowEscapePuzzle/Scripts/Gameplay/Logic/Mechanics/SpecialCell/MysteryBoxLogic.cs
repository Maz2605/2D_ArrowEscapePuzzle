using ShareCore.Data;
using ShareCore.Scripts.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    /// <summary>
    /// Logic cho ô Mystery Box (hộp bí ẩn).
    /// Khi đang bị khóa, ô này hoạt động như một bức tường rắn chặn hoàn toàn đường đi.
    /// Sau khi mở khóa (thông qua Key), ô này biến mất và hé lộ ô SpecialCell bên trong.
    /// </summary>
    public class MysteryBoxLogic : ISpecialCellLogic
    {
        public SpecialCellStepResult Evaluate(TraceContext context, SpecialCellSaveData specialCell)
        {
            // Mystery Box chặn hoàn toàn đường đi, giống như một khối tường
            return SpecialCellStepResult.Stop(EscapeBlockReason.MysteryBox, context.TravelDirection,
                removeCurrentWaypoint: true);
        }
    }
}
