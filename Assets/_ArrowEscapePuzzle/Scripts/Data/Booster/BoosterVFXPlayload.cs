using UnityEngine;

namespace ArrowGame.Data.Booster
{
    public struct BoosterVFXPayload
    {
        public string TargetArrowId;       
        public GameObject VfxPrefab;       
        public float VfxOffsetDistance;   
        public float VisualTotalDuration;  
    }
}