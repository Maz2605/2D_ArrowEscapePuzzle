using System;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "LineGuideBooster", menuName = "ArrowGame/Boosters/LineGuide")]
    public class LineGuideBooster : BoosterConfigSO
    {
        public override bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty();
        }
        
        public void ExecuteWithState(bool newState, Action onComplete)
        {
            EventManager<LogicGameEventID>.Post(LogicGameEventID.LineGuideToggle, newState);
            EventManager<VisualEventID>.Post(VisualEventID.ShowDirectionLines, newState);

            onComplete?.Invoke();
        }

        public override void Execute(GridSystem gridLogic, int targetX, int targetY, Sequence seq, Action onComplete)
        {
            Debug.LogWarning("[LineGuideBooster] Nên dùng ExecuteWithState cho loại Booster Toggle!");
            onComplete?.Invoke();
        }
    }
}