using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ShareCore.Data;
using UnityEngine;

namespace ShareCore.Scripts.Data
{
    [Serializable]
    public class SpecialCellSaveData
    {
        [JsonProperty("position")] 
        public Vector2Int Position;

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BoardSpecialType Type;

        [JsonProperty("portalId")] 
        public string PortalId;

        [JsonProperty("id")] 
        public string Id;

        [JsonProperty("exitDirection")]
        [JsonConverter(typeof(StringEnumConverter))]
        public Direction4 ExitDirection;

        [JsonProperty("counter")] 
        public int Counter;

        [JsonProperty("offsets")] 
        public List<Vector2Int> OccupiedOffsets;

        public SpecialCellSaveData()
        {
            Id = string.Empty;
            PortalId = string.Empty;
            ExitDirection = Direction4.Up;
            Counter = 0;
            OccupiedOffsets = new List<Vector2Int>();
        }

        public SpecialCellSaveData(Vector2Int position, BoardSpecialType type, Direction4 exitDirection,
            string portalId = "", int counter = 0, List<Vector2Int> occupiedOffsets = null, string id = "")
        {
            Position = position;
            Type = type;
            ExitDirection = exitDirection;
            PortalId = portalId ?? string.Empty; 
            Id = id ?? string.Empty;
            Counter = counter;
            OccupiedOffsets = occupiedOffsets ?? new List<Vector2Int>();
        }
    }
}
