using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ShareCore.Data
{
    [Serializable]
    public class CellData
    {
        [JsonProperty("x")] public int x;
        
        [JsonProperty("y")] public int y;
        
        [JsonProperty("type")] 
        [JsonConverter(typeof(StringEnumConverter))]
        public CellType type;
        
        [JsonProperty("id")]
        public string arrowID;

        public CellData()
        {
            
        }
        
        public  CellData(int x, int y, CellType type, string arrowID)
        {
            this.x = x;
            this.y = y;
            this.type = type;
            this.arrowID = arrowID;
        }
    }
}