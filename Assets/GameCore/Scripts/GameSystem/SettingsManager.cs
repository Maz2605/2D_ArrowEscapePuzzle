using GameCore.Audio.Manager;
using GameCore.Data;
using GameCore.Utils.DesignPattern.Singleton;
using GameCore.Vibration;
using UnityEngine;
using GameCore.Interface;

namespace GameCore.GameSystem
{
    public class SettingsManager : Singleton<SettingsManager>, IAppService
    {
        private const string SAVE_ID = "global_user_settings";
        public GlobalUserSetting CurrentSettings { get; private set; }

        public void Init()
        {
            LoadData();
            ApplyAllSettings();
            Debug.Log("[SettingsManager] Initialized.");
        }

        protected override void Awake()
        {
            base.Awake();
            LoadData();
        }

        private void Start()
        {
            ApplyAllSettings();
        }

        private void LoadData()
        {
            CurrentSettings = SaveSystem.Load<GlobalUserSetting>(SAVE_ID);
            
            if (CurrentSettings == null)
            {
                CurrentSettings = new GlobalUserSetting();
                Debug.Log("Created New Settings (Default)");
            }
            else
            {
                Debug.Log($"Loaded Settings: SFX={CurrentSettings.isSfxEnabled}, Vol={CurrentSettings.sfxVolume}");
            }
        }

        private void ApplyAllSettings()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(CurrentSettings.masterVolume);
                AudioManager.Instance.SetMusicVolume(CurrentSettings.musicVolume);
                AudioManager.Instance.SetSfxVolume(CurrentSettings.sfxVolume);
                
                AudioManager.Instance.SetSfxState(CurrentSettings.isSfxEnabled);
                AudioManager.Instance.SetMusicState(CurrentSettings.isMusicEnabled); 
            }

            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.ToggleVibration(CurrentSettings.isVibrationEnabled);
            }
        }

        private void SaveData()
        {
            SaveSystem.Save(SAVE_ID, CurrentSettings);
        }

        // === PUBLIC API (Gắn vào UI Toggle / Slider) ===

        public void SetVibrationState(bool isOn)
        {
            CurrentSettings.isVibrationEnabled = isOn;
            if (VibrationManager.Instance != null)
                VibrationManager.Instance.ToggleVibration(isOn);
            SaveData();
        }

        public void SetMasterVolume(float value)
        {
            CurrentSettings.masterVolume = value;
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMasterVolume(value);
            SaveData();
        }

        public void SetMusicVolume(float value)
        {
            CurrentSettings.musicVolume = value;
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
            SaveData();
        }
        
        public void SetMusicState(bool isOn)
        {
            CurrentSettings.isMusicEnabled = isOn;
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicState(isOn);
            SaveData();
        }

        public void SetSfxVolume(float value)
        {
            CurrentSettings.sfxVolume = value;
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSfxVolume(value);
            SaveData();
        }
        
        public void SetSfxState(bool isOn)
        {
            CurrentSettings.isSfxEnabled = isOn;
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetSfxState(isOn);
            SaveData();
        }
    }
}