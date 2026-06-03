using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Data.LevelProvider
{
    [Serializable]
    public class TutorialStepConfig
    {
        public Vector2Int targetGridPos;
        public string tooltipText;
        public bool showHandPointer = true;
        public bool blockOutsideClick = true;
        public bool hasSecondTarget;
        public Vector2Int secondTargetGridPos;

        public TutorialStepConfig()
        {
            showHandPointer = true;
            blockOutsideClick = true;
            hasSecondTarget = false;
        }

        public TutorialStepConfig(Vector2Int targetGridPos, string tooltipText, bool showHandPointer = true, bool blockOutsideClick = true, bool hasSecondTarget = false, Vector2Int secondTargetGridPos = default)
        {
            this.targetGridPos = targetGridPos;
            this.tooltipText = tooltipText;
            this.showHandPointer = showHandPointer;
            this.blockOutsideClick = blockOutsideClick;
            this.hasSecondTarget = hasSecondTarget;
            this.secondTargetGridPos = secondTargetGridPos;
        }

        public TutorialStepConfig Clone()
        {
            return new TutorialStepConfig(targetGridPos, tooltipText, showHandPointer, blockOutsideClick, hasSecondTarget, secondTargetGridPos);
        }
    }

    [CreateAssetMenu(fileName = "Level_X", menuName = "ArrowGame/TutorialConfig")]
    public class TutorialConfigSO : ScriptableObject
    {
        public string levelID;
        /// <summary>
        /// Tên class Handler tùy chỉnh (chỉ tên class, không cần namespace).
        /// Nếu để trống, hệ thống sẽ tự tìm theo convention: [LevelID]_TutorialHandler.
        /// Ví dụ: "CameraInputTutorialHandler"
        /// </summary>
        public string handlerTypeName;
        public List<TutorialStepConfig> steps = new List<TutorialStepConfig>();
    }
}
