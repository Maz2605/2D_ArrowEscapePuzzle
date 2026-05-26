using ArrowGame.Data.Events;
using ArrowGame.Data.States; 
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Components;
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
        private int _pendingLevelStart;
        private int _pendingLevelTarget;
        private bool _hasPendingLevelProgression;

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
                PrepareLevelProgression();
                
                if (mapManager != null)
                {
                    mapManager.RefreshMapData();
                }
            }
        }

        public override void Show()
        {
            base.Show(); 

            PrepareLevelProgression();

            if (!IsWaitingForLoadingToHide())
            {
                PlayRevealAnimations();
            }
        }

        public override void PlayRevealAnimations()
        {
            PlayLevelProgressionIfNeeded();

            StreakWidget[] streakWidgets = GetComponentsInChildren<StreakWidget>(true);
            for (int i = 0; i < streakWidgets.Length; i++)
            {
                streakWidgets[i]?.RefreshFromData(true);
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

        private void PrepareLevelProgression()
        {
            if (txtCurrentLevel == null) return;

            int actualLevel = DataManager.Instance.GetCurrentLevel();

            if (_cachedLevelForUI == -1 || _cachedLevelForUI == actualLevel)
            {
                _cachedLevelForUI = actualLevel;
                txtCurrentLevel.text = $"LEVEL {actualLevel}";
                _hasPendingLevelProgression = false;
                return;
            }

            if (actualLevel > _cachedLevelForUI)
            {
                _pendingLevelStart = _cachedLevelForUI;
                _pendingLevelTarget = actualLevel;
                _hasPendingLevelProgression = true;
                txtCurrentLevel.text = $"LEVEL {_pendingLevelStart}";
            }
        }

        private void PlayLevelProgressionIfNeeded()
        {
            if (!_hasPendingLevelProgression || txtCurrentLevel == null) return;

            int startValue = _pendingLevelStart;
            int actualLevel = _pendingLevelTarget;
            _cachedLevelForUI = actualLevel;
            _hasPendingLevelProgression = false;
            
            if (actualLevel > startValue)
            {
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

        private bool IsWaitingForLoadingToHide()
        {
            return UIManager.HasInstance && UIManager.Instance != null && UIManager.Instance.IsLoadingVisible;
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
