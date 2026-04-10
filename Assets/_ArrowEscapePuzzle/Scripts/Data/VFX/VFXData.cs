using System;
using UnityEngine;

namespace ArrowGame.Data.VFX
{
    [Serializable]
    public struct VFXConfig
    {
        public GameObject prefab;
        public float offsetDistance; 
        public float duration;       
    }

    public struct VFXRequestPayload
    {
        public string TargetArrowId; 
        public VFXConfig Config;     
    }
    
    public struct VFXChainRequestPayload
    {
        public System.Collections.Generic.List<string> TargetArrowIds;
        public VFXConfig Config;
    }
}