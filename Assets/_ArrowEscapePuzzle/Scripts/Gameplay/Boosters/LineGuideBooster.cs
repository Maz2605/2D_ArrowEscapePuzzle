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
    public class LineGuideBooster: BoosterConfigSO
    {
        public BoosterType Type => BoosterType.LineGuide;
        private bool _isActive = false; 
        public override bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty();
        }
        
        public override void Execute(GridSystem gridLogic, int targetX, int targetY, Sequence seq, Action onComplete)
        {
            _isActive = !_isActive; // Đảo trạng thái

            EventManager<LogicGameEventID>.Post(LogicGameEventID.LineGuideToggle, _isActive);
            EventManager<VisualEventID>.Post(VisualEventID.ShowDirectionLines, _isActive);

            onComplete?.Invoke();
        }
    }
}