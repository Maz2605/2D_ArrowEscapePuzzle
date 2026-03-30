using System;
using ArrowGame.UI.Base;
using ArrowGame.UI.Controllers;
using ArrowGame.UI.Manager;
using GameCore.Utils.DesignPattern.Events;
using ArrowGame.Data.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Screens
{
    public class MainMenuScreen : BaseScreen
    {
        [Header("--- Controllers ---")]
        [SerializeField] private BottomBarController bottomBar;

        [Header("--- Container & Sub-Panels ---")]
        [SerializeField] private Transform contentArea; 
        [SerializeField] private GameObject homePanelInstance; 
        [SerializeField] private Button btnPlay;

        
        [Header("--- Lazy Load Prefabs ---")]
        [SerializeField] private GameObject shopPanelPrefab;
        [SerializeField] private GameObject settingPanelPrefab;

        private GameObject _shopPanelInstance;
        private GameObject _settingPanelInstance;

        protected virtual void Start()
        {
            // Lắng nghe sự kiện click từ BottomBar
            bottomBar.OnTabClicked += HandleTabChanged;
            
            if (btnPlay != null)
                btnPlay.onClick.AddListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            // Che màn hình trước. Sau khi màn che đóng lại hoàn toàn (onCovered),
            // mới ra lệnh build level — người chơi sẽ không thấy grid bị spawn giật cục.
            UIManager.Instance.ShowLoading(onCovered: () =>
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestRestartLevel);
            });
        }

        public override void Show(Action onOpened = null)
        {
            base.Show(onOpened);
            // Khi bật Screen Menu lên, luôn ép nó về tab Home (index 1) ngay lập tức (instant = true)
            bottomBar.ChangeTab(1, instant: true); 
        }

        private void HandleTabChanged(int index)
        {
            // Tắt hết các panel đi
            homePanelInstance.SetActive(false);
            if (_shopPanelInstance != null) _shopPanelInstance.SetActive(false);
            if (_settingPanelInstance != null) _settingPanelInstance.SetActive(false);

            switch (index)
            {
                case 0: 
                    if (_shopPanelInstance == null)
                    {
                        _shopPanelInstance = Instantiate(shopPanelPrefab, contentArea);
                    }
                    _shopPanelInstance.SetActive(true);
                    break;

                case 1: 
                    homePanelInstance.SetActive(true);
                    break;

                case 2: // Bật Màn Setting
                    if (_settingPanelInstance == null)
                    {
                        _settingPanelInstance = Instantiate(settingPanelPrefab, contentArea);
                    }
                    _settingPanelInstance.SetActive(true);
                    break;
            }
        }

        private void OnDestroy()
        {
            // Clean up event delegate để tránh memory leak
            if (bottomBar != null)
            {
                bottomBar.OnTabClicked -= HandleTabChanged;
            }
            if (btnPlay != null)
            {
                btnPlay.onClick.RemoveListener(OnPlayClicked);
            }
        }
    }
}