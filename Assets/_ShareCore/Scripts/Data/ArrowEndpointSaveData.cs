using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ShareCore.Data;

namespace ShareCore.Scripts.Data
{
    [Serializable]
    public class ArrowEndpointSaveData
    {
        [JsonProperty("pathIndex")]
        public int PathIndex;

        [JsonProperty("exitDirection")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Direction4 ExitDirection;

        [JsonProperty("isPrimary")]
        public bool IsPrimary;

        public ArrowEndpointSaveData()
        {
            ExitDirection = Direction4.Up;
        }

        public ArrowEndpointSaveData(int pathIndex, Direction4 exitDirection, bool isPrimary = false)
        {
            PathIndex = pathIndex;
            ExitDirection = exitDirection;
            IsPrimary = isPrimary;
        }

        public ArrowEndpointSaveData Clone()
        {
            return new ArrowEndpointSaveData(PathIndex, ExitDirection, IsPrimary);
        }
    }
}
