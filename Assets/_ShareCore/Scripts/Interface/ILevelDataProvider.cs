using ShareCore.Data;
using ShareCore.Scripts.Data;

namespace ShareCore.Interface
{
    public interface ILevelDataProvider
    {
        LevelSaveData GetLevelData(string levelId);
    }
}