using ShareCore.Scripts.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public interface ISpecialCellLogic
    {
        SpecialCellStepResult Evaluate(TraceContext context, SpecialCellSaveData specialCell);
    }
}
