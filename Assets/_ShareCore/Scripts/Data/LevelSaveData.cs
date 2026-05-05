using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ShareCore.Data;
using UnityEngine;

namespace ShareCore.Scripts.Data
{
    [Serializable]
    public class ArrowSaveData
    {
        [JsonProperty("id")] public string ArrowID;
        [JsonProperty("path")] public List<Vector2Int> Path; 
        [JsonProperty("isHeadFirst")] public bool IsHeadFirst;

        public ArrowSaveData() { Path = new List<Vector2Int>(); }
        
        public ArrowSaveData(string id, List<Vector2Int> path, bool isHeadFirst)
        {
            this.ArrowID = id;
            this.Path = new List<Vector2Int>(path);
            this.IsHeadFirst = isHeadFirst;
        }
    }

    [Serializable]
    public class LevelSaveData
    {
        [JsonProperty("id")] public string LevelID;
        [JsonProperty("width")] public int Width;
        [JsonProperty("height")] public int Height;
        [JsonProperty("difficulty")] public LevelDifficulty Difficulty;
        [JsonProperty("arrows")] public List<ArrowSaveData> Arrows;
        [JsonProperty("specialCells")] public List<SpecialCellSaveData> SpecialCells;

        public LevelSaveData()
        {
            Arrows = new List<ArrowSaveData>();
            SpecialCells = new List<SpecialCellSaveData>();
        }

        public LevelSaveData(string levelID, int width, int height, LevelDifficulty difficulty)
        {
            this.LevelID = levelID;
            this.Width = width;
            this.Height = height;
            this.Difficulty = difficulty;
            Arrows = new List<ArrowSaveData>();
            SpecialCells = new List<SpecialCellSaveData>();
        }
    }
}
