using System;
using System.Collections.Generic;
using ArrowGame.UI.Base;
using ArrowGame.UI.Controllers;
using ArrowGame.UI.Screens.SubScreen;
using UnityEngine;

namespace ArrowGame.UI.Screens
{
    public sealed class MainMenuScreen : BaseScreen
    {
        [Header("--- Controllers ---")]
        [SerializeField] private BottomBarController bottomBar;

        [Header("--- Container ---")]
        [SerializeField] private Transform contentArea; 
        
        [Header("--- Sub-Screen Prefabs ---")]
        [SerializeField] private HomeSubScreen homePanelPrefab; 
        [SerializeField] private ShopSubScreen shopPanelPrefab;
        [SerializeField] private SettingSubScreen settingPanelPrefab;

        private Dictionary<MainTabID, BaseSubScreen> _tabDict = new Dictionary<MainTabID, BaseSubScreen>();
        private BaseSubScreen _currentActiveTab;

        protected override void Awake()
        {
            bottomBar.OnTabClicked += HandleTabChanged;
        }

        public override void Show(Action onOpened = null)
        {
            base.Show(onOpened);
            // Vừa vào Main Menu là bắt nó force nhảy sang tab Home ngay lập tức
            bottomBar.ChangeTab(MainTabID.Home, instant: true); 
        }

        private void HandleTabChanged(MainTabID tabID)
        {
            // 1. Tắt tab hiện tại đi
            if (_currentActiveTab != null)
            {
                _currentActiveTab.Hide();
            }

            // 2. Nếu tab này chưa từng được mở -> Spawn nó ra từ Prefab
            if (!_tabDict.TryGetValue(tabID, out BaseSubScreen targetTab))
            {
                BaseSubScreen prefabToLoad = GetPrefabForTab(tabID);
                if (prefabToLoad != null)
                {
                    targetTab = Instantiate(prefabToLoad, contentArea);
                    targetTab.Init(); // Khởi tạo dữ liệu lần đầu
                    _tabDict.Add(tabID, targetTab);
                }
                else
                {
                    Debug.LogError($"[MainMenuScreen] Chưa gán Prefab cho tab {tabID}!");
                    return;
                }
            }

            // 3. Bật tab lên
            targetTab.Show();
            _currentActiveTab = targetTab;
        }

        private BaseSubScreen GetPrefabForTab(MainTabID id)
        {
            switch (id)
            {
                case MainTabID.Home: return homePanelPrefab;
                case MainTabID.Shop: return shopPanelPrefab;
                case MainTabID.Setting: return settingPanelPrefab;
                default: return null;
            }
        }

        private void OnDestroy()
        {
            if (bottomBar != null) bottomBar.OnTabClicked -= HandleTabChanged;
        }
    }
}