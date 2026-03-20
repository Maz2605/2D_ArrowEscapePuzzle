using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;

namespace ShareCore.Data
{
    [Serializable]
    public class LevelSaveData
    {
        [JsonProperty("id")] public string LevelID;

        [JsonProperty("width")] public int Width;
        
        [JsonProperty("height")] public int Height;
        
        [JsonProperty("cells")] public List<CellData> Cells;
        
        public LevelSaveData()
        {
            Cells = new List<CellData>();
        }

        public LevelSaveData(string levelID, int width, int height)
        {
            this.LevelID = levelID;
            this.Width = width;
            this.Height = height;
            Cells = new List<CellData>();
        }
    }
}