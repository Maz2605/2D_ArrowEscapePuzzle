using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ShareCore.Data;
using UnityEngine;

namespace ShareCore.Scripts.Data
{
    [Serializable]
    public class SpecialCellSaveData
    {
        [JsonProperty("position")] public Vector2Int Position;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BoardSpecialType Type;

        [JsonProperty("portalId")] public string PortalId;

        [JsonProperty("exitDirection")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Direction4 ExitDirection;

        [JsonProperty("counter")] public int Counter;

        public SpecialCellSaveData()
        {
            PortalId = string.Empty;
            ExitDirection = Direction4.Up;
            Counter = 0;
        }

        public SpecialCellSaveData(Vector2Int position, BoardSpecialType type, Direction4 exitDirection,
            string portalId = "", int counter = 0)
        {
            Position = position;
            Type = type;
            ExitDirection = exitDirection;
            PortalId = portalId ?? string.Empty;
            Counter = counter;
        }
    }
}
