using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    internal enum ArrowLineVisualState
    {
        Idle,
        Spawning,
        Blocked,
        Escaping
    }

    internal sealed class ArrowLineRuntimeState
    {
        public string ArrowId;
        public ArrowLineVisualState CurrentState = ArrowLineVisualState.Idle;
        public Color BaseColor;
        public Color BlockedColor;
        public Color LoseColor;
        public Vector3 EscapeDirection;
        public Vector3 DefaultEscapeDirection;
        public Vector3[] BodyPoints;
        public float CellSize;
        public float TravelDistance;
        public Quaternion CurrentHeadRotation = Quaternion.identity;
        public bool IsMarkedAsWrong;
        public Vector3 CenterPivot;
        public bool IsDirectionLinePersistent;
        public float DirectionLineProgress = 1f;
        public readonly List<Vector2Int> GridPath = new List<Vector2Int>();
        public readonly List<ArrowEndpoint> AvailableEndpoints = new List<ArrowEndpoint>();
        public int PrimaryEndpointPathIndex = -1;
        public int ActiveEndpointPathIndex = -1;
        public EscapeTraceResult ActiveTraceResult;
        public EscapeTraceResult SecondaryGuideTraceResult;

        public void ResetForSpawn()
        {
            CurrentState = ArrowLineVisualState.Idle;
            TravelDistance = 0f;
            IsMarkedAsWrong = false;
            IsDirectionLinePersistent = false;
            DirectionLineProgress = 1f;
            ActiveTraceResult = null;
            SecondaryGuideTraceResult = null;
        }

        public void ResetForDespawn()
        {
            TravelDistance = 0f;
            ActiveTraceResult = null;
            SecondaryGuideTraceResult = null;
            IsDirectionLinePersistent = false;
            DirectionLineProgress = 1f;
            CurrentState = ArrowLineVisualState.Idle;
        }
    }
}
