using ShareCore.Data;

namespace ShareCore.Interface
{
    public interface ILevelDataProvider
    {
        LevelSaveData GetLevelData(string levelId);
    }
}