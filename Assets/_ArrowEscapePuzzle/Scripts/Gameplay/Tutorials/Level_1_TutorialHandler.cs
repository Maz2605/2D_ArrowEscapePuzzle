using ArrowGame.Data.LevelProvider;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Popups;
using UnityEngine;

namespace ArrowGame.Gameplay.Tutorials
{
    public class Level_1_TutorialHandler : BaseTutorialHandler
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

            // Chỉ cho phép click nếu trùng khớp với tọa độ mục tiêu
            return _config.steps[_currentStepIndex].targetGridPos == gridPos;
        }
    }
}
