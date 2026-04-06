using ArrowGame.Gameplay.Managers;
using ArrowGame.Haptic;
using GameCore.Audio.Manager;
using GameCore.Data;
using AudioManager = ArrowGame.Audio.AudioManager;

namespace ArrowGame.UI.Base
{
    public abstract class BaseSetting : BasePopup 
    {
        protected GlobalUserSetting CurrentSettings;
        private const string SETTING_SAVE_KEY = "global_user_setting";

        protected override void Awake()
        {
            base.Awake(); 
            LoadSettings();
        }

        protected void LoadSettings()
        {
            CurrentSettings = SaveSystem.Load<GlobalUserSetting>(SETTING_SAVE_KEY); 
            ApplySettingsToGame(); 
            UpdateUIVisuals();
        }

        protected void SaveSettings()
        {
            SaveSystem.Save(SETTING_SAVE_KEY, CurrentSettings); 
            ApplySettingsToGame();
        }

        protected abstract void UpdateUIVisuals();

        protected virtual void ApplySettingsToGame()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicState(CurrentSettings.isMusicEnabled); 
                AudioManager.Instance.SetSfxState(CurrentSettings.isSfxEnabled); 
            
                AudioManager.Instance.SetMasterVolume(CurrentSettings.masterVolume); 
                AudioManager.Instance.SetMusicVolume(CurrentSettings.musicVolume); 
                AudioManager.Instance.SetSfxVolume(CurrentSettings.sfxVolume); 
            }

            if (HapticManager.Instance != null)
            {
                HapticManager.Instance.IsVibrationEnabled = CurrentSettings.isVibrationEnabled; 
            }
        }

        public virtual void ToggleMusic()
        {
            CurrentSettings.isMusicEnabled = !CurrentSettings.isMusicEnabled; 
            SaveSettings();
            UpdateUIVisuals();
        }

        public virtual void ToggleSFX()
        {
            CurrentSettings.isSfxEnabled = !CurrentSettings.isSfxEnabled; 
            SaveSettings();
            UpdateUIVisuals();
        }

        public virtual void ToggleVibration()
        {
            CurrentSettings.isVibrationEnabled = !CurrentSettings.isVibrationEnabled; 
            SaveSettings();
            UpdateUIVisuals();
            if (CurrentSettings.isVibrationEnabled && HapticManager.Instance != null) 
            {
                HapticManager.Instance.LightVibrateImpact();
            }
        }

        public virtual void ToggleTheme()
        {
            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.NextTheme();
               
                CurrentSettings.currentThemeId = ThemeManager.Instance.CurrentTheme.themeId;
                SaveSettings();
                UpdateUIVisuals();
            }
        }
    }
}