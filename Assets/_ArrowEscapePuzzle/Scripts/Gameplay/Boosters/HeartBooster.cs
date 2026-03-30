using System;
using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Logic;

namespace ArrowGame.Gameplay.Boosters
{
    public class HeartBooster : IBooster
    {
        public BoosterType Type { get; }
        public bool CanUse(GridSystem gridSystem)
        {
            return gridSystem.IsBoardEmpty();
        }

        public void Execute(GridSystem gridSystem, int targetRow, int targetCol, Action onComplete)
        {
            if (CanUse(gridSystem))
            {
                
            }
        }
    }
}