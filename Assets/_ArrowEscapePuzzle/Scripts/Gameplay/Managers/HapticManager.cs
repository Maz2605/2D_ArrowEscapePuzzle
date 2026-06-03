using GameCore.Interface;
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

        protected override void Awake()
        {
            // Đảm bảo không destroy khi chuyển scene
            KeepAlive(true);
            base.Awake();
            
            InitHaptics();
        }

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
            _isHardwareSupported = true; // Trong Editor set true để có thể hiện Debug Log
            Debug.Log("<color=#5dade2>[HapticManager] Editor Mode: Debug logging enabled.</color>");
#endif
        }

        public void SetVibrationState(bool enabled)
        {
            IsVibrationEnabled = enabled;
        }

        private bool CanVibrate()
        {
            if (!IsVibrationEnabled) return false;
            
            // Trong Editor thì không check hardware thật, chỉ check cooldown
#if !UNITY_EDITOR
            if(!_isHardwareSupported) return false;
#endif
            
            if (Time.unscaledTime - _lastVibrateTime < hapticCooldown) return false;
            
            _lastVibrateTime = Time.unscaledTime;
            return true;
        }

        // PUBLIC API - SEMANTIC PATTERNS

        public void Success()
        {
            if (CanVibrate())
            {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
                Vibration.VibratePeek(); // Rung nhẹ 2 lần hoặc kiểu success
#else
                Debug.Log("<color=#2ecc71>[HAPTIC:Success]</color> Level Complete / Reward");
#endif
            }
        }

        public void Failure()
        {
            if (CanVibrate())
            {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
                Vibration.VibrateNope(); // Kiểu rung cảnh báo/thất bại
#else
                Debug.Log("<color=#e74c3c>[HAPTIC:Failure]</color> Blocked / Lose");
#endif
            }
        }

        public void Selection()
        {
            if (CanVibrate())
            {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
                Vibration.VibratePop(); // Rung cực nhẹ cho UI
#else
                Debug.Log("<color=#f1c40f>[HAPTIC:Selection]</color> UI Click");
#endif
            }
        }

        public void LightVibrateImpact()
        {
            if (CanVibrate())
            {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
                Vibration.VibratePop();
#else
                Debug.Log("[HAPTIC:Light] Tap Arrow");
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
                Debug.Log("[HAPTIC:Medium] Impact");
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
                Debug.Log("[HAPTIC:Heavy] Major Impact");
#endif
            }
        }
    }
}