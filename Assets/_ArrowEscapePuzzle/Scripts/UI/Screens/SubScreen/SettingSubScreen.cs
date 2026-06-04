using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Components;
using ArrowGame.UI.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Screens.SubScreen
{
    public class SettingScreen : BaseSubScreen
    {
        [Header("--- UI Toggles ---")]
        [SerializeField] private UIToggle musicToggle;
        [SerializeField] private UIToggle sfxToggle;
        [SerializeField] private UIToggle vibrationToggle;
        [SerializeField] private UIToggle themeToggle;
        [SerializeField] private UIToggle fpsToggle;
        
        [Header("--- Develop ---")]
        [SerializeField] private Button resetButton;
        [SerializeField] private Button boosterButton;

        public override void Init()
        {
            if (isInitialized) return; 
            base.Init(); 

            if (musicToggle) musicToggle.OnValueChanged = _ => SettingManager.Instance.ToggleMusic();
            if (sfxToggle) sfxToggle.OnValueChanged = _ => SettingManager.Instance.ToggleSFX();
            if (vibrationToggle) vibrationToggle.OnValueChanged = _ => SettingManager.Instance.ToggleVibration();
            if (fpsToggle) fpsToggle.OnValueChanged = _ => SettingManager.Instance.ToggleFPS();
            
            BindButton(resetButton, () =>
            {
                DataManager.Instance.DeleteAllProgress();
                UIManager.Instance.ShowToast("DeleteAllProgress");
            });
            BindButton(boosterButton, () =>
            {
                // DataManager.Instance.AddBooster(BoosterType.Hint, 5);
                // DataManager.Instance.AddBooster(BoosterType.Gate, 5);
                // DataManager.Instance.AddBooster(BoosterType.ArrowDash, 5);
                DataManager.Instance.AddCoin(9999);
                UIManager.Instance.ShowToast("AddCoin");
            });
            
            // --- CẬP NHẬT LOGIC THEME ---
            if (themeToggle) 
            {
                themeToggle.OnValueChanged = _ => 
                {
                    // 1. Gọi lệnh xoay vòng Theme (Logic)
                    SettingManager.Instance.ToggleTheme(); 
                    
                    // 2. Ép UI phải cập nhật lại đúng với Theme đang chạy
                    SyncThemeToggleVisual();
                };
            }
        }

        public override void Show()
        {
            base.Show(); 
            UpdateUIVisuals(); 
        }

        private void UpdateUIVisuals()
        {
            var data = SettingManager.Instance.CurrentSettings;

            if (musicToggle) 
                musicToggle.InitState(data.isMusicEnabled);
            
            if (sfxToggle) 
                sfxToggle.InitState(data.isSfxEnabled);
            
            if (vibrationToggle) 
                vibrationToggle.InitState(data.isVibrationEnabled);
            
            if (fpsToggle) 
                fpsToggle.InitState(data.targetFPS == 60);

            // --- ĐỒNG BỘ THEME LÚC MỚI MỞ UI ---
            SyncThemeToggleVisual();
        }

        private void SyncThemeToggleVisual()
        {
            if (themeToggle == null || ThemeManager.Instance == null || ThemeManager.Instance.CurrentTheme == null) return;

            // Kiểm tra xem Theme hiện tại có phải là Theme đầu tiên trong list (Dark Mode) hay không
            bool isDarkMode = ThemeManager.Instance.CurrentTheme.themeId == ThemeManager.Instance.availableThemes[0].themeId;
            
            // Ép trạng thái của nút trượt về đúng vị trí (True/False) mà KHÔNG kích hoạt OnValueChanged
            themeToggle.InitState(isDarkMode);
        }
    }
}