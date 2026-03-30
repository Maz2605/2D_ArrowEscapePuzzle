using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Controllers
{
    [Serializable]
    public class NavTab
    {
        public Button btn;
        public RectTransform rectTransform; // Cục để scale
        public GameObject activeState;      // Chứa icon to + text
        public GameObject inactiveState;    // Chứa icon nhỏ
    }

    public class BottomBarController : MonoBehaviour
    {
        [Header("--- Tabs Setup (0: Shop, 1: Home, 2: Setting) ---")]
        [SerializeField] private NavTab[] tabs;
        [SerializeField] private float animDuration = 0.25f;

        public event Action<int> OnTabClicked;

        private int _currentIndex = -1;

        private void Start()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i; // Cache lại index cho closure
                tabs[i].btn.onClick.AddListener(() => ChangeTab(index));
            }
        }

        public void ChangeTab(int index, bool instant = false)
        {
            if (_currentIndex == index) return;
            _currentIndex = index;

            float duration = instant ? 0f : animDuration;

            for (int i = 0; i < tabs.Length; i++)
            {
                bool isSelected = (i == index);
                
                tabs[i].activeState.SetActive(isSelected);
                tabs[i].inactiveState.SetActive(!isSelected);

                tabs[i].rectTransform.DOKill(); // Dừng anim cũ nếu đang chạy
                if (isSelected)
                {
                    tabs[i].rectTransform.DOScale(1.15f, duration).SetEase(Ease.OutBack);
                }
                else
                {
                    tabs[i].rectTransform.DOScale(1.0f, duration).SetEase(Ease.OutQuad);
                }
            }

            OnTabClicked?.Invoke(index);
        }
    }
}