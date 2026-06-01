using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Data.LevelProvider
{
    [CreateAssetMenu(fileName = "TutorialInformation", menuName = "ArrowGame/TutorialInformation")]
    public class TutorialInformation : ScriptableObject
    {
        [SerializeField] private List<TutorialConfigSO> tutorials = new List<TutorialConfigSO>();

        public TutorialConfigSO GetTutorialForLevel(string levelID)
        {
            if (tutorials == null) return null;
            return tutorials.Find(t => t != null && t.levelID == levelID);
        }
    }
}
