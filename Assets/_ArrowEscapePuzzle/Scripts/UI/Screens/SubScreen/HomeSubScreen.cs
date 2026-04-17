using ArrowGame.Data.Events;
using ArrowGame.Data.States; 
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Controllers;
using ArrowGame.UI.Manager;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Screens.SubScreen
{
    public class HomeSubScreen : BaseSubScreen
    {
        [Header("--- Sub-Components ---")]
        [SerializeField] private MapManager mapManager;
        
        [Header("--- UI References ---")]
        [SerializeField] private Button btnPlay;
        [SerializeField] private TextMeshProUGUI txtCurrentLevel;

        private static int _cachedLevelForUI = -1; 
        private Tween _countTween;

        public override void Init()
        {
            base.Init();

            if (mapManager == null)
            {
                mapManager = GetComponentInChildren<MapManager>(true);
            }

            if (btnPlay != null)
            {
                btnPlay.onClick.AddListener(OnPlayClicked);
            }

            // Đăng ký lắng nghe sự kiện chuyển State toàn cục
            EventManager<LogicGameEventID>.AddListener<GameState>(LogicGameEventID.GameStateChanged, OnGameStateChanged);
        }

        // Tách riêng thành 1 hàm xử lý khi có State đổi
        private void OnGameStateChanged(GameState newState)
        {
            // Chỉ cập nhật UI Level và Map khi game thực sự chuyển về MainMenu
            if (newState == GameState.MainMenu)
            {
                HandleLevelProgression();
                
                if (mapManager != null)
                {
                    mapManager.RefreshMapData();
                }
            }
        }

        public override void Show()
        {
            base.Show(); 

            // Nếu đây là lần đầu tiên game load lên (Init chưa kịp bắt event GameStateChanged)
            // thì fallback gọi tay 1 lần để đảm bảo có data.
            if (_cachedLevelForUI == -1)
            {
                HandleLevelProgression();
            }

            if (mapManager != null)
            {
                DOVirtual.DelayedCall(0.1f, () => 
                {
                    if (this != null && gameObject.activeInHierarchy)
                    {
                        mapManager.FocusOnCurrentLevel();
                    }
                }).SetLink(gameObject);
            }
        }

        private void HandleLevelProgression()
        {
            if (txtCurrentLevel == null) return;

            int actualLevel = DataManager.Instance.GetCurrentLevel();

            if (_cachedLevelForUI == -1 || _cachedLevelForUI == actualLevel)
            {
                _cachedLevelForUI = actualLevel;
                txtCurrentLevel.text = $"LEVEL {actualLevel}";
                return;
            }

            if (actualLevel > _cachedLevelForUI)
            {
                int startValue = _cachedLevelForUI;
                _cachedLevelForUI = actualLevel; 

                txtCurrentLevel.transform.localScale = Vector3.one;
                _countTween?.Kill();
                
                _countTween = DOVirtual.Int(startValue, actualLevel, 0.8f, (v) => 
                {
                    txtCurrentLevel.text = $"LEVEL {v}";
                })
                .SetEase(Ease.OutExpo)
                .OnComplete(() => 
                {
                    txtCurrentLevel.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 8, 1);
                });
            }
        }

        public override void Hide()
        {
            base.Hide();
            _countTween?.Kill(); 
        }

        private void OnPlayClicked()
        {
            if (UIManager.HasInstance)
            {
                UIManager.Instance.ShowLoading(onCovered: () =>
                {
                    EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel);
                });
            }
            else
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel);
            }
        }

        private void OnDestroy()
        {
            if (btnPlay != null)
            {
                btnPlay.onClick.RemoveListener(OnPlayClicked);
            }
            
            // Hủy đăng ký lắng nghe sự kiện để tránh Memory Leak (Rất quan trọng!)
            EventManager<LogicGameEventID>.RemoveListener<GameState>(LogicGameEventID.GameStateChanged, OnGameStateChanged);
            
            _countTween?.Kill(); 
        }
    }
}