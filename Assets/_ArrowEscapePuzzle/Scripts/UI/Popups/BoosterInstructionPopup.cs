using System;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ArrowGame.UI.Popups
{
    public class BoosterInstructionPopup : BasePopup
    {
        [Header("--- UI References ---")]
        [SerializeField] private Button btnCancel;
        [SerializeField] private RectTransform contentPanel; 

        [Header("--- Animation Settings ---")]
        [SerializeField] private float slideOffset = 300f; // Cùng giá trị với GameplayScreen
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        private Vector2 _originalPos;
        private bool _isInitialized = false;

        protected override void Awake()
        {
            base.Awake();
            
            // Lưu vị trí gốc ban đầu của panel
            if (contentPanel != null)
            {
                _originalPos = contentPanel.anchoredPosition;
                _isInitialized = true;
            }

            BindButton(btnCancel, () =>
            {
                GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
            });
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            if (contentPanel != null)
            {
                contentPanel.DOKill();
                // Đặt vị trí xuất phát ở dưới (vị trí gốc trừ đi offset)
                contentPanel.anchoredPosition = _originalPos - new Vector2(0, slideOffset);
                canvasGroup.alpha = 0f;
            }
        }

        protected override void PlayShowAnimation()
        {
            if (contentPanel != null)
            {
                contentPanel.DOKill();
                // Trượt lên vị trí gốc
                contentPanel.DOAnchorPos(_originalPos, animDuration)
                    .SetEase(showEase)
                    .SetUpdate(true)
                    .SetLink(gameObject);
                
                canvasGroup.DOFade(1f, animDuration).SetUpdate(true);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            if (contentPanel != null)
            {
                contentPanel.DOKill();
                contentPanel.DOAnchorPos(_originalPos - new Vector2(0, slideOffset), animDuration)
                    .SetEase(hideEase)
                    .SetUpdate(true)
                    .SetLink(gameObject)
                    .OnComplete(() => onComplete?.Invoke());

                canvasGroup.DOFade(0f, animDuration).SetUpdate(true);
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}