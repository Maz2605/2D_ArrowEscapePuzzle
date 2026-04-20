using System;
using System.Collections.Generic;
using ArrowGame.UI.Base;
using ArrowGame.UI.Controllers;
using UnityEngine;

namespace ArrowGame.UI.Screens
{
    public sealed class MainMenuScreen : BaseScreen
    {
        [Header("--- UI Controllers ---")]
        [SerializeField] private BottomBarController bottomBar;

        [Header("--- Layout Container ---")]
        [SerializeField] private Transform contentArea; 
        
        [Header("--- Sub-Screen Prefabs ---")]
        [SerializeField] private BaseSubScreen homePrefab; 
        [SerializeField] private BaseSubScreen shopPrefab;
        [SerializeField] private BaseSubScreen settingPrefab;

        private Dictionary<MainTabID, BaseSubScreen> _tabDict = new Dictionary<MainTabID, BaseSubScreen>();
        private BaseSubScreen _currentActiveTab;

        protected override void Awake()
        {
            // Đăng ký event từ BottomBar
            bottomBar.OnTabClicked += HandleTabChanged;
        }

        public override void Show(Action onOpened = null)
        {
            base.Show(onOpened);
            
            // Bắt buộc Init trước để tính toán tọa độ responsive
            bottomBar.Init(); 

            // Mặc định nhảy vào tab Home không animation
            bottomBar.ChangeTab(MainTabID.Home, instant: true); 
        }

        private void HandleTabChanged(MainTabID tabID)
        {
            // 1. Ẩn tab cũ
            if (_currentActiveTab != null) _currentActiveTab.Hide();

            // 2. Lazy-load: Chỉ spawn khi người dùng nhấn vào
            if (!_tabDict.TryGetValue(tabID, out BaseSubScreen targetTab))
            {
                BaseSubScreen prefab = GetPrefabByID(tabID);
                if (prefab != null)
                {
                    targetTab = Instantiate(prefab, contentArea);
                    targetTab.Init(); 
                    _tabDict.Add(tabID, targetTab);
                }
            }

            // 3. Hiện tab mới
            if (targetTab != null)
            {
                targetTab.Show();
                _currentActiveTab = targetTab;
            }
        }

        private BaseSubScreen GetPrefabByID(MainTabID id)
        {
            return id switch
            {
                MainTabID.Home => homePrefab,
                MainTabID.Shop => shopPrefab,
                MainTabID.Settings => settingPrefab,
                _ => null
            };
        }

        private void OnDestroy()
        {
            if (bottomBar != null) bottomBar.OnTabClicked -= HandleTabChanged;
        }
    }
}