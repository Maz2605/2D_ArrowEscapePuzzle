using ArrowGame.Data.LevelProvider;
using UnityEngine;

namespace ArrowGame.Gameplay.Tutorials
{
    public class DefaultTutorialHandler : BaseTutorialHandler
    {
        private int _currentStepIndex = -1;

        public override void OnStepStarted(int stepIndex, TutorialStepConfig step)
        {
            _currentStepIndex = stepIndex;
        }

        public override bool IsGridActionAllowed(Vector2Int gridPos)
        {
            if (_config == null || _config.steps == null || _currentStepIndex < 0 || _currentStepIndex >= _config.steps.Count)
            {
                return true;
            }

            var step = _config.steps[_currentStepIndex];
            
            // Nếu là bước camera (tọa độ âm như -1, -1), cho phép click thoải mái qua màn che
            if (step.targetGridPos.x < 0 || step.targetGridPos.y < 0)
            {
                return true;
            }

            // Chỉ cho phép click nếu trùng khớp với tọa độ mục tiêu 1 hoặc 2
            if (step.targetGridPos == gridPos)
            {
                return true;
            }

            if (step.hasSecondTarget && step.secondTargetGridPos == gridPos)
            {
                return true;
            }

            // Kiểm tra xem có cùng thuộc về một Blocker không
            var gm = ArrowGame.Gameplay.Managers.GameManager.Instance;
            if (gm != null && gm.GridLogic != null)
            {
                var targetCell = gm.GridLogic.GetSpecialCellAt(step.targetGridPos.x, step.targetGridPos.y);
                if (targetCell != null && targetCell.Type == ShareCore.Data.BoardSpecialType.CounterBlock)
                {
                    var clickedCell = gm.GridLogic.GetSpecialCellAt(gridPos.x, gridPos.y);
                    if (clickedCell != null && clickedCell.Type == ShareCore.Data.BoardSpecialType.CounterBlock)
                    {
                        return targetCell.Id == clickedCell.Id || targetCell.Position == clickedCell.Position;
                    }
                }
            }

            return false;
        }
    }
}
