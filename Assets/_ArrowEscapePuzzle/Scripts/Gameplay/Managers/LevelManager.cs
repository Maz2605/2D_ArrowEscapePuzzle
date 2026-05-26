    using ShareCore.Data;
using ShareCore.Interface;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    public class LevelManager : MonoBehaviour
    {
        [Header("Dependencies")] 
        [SerializeField] private MonoBehaviour dataProviderObject;
        private ILevelDataProvider _dataProvider;

        [Header("Settings")] 
        [SerializeField] private int maxLevelCount = 50;

        private void Awake()
        {
            _dataProvider = dataProviderObject as ILevelDataProvider;
        }

        public LevelSaveData LoadCurrentLevelMap()
        {
            int currentLevel = DataManager.Instance.GetActiveLevel(); 
            int playLevelIndex = CalculateActualLevelIndex(currentLevel);

            string levelID = $"Level_{playLevelIndex}";
            LevelSaveData data = _dataProvider.GetLevelData(levelID);

            if (data == null)
            {
                Debug.LogWarning($"[LevelManager] Fallback Level_1 do thiếu data cho {levelID}!");
                data = _dataProvider.GetLevelData("Level_1");
            }

            return data;
        }

        private int CalculateActualLevelIndex(int currentLevel)
        {
            if (currentLevel <= maxLevelCount) return currentLevel;
            
            int loopStart = Mathf.Max(1, maxLevelCount / 2);
            Random.InitState(currentLevel); 
            return Random.Range(loopStart, maxLevelCount + 1);
        }
    }
}