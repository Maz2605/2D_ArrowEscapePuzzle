using System;
using UnityEngine.Serialization;

namespace GameCore.Data
{
    [Serializable]
    public class BaseGameData 
    {
        [FormerlySerializedAs("GameId")] public string gameId;
        [FormerlySerializedAs("HightScore")] public int hightScore;
        [FormerlySerializedAs("LastPlayedTime")] public long lastPlayedTime;

        public BaseGameData()
        {
            hightScore = 0;
            lastPlayedTime = DateTime.Now.Ticks;
        }
    }
}
