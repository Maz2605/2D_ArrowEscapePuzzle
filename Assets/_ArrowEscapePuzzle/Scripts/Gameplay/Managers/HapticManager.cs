using ArrowGame.Interface;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;

namespace ArrowGame.Haptic
{
    public class HapticManager : Singleton<HapticManager>, IAppService
    {
        [Header("Haptic Settings")]
        [Tooltip("Thời gian tối thiểu giữa 2 lần rung (Chống spam và GC)")]
        [SerializeField] private float hapticCooldown = 0.05f;
        
        private float _lastVibrateTime;

        // // Biến kiểm tra xem phần cứng máy có hỗ trợ rung không
        private bool _isHardwareSupported = false; 

        public bool IsVibrationEnabled { get; set; } = true; 

        // protected override void Awake()
        // {
        //     base.Awake();
        //     
        //     DontDestroyOnLoad(gameObject); 
        //
        //     InitHaptics();
        // }

        public void Init()
        {
            InitHaptics();
        }

        private void InitHaptics()
        {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
            Vibration.Init();
            
            _isHardwareSupported = Vibration.HasVibrator();
            
            Debug.Log($"[HapticManager] Hardware Supported: {_isHardwareSupported}");
#else
            _isHardwareSupported = false; 
            Debug.Log("[HapticManager] Editor Mode: Haptics disabled.");
#endif
        }

        public void SetVibrationState(bool enabled)
        {
            IsVibrationEnabled = enabled;
        }

        private bool CanVibrate()
        {
            if (!IsVibrationEnabled) return false;
            if(!_isHardwareSupported) return false;
            
            if (Time.unscaledTime - _lastVibrateTime < hapticCooldown) return false;
            
            _lastVibrateTime = Time.unscaledTime;
            return true;
        }

        // PUBLIC API

        public void LightVibrateImpact()
        {
            if (CanVibrate())
            {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
                Vibration.VibratePop();
#else
                Debug.Log("[HAPTIC] LightVibrateImpact");
#endif
            }
        }

        public void MediumVibrateImpact()
        {
            if (CanVibrate())
            {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
                Vibration.VibratePeek();
#else
                Debug.Log("[HAPTIC] MediumVibrateImpact");
#endif
            }
        }

        public void HeavyVibrateImpact()
        {
            if (CanVibrate())
            {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
                Vibration.VibrateNope();
#else
                Debug.Log("[HAPTIC] HeavyVibrateImpact");
#endif
            }
        }
    }
}