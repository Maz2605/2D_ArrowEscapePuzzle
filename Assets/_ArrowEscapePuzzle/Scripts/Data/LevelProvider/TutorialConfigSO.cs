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

        public TutorialStepConfig()
        {
            showHandPointer = true;
        }

        public TutorialStepConfig(Vector2Int targetGridPos, string tooltipText, bool showHandPointer = true)
        {
            this.targetGridPos = targetGridPos;
            this.tooltipText = tooltipText;
            this.showHandPointer = showHandPointer;
        }

        public TutorialStepConfig Clone()
        {
            return new TutorialStepConfig(targetGridPos, tooltipText, showHandPointer);
        }
    }

    [CreateAssetMenu(fileName = "Level_X", menuName = "ArrowGame/TutorialConfig")]
    public class TutorialConfigSO : ScriptableObject
    {
        public string levelID;
        public List<TutorialStepConfig> steps = new List<TutorialStepConfig>();
    }
}
