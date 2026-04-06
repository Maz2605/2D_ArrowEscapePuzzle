using System;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.Events;

namespace ArrowGame.Gameplay.Boosters
{
    public class LineGuideBooster : IBooster
    {
        public BoosterType Type => BoosterType.LineGuide;
        private bool _isActive = false; 
        public bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty();
        }
        
        public void Execute(GridSystem gridLogic, int targetX, int targetY, Action onComplete)
        {
            _isActive = !_isActive; // Đảo trạng thái

            EventManager<LogicGameEventID>.Post(LogicGameEventID.LineGuideToggle, _isActive);
            EventManager<VisualEventID>.Post(VisualEventID.ShowDirectionLines, _isActive);

            onComplete?.Invoke();
        }
    }
}