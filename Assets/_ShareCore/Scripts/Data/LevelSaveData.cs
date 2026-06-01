using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        [JsonProperty("endpoints")] public List<ArrowEndpointSaveData> Endpoints;
        [JsonProperty("linkGroupId")] public string LinkGroupId;
        [JsonProperty("topologyType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ArrowTopologyType TopologyType;

        public ArrowSaveData()
        {
            Path = new List<Vector2Int>();
            Endpoints = new List<ArrowEndpointSaveData>();
            LinkGroupId = string.Empty;
            TopologyType = ArrowTopologyType.SingleHeadSingleTail;
        }

        public ArrowSaveData(string id, List<Vector2Int> path, bool isHeadFirst)
        {
            ArrowID = id;
            Path = path != null ? new List<Vector2Int>(path) : new List<Vector2Int>();
            IsHeadFirst = isHeadFirst;
            Endpoints = new List<ArrowEndpointSaveData>();
            LinkGroupId = string.Empty;
            TopologyType = ArrowTopologyType.SingleHeadSingleTail;
        }

        public ArrowSaveData(string id, List<Vector2Int> path, bool isHeadFirst,
            List<ArrowEndpointSaveData> endpoints, string linkGroupId, ArrowTopologyType topologyType)
        {
            ArrowID = id;
            Path = path != null ? new List<Vector2Int>(path) : new List<Vector2Int>();
            IsHeadFirst = isHeadFirst;
            Endpoints = CloneEndpoints(endpoints);
            LinkGroupId = linkGroupId ?? string.Empty;
            TopologyType = topologyType;
        }

        public ArrowSaveData Clone()
        {
            return new ArrowSaveData(ArrowID, Path, IsHeadFirst, Endpoints, LinkGroupId, TopologyType);
        }

        private static List<ArrowEndpointSaveData> CloneEndpoints(List<ArrowEndpointSaveData> endpoints)
        {
            List<ArrowEndpointSaveData> clones = new List<ArrowEndpointSaveData>();
            if (endpoints == null) return clones;

            for (int i = 0; i < endpoints.Count; i++)
            {
                ArrowEndpointSaveData endpoint = endpoints[i];
                if (endpoint != null)
                {
                    clones.Add(endpoint.Clone());
                }
            }

            return clones;
        }
    }

    [Serializable]
    public class TutorialStepData
    {
        [JsonProperty("targetGridPos")] public Vector2Int TargetGridPos;
        [JsonProperty("tooltipText")] public string TooltipText;
        [JsonProperty("showHandPointer")] public bool ShowHandPointer;

        public TutorialStepData()
        {
            ShowHandPointer = true;
        }

        public TutorialStepData(Vector2Int targetGridPos, string tooltipText, bool showHandPointer = true)
        {
            TargetGridPos = targetGridPos;
            TooltipText = tooltipText;
            ShowHandPointer = showHandPointer;
        }

        public TutorialStepData Clone()
        {
            return new TutorialStepData(TargetGridPos, TooltipText, ShowHandPointer);
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
        [JsonProperty("tutorialSteps")] public List<TutorialStepData> TutorialSteps;

        public LevelSaveData()
        {
            Arrows = new List<ArrowSaveData>();
            SpecialCells = new List<SpecialCellSaveData>();
            TutorialSteps = new List<TutorialStepData>();
        }

        public LevelSaveData(string levelID, int width, int height, LevelDifficulty difficulty)
        {
            LevelID = levelID;
            Width = width;
            Height = height;
            Difficulty = difficulty;
            Arrows = new List<ArrowSaveData>();
            SpecialCells = new List<SpecialCellSaveData>();
            TutorialSteps = new List<TutorialStepData>();
        }

        public LevelSaveData Clone()
        {
            LevelSaveData clone = new LevelSaveData(LevelID, Width, Height, Difficulty);

            if (Arrows != null)
            {
                for (int i = 0; i < Arrows.Count; i++)
                {
                    ArrowSaveData arrow = Arrows[i];
                    if (arrow != null)
                    {
                        clone.Arrows.Add(arrow.Clone());
                    }
                }
            }

            if (SpecialCells != null)
            {
                for (int i = 0; i < SpecialCells.Count; i++)
                {
                    SpecialCellSaveData specialCell = SpecialCells[i];
                    if (specialCell != null)
                    {
                        clone.SpecialCells.Add(CounterBlockUtility.Clone(specialCell));
                    }
                }
            }

            if (TutorialSteps != null)
            {
                for (int i = 0; i < TutorialSteps.Count; i++)
                {
                    TutorialStepData step = TutorialSteps[i];
                    if (step != null)
                    {
                        clone.TutorialSteps.Add(step.Clone());
                    }
                }
            }

            return clone;
        }
    }
}
