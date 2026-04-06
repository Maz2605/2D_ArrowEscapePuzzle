using System;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.Events;

namespace ArrowGame.Gameplay.Boosters
{
    public class HintBooster : IBooster
    {
        public BoosterType Type => BoosterType.Hint;

        public bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty() && gridLogic.GetOneEscapableArrow() != null;
        }

        public void Execute(GridSystem gridLogic, int targetX, int targetY, Action onComplete)
        {
            ArrowData escapableArrow = gridLogic.GetOneEscapableArrow();
            if (escapableArrow != null)
            {
                EventManager<VisualEventID>.Post(VisualEventID.ShowHintVisual, escapableArrow.ID);
            }
            onComplete?.Invoke();
        }

        BoosterType IBooster.Type => Type;
    }
}