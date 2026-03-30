using System;
using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Logic;

namespace ArrowGame.Gameplay.Boosters
{
    public interface IBooster
    {
        BoosterType Type { get; }
        
        bool CanUse(GridSystem gridSystem);
        void Execute(GridSystem gridSystem, int targetRow, int targetCol, Action onComplete);
    }
}