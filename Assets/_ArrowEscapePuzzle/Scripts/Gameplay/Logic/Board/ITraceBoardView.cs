using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public interface ITraceBoardView
    {
        bool IsValidPosition(int x, int y);
        ArrowData GetArrow(int x, int y);
        ArrowModel GetArrowModel(string arrowId);
        SpecialCellSaveData GetSpecialCellAt(int x, int y);
        SpecialCellSaveData ResolveExitPortal(SpecialCellSaveData entryPortal);
    }
}
