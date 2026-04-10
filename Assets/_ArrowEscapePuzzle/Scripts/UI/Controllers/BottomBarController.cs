using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ArrowGame.UI.Base; // Phải using namespace này để thấy MainTabID

namespace ArrowGame.UI.Controllers
{
    [Serializable]
    public class NavTab
    {
        public MainTabID tabID; // Thay thế biến int ngầm định bằng Enum
        public Button btn;
        public RectTransform rectTransform; 
        public GameObject activeState;     
        public GameObject inactiveState;   
    }

    public class BottomBarController : MonoBehaviour
    {
        [Header("--- Tabs Setup ---")]
        [SerializeField] private NavTab[] tabs;
        [SerializeField] private float animDuration = 0.25f;

        // Đổi Action<int> thành Action<MainTabID>
        public event Action<MainTabID> OnTabClicked; 

        private MainTabID _currentTab = (MainTabID)(-1); // Khởi tạo giá trị rác để ép nó cập nhật lần đầu

        private void Start()
        {
            foreach (var tab in tabs)
            {
                MainTabID id = tab.tabID; // Cache lại id cho closure của lambda
                tab.btn.onClick.AddListener(() => ChangeTab(id));
            }
        }

        public void ChangeTab(MainTabID tabID, bool instant = false)
        {
            if (_currentTab == tabID) return;
            _currentTab = tabID;

            float duration = instant ? 0f : animDuration;

            foreach (var tab in tabs)
            {
                bool isSelected = (tab.tabID == tabID);
                
                tab.activeState.SetActive(isSelected);
                tab.inactiveState.SetActive(!isSelected);

                tab.rectTransform.DOKill(); 
                if (isSelected)
                {
                    tab.rectTransform.DOScale(1.15f, duration).SetEase(Ease.OutBack); 
                }
                else
                {
                    tab.rectTransform.DOScale(1.0f, duration).SetEase(Ease.OutQuad); 
                }
            }

            OnTabClicked?.Invoke(tabID);
        }
    }
}