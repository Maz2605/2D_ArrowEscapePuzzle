using UnityEngine;
using ShareCore.Scripts.Data;
using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public class CounterBlockLogic : ISpecialCellLogic
    {
        public SpecialCellStepResult Evaluate(TraceContext context, SpecialCellSaveData specialCell)
        {
            return SpecialCellStepResult.Stop(EscapeBlockReason.CounterBlock, context.TravelDirection,
                removeCurrentWaypoint: true);
        }
    }
}
