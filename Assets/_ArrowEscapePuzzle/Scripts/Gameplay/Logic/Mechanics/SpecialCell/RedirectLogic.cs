using UnityEngine;
using ShareCore.Scripts.Data;
using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public class RedirectLogic : ISpecialCellLogic
    {
        public SpecialCellStepResult Evaluate(TraceContext context, SpecialCellSaveData specialCell)
        {
            Vector2Int nextDirection = specialCell.ExitDirection.ToVector2Int();
            Vector2Int nextPosition = specialCell.Position + nextDirection;
            return SpecialCellStepResult.Continue(nextPosition, nextDirection, specialCell.ExitDirection);
        }
    }
}
