using System;
using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Logic;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    public interface IBoosterExecutor
    {
        BoosterType Type { get; }
        bool CanUse(BoosterConfigSO config, GridSystem gridLogic);
        bool IsValidTarget(BoosterConfigSO config, GridSystem gridLogic, Vector2Int gridPosition);
        string GetConfigurationError(BoosterConfigSO config);
        void Execute(BoosterExecutionContext context, Action<bool> onComplete);
    }
}
