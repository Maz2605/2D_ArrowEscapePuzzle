using GameCore.Interface;
using DG.Tweening;
using UnityEngine;

namespace ArrowGame.Utils
{
    public class DeviceSettingService : MonoBehaviour, IAppService
    {
        [Header("Performance Settings")]
        [SerializeField] private int targetFPS = 60;
        [SerializeField] private bool disableVSync = true;

        [Header("Screen Settings")]
        [SerializeField] private bool keepScreenAwake = true;

        public void Init()
        {
            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }
            Application.targetFrameRate = targetFPS;

            if (keepScreenAwake)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
            
            InitDOTween();
            SetTargetFPS(targetFPS);

            Debug.Log($"[DeviceSettingService] Initialized: Target FPS = {targetFPS}, VSync = {!disableVSync}");
        }
        
        public void SetTargetFPS(int fps)
        {
            Application.targetFrameRate = fps;
            Debug.Log($"[DeviceSettingService] Changed FPS to {fps}");
        }
        
        private void InitDOTween()
        {
            // Kiểm tra tránh Init nhiều lần
            if (!DOTween.instance)
            {
                DOTween.logBehaviour = LogBehaviour.ErrorsOnly; 

                DOTween.Init(true, true, LogBehaviour.ErrorsOnly).SetCapacity(2000, 500);

                DOTween.defaultAutoKill = true; 
                DOTween.defaultRecyclable = false; 
                
                Debug.Log($"[DeviceSettingService] DOTween Initialized. Capacity: 2000/500");
            }
        }
    }
}