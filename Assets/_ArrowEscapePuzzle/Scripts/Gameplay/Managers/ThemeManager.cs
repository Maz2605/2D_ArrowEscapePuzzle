using System.Collections.Generic;
using UnityEngine;
using GameCore.Utils.DesignPattern.Singleton;
using GameCore.Utils.DesignPattern.Events;
using ArrowGame.Data.Events;
using ArrowGame.Data.Theme;
using System.Linq;
using ArrowGame.Interface;
using GameCore.Data; 

namespace ArrowGame.Gameplay.Managers
{
    public class ThemeManager : Singleton<ThemeManager>, IAppService
    {
        [Header("Available Themes")]
        public List<ThemeConfigSO> availableThemes; 

        private Dictionary<string, ThemeConfigSO> _themeDict;
        
        public ThemeConfigSO CurrentTheme { get; private set; }

        private const string SETTING_SAVE_KEY = "global_user_setting";

        public void Init()
        {
            InitDictionary();
            LoadThemeData();
        }

        private void InitDictionary()
        {
            _themeDict = new Dictionary<string, ThemeConfigSO>();
            foreach (var theme in availableThemes)
            {
                if (theme != null && !_themeDict.ContainsKey(theme.themeId))
                {
                    _themeDict.Add(theme.themeId, theme);
                }
            }
        }

        private void LoadThemeData()
        {
            var settings = SaveSystem.Load<GlobalUserSetting>(SETTING_SAVE_KEY) ?? new GlobalUserSetting();
            string savedThemeId = settings.currentThemeId;
            
            if (!string.IsNullOrEmpty(savedThemeId) && _themeDict.ContainsKey(savedThemeId))
            {
                CurrentTheme = _themeDict[savedThemeId];
            }
            else
            {
                // Lần đầu vào game chưa có save -> Lấy theme mặc định đầu tiên
                CurrentTheme = availableThemes.FirstOrDefault(); 
            }
            CurrentTheme = _themeDict.ContainsKey(savedThemeId) ? _themeDict[savedThemeId] : availableThemes.FirstOrDefault();
            EventManager<VisualEventID>.Post(VisualEventID.ThemeChanged, CurrentTheme);
            Debug.Log($"[ThemeManager] Init xong! Đang dùng Theme: {(CurrentTheme != null ? CurrentTheme.themeId : "NULL")}");
        }

        public void SwitchTheme(string newThemeId)
        {
            if (!_themeDict.ContainsKey(newThemeId) || CurrentTheme.themeId == newThemeId) return;

            CurrentTheme = _themeDict[newThemeId];
            EventManager<VisualEventID>.Post(VisualEventID.ThemeChanged, CurrentTheme);
        }
        
        public void NextTheme()
        {
            if (availableThemes == null || availableThemes.Count <= 1) return;

            int currentIndex = availableThemes.FindIndex(t => t.themeId == CurrentTheme.themeId);
            
            int nextIndex = (currentIndex + 1) % availableThemes.Count;
            
            string nextThemeId = availableThemes[nextIndex].themeId;
            SwitchTheme(nextThemeId);
        }
    }
}