using System;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "HintBooster", menuName = "ArrowGame/Boosters/Hint")]
    public class HintBoosterSO : BoosterConfigSO
    {
        public override bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty() && gridLogic.GetOneEscapableArrow() != null;
        }

        public override void Execute(GridSystem gridLogic, int targetX, int targetY, Sequence seq, Action onComplete)
        {
            ArrowData escapableArrow = gridLogic.GetOneEscapableArrow();
            if (escapableArrow != null)
            {
                EventManager<VisualEventID>.Post(VisualEventID.ShowHintVisual, escapableArrow.ID);
            }
            
            onComplete?.Invoke();
        }
    }
}