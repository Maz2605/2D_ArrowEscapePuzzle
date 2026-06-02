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

        public TutorialStepConfig()
        {
            showHandPointer = true;
            blockOutsideClick = true;
        }

        public TutorialStepConfig(Vector2Int targetGridPos, string tooltipText, bool showHandPointer = true, bool blockOutsideClick = true)
        {
            this.targetGridPos = targetGridPos;
            this.tooltipText = tooltipText;
            this.showHandPointer = showHandPointer;
            this.blockOutsideClick = blockOutsideClick;
        }

        public TutorialStepConfig Clone()
        {
            return new TutorialStepConfig(targetGridPos, tooltipText, showHandPointer, blockOutsideClick);
        }
    }

    [CreateAssetMenu(fileName = "Level_X", menuName = "ArrowGame/TutorialConfig")]
    public class TutorialConfigSO : ScriptableObject
    {
        public string levelID;
        public List<TutorialStepConfig> steps = new List<TutorialStepConfig>();
    }
}
