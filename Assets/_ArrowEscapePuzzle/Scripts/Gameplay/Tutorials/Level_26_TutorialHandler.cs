using ArrowGame.Data.LevelProvider;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
using UnityEngine;

namespace ArrowGame.Gameplay.Tutorials
{
    public class Level_26_TutorialHandler : DefaultTutorialHandler
    {
        private int _currentStepIndex = -1;

        public override void OnStepStarted(int stepIndex, TutorialStepConfig step)
        {
            base.OnStepStarted(stepIndex, step);
            _currentStepIndex = stepIndex;
        }

        public override bool IsGridActionAllowed(Vector2Int gridPos)
        {
            if (_config == null || _config.steps == null || _currentStepIndex < 0 || _currentStepIndex >= _config.steps.Count)
            {
                return true;
            }

            var step = _config.steps[_currentStepIndex];

            // Cho phép click nếu click trúng đích
            if (base.IsGridActionAllowed(gridPos))
            {
                return true;
            }

            // Kiểm tra click vào Linked Arrow cùng nhóm
            var gm = GameManager.Instance;
            if (gm != null && gm.GridLogic != null)
            {
                var gridLogic = gm.GridLogic;
                var targetArrowData = gridLogic.GetArrow(step.targetGridPos.x, step.targetGridPos.y);
                var clickedArrowData = gridLogic.GetArrow(gridPos.x, gridPos.y);

                if (targetArrowData != null && clickedArrowData != null && !string.IsNullOrEmpty(targetArrowData.ID) && !string.IsNullOrEmpty(clickedArrowData.ID))
                {
                    var targetModel = gridLogic.GetArrowModel(targetArrowData.ID);
                    var clickedModel = gridLogic.GetArrowModel(clickedArrowData.ID);

                    if (targetModel != null && clickedModel != null)
                    {
                        if (!string.IsNullOrEmpty(targetModel.LinkGroupId) && targetModel.LinkGroupId == clickedModel.LinkGroupId)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override bool TryGetCustomWorldPosition(int stepIndex, TutorialStepConfig step, out Vector3 worldPos, out float highlightSize, out Vector3 secondWorldPos, out float secondHighlightSize)
        {
            worldPos = Vector3.zero;
            highlightSize = 120f;
            secondWorldPos = Vector3.zero;
            secondHighlightSize = 120f;

            var gm = GameManager.Instance;
            if (gm != null && gm.GridLogic != null && gm.CurrentGridView != null)
            {
                var gridLogic = gm.GridLogic;
                var targetArrowData = gridLogic.GetArrow(step.targetGridPos.x, step.targetGridPos.y);

                if (targetArrowData != null && !string.IsNullOrEmpty(targetArrowData.ID))
                {
                    var targetModel = gridLogic.GetArrowModel(targetArrowData.ID);
                    if (targetModel != null && !string.IsNullOrEmpty(targetModel.LinkGroupId))
                    {
                        Vector2 sumPos = Vector2.zero;
                        int count = 0;

                        foreach (var arrowModel in gridLogic.ArrowModels.Values)
                        {
                            if (arrowModel.LinkGroupId == targetModel.LinkGroupId)
                            {
                                if (arrowModel.PrimaryEndpoint != null)
                                {
                                    sumPos += new Vector2(arrowModel.PrimaryEndpoint.Position.x, arrowModel.PrimaryEndpoint.Position.y);
                                    count++;
                                }
                            }
                        }

                        if (count > 0)
                        {
                            Vector2 centerGridPos = sumPos / count;
                            worldPos = gm.CurrentGridView.GetCellWorldPosition(centerGridPos);
                            // Set a large highlight size to cover all 3 arrow heads
                            highlightSize = 280f;
                            return true;
                        }
                    }
                }
            }

            return base.TryGetCustomWorldPosition(stepIndex, step, out worldPos, out highlightSize, out secondWorldPos, out secondHighlightSize);
        }
    }
}
