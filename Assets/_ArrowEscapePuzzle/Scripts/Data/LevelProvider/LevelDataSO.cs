using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Data.LevelProvider
{
    [CreateAssetMenu(fileName = "LevelDataSO", menuName = "ArrowGame/LevelDataSO")]
    public class LevelDataSO : ScriptableObject
    { 
        public string levelID;
        public int width = 10;
        public int height = 10;
        public LevelDifficulty difficulty = LevelDifficulty.Normal;

        public List<ArrowSaveData> arrows = new List<ArrowSaveData>();
        public List<SpecialCellSaveData> specialCells = new List<SpecialCellSaveData>();
        public List<TutorialStepData> tutorialSteps = new List<TutorialStepData>();
        
        /// <summary>
        /// Chuyển đổi từ ScriptableObject sang đối tượng Data chuẩn để Logic Game sử dụng.
        /// </summary>
        public LevelSaveData ToLevelSaveData()
        {
            var data = new LevelSaveData(levelID, width, height, difficulty);
            data.Arrows = new List<ArrowSaveData>();
            data.SpecialCells = new List<SpecialCellSaveData>();
            data.TutorialSteps = new List<TutorialStepData>();

            if (arrows != null)
            {
                for (int i = 0; i < arrows.Count; i++)
                {
                    ArrowSaveData arrow = arrows[i];
                    if (arrow != null)
                    {
                        data.Arrows.Add(arrow.Clone());
                    }
                }
            }

            if (specialCells != null)
            {
                for (int i = 0; i < specialCells.Count; i++)
                {
                    SpecialCellSaveData specialCell = specialCells[i];
                    if (specialCell != null)
                    {
                        data.SpecialCells.Add(CounterBlockUtility.Clone(specialCell));
                    }
                }
            }

            if (tutorialSteps != null)
            {
                for (int i = 0; i < tutorialSteps.Count; i++)
                {
                    TutorialStepData step = tutorialSteps[i];
                    if (step != null)
                    {
                        data.TutorialSteps.Add(step.Clone());
                    }
                }
            }
            
            return data;
        }
    }
}
