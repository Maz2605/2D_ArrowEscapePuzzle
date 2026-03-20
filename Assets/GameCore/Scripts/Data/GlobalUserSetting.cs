using System;

namespace GameCore.Data
{
    [Serializable]
    public class GlobalUserSetting 
    {
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        
        public bool isSfxEnabled = true;
        public bool isMusicEnabled = true;
        public bool isVibrationEnabled = true;
    }
}
