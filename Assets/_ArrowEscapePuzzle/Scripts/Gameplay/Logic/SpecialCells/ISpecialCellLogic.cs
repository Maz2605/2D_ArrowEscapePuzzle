using UnityEngine;
using ShareCore.Scripts.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public interface ISpecialCellLogic
    {
        void OnSteppedOn(ref int checkX, ref int checkY, ref Vector2Int direction, EscapeTraceResult result, GridSystem grid, SpecialCellSaveData specialCell);
    }
}
