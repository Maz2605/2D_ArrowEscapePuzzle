using ArrowGame.Data.Events;
using ArrowGame.UI.Base;
using ArrowGame.UI.Controllers;
using ArrowGame.UI.Manager;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Screens.SubScreen
{
    public class HomeSubScreen : BaseSubScreen
    {
        [Header("--- Sub-Components ---")]
        [SerializeField] private MapManager mapManager;
        
        [Header("--- UI References ---")]
        [SerializeField] private Button btnPlay; // Nút Play chính để chơi Level tiếp theo (nếu có)

        public override void Init()
        {
            base.Init(); // Set isInitialized = true

            // Tự động tìm MapManager nếu bạn quên kéo thả trên Inspector
            if (mapManager == null)
            {
                mapManager = GetComponentInChildren<MapManager>(true);
            }

            if (btnPlay != null)
            {
                btnPlay.onClick.AddListener(OnPlayClicked);
            }
        }

        public override void Show()
        {
            base.Show(); // Bật active GameObject

            if (mapManager != null)
            {
                mapManager.RefreshMapData();
                
                // Focus lại vào level hiện tại sau 1 frame để UI Layout kịp update kích thước
                DOVirtual.DelayedCall(0.1f, () => 
                {
                    if (this != null && gameObject.activeInHierarchy)
                    {
                        mapManager.FocusOnCurrentLevel();
                    }
                }).SetLink(gameObject);
            }
        }

        public override void Hide()
        {
            base.Hide(); // Tắt active GameObject
            
            // Nếu cần dừng animation nào đó ở màn Home khi chuyển sang Shop/Setting thì viết ở đây
        }

        private void OnPlayClicked()
        {
            // Flow giống hệt như bạn đã viết
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
        }
    }
}