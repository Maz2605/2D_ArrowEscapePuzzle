using UnityEngine;
using ShareCore.Scripts.Data;
using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic.SpecialCells
{
    public class RedirectLogic : ISpecialCellLogic
    {
        public void OnSteppedOn(ref int checkX, ref int checkY, ref Vector2Int direction, EscapeTraceResult result, GridSystem grid, SpecialCellSaveData specialCell)
        {
            direction = specialCell.ExitDirection.ToVector2Int();
            result.FinalDirection = specialCell.ExitDirection;
            checkX = specialCell.Position.x + direction.x;
            checkY = specialCell.Position.y + direction.y;
        }
    }
}
