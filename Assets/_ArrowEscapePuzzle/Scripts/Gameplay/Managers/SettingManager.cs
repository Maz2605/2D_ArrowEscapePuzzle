using System;
using UnityEngine;
using ArrowGame.Haptic;
using GameCore.Interface;
using GameCore.Audio.Manager;
using GameCore.Data;
using GameCore.Utils.DesignPattern.Singleton; // Import namespace Singleton của bạn
using AudioManager = ArrowGame.Audio.AudioManager;

namespace ArrowGame.Gameplay.Managers
{
    public class SettingManager : Singleton<SettingManager>, IAppService
    {
        private GlobalUserSetting _currentSettings;
        public GlobalUserSetting CurrentSettings 
        { 
            get 
            {
                if (_currentSettings == null) 
                {
                    Debug.LogWarning("[SettingManager] Đang tự động LoadSettings vì Init chưa được gọi!");
                    _currentSettings = GameCore.Data.SaveSystem.Load<GlobalUserSetting>(SETTING_SAVE_KEY) ?? new GlobalUserSetting();
                }
                return _currentSettings;
            }
            private set => _currentSettings = value;
        }
        private const string SETTING_SAVE_KEY = "global_user_setting";

        private readonly int[] _supportedFPS = { 30, 60};

        protected override void Awake()
        {
            base.Awake(); 

            // LoadSettings();
        }
        public void Init()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            CurrentSettings = SaveSystem.Load<GlobalUserSetting>(SETTING_SAVE_KEY) ?? new GlobalUserSetting();
            
            if (Array.IndexOf(_supportedFPS, CurrentSettings.targetFPS) == -1)
            {
                CurrentSettings.targetFPS = 60; 
            }

            ApplySettingsToGame();
        }

        private void SaveSettings()
        {
            SaveSystem.Save(SETTING_SAVE_KEY, CurrentSettings);
            ApplySettingsToGame();
        }

        private void ApplySettingsToGame()
        {
            Debug.Log($"[SettingManager] Applying settings: Music={CurrentSettings.isMusicEnabled}, SFX={CurrentSettings.isSfxEnabled}");
            if (AudioManager.Instance != null)
            {
                Debug.Log("[SettingManager] FORCING MUSIC OFF FOR TEST");
                AudioManager.Instance.SetMusicState(false);
                AudioManager.Instance.SetSfxState(CurrentSettings.isSfxEnabled);
            }

            if (HapticManager.Instance != null)
            {
                HapticManager.Instance.IsVibrationEnabled = CurrentSettings.isVibrationEnabled;
            }

            Application.targetFrameRate = CurrentSettings.targetFPS;
        }

        #region --- Public API ---

        public void ToggleMusic()
        {
            CurrentSettings.isMusicEnabled = !CurrentSettings.isMusicEnabled;
            SaveSettings();
        }

        public void ToggleSFX()
        {
            CurrentSettings.isSfxEnabled = !CurrentSettings.isSfxEnabled;
            SaveSettings();
        }

        public void ToggleVibration()
        {
            CurrentSettings.isVibrationEnabled = !CurrentSettings.isVibrationEnabled;
            SaveSettings();
            
            if (CurrentSettings.isVibrationEnabled && HapticManager.Instance != null)
            {
                HapticManager.Instance.LightVibrateImpact();
            }
        }

        public void ToggleFPS()
        {
            int currentIndex = Array.IndexOf(_supportedFPS, CurrentSettings.targetFPS);
            int nextIndex = (currentIndex + 1) % _supportedFPS.Length;
            
            CurrentSettings.targetFPS = _supportedFPS[nextIndex];
            SaveSettings();
        }

        public void ToggleTheme()
        {
            ThemeManager.Instance.NextTheme();
            CurrentSettings.currentThemeId = ThemeManager.Instance.CurrentTheme.themeId;
            
            SaveSettings();
        }

        #endregion

        
    }
}