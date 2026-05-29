using System;
using ArrowGame.UI.Base;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Manager;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ArrowGame.UI.Popups
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LosePopup : BasePopup
    {
        [Header("--- Core UI References ---")]
        [SerializeField] private RectTransform contentPanel; 
        [SerializeField] private RectTransform heartLeft;    
        [SerializeField] private RectTransform heartRight;   
        [SerializeField] private RectTransform btnHomeRect;  
        [SerializeField] private RectTransform btnReplayRect;

        [Header("--- Buttons ---")]
        [SerializeField] private Button btnReplay;
        [SerializeField] private Button btnHome;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float popupDuration = 0.4f;
        [SerializeField] private float breakDistance = 10f;  
        [SerializeField] private float breakRotation = 5f;  

        private CanvasGroup _canvasGroup;
        private Sequence _showSequence;
        private Sequence _hideSequence;
        
        private Vector2 _leftOriginPos;
        private Vector2 _rightOriginPos;

        protected override void Awake()
        {
            base.Awake();
            _canvasGroup = GetComponent<CanvasGroup>();

            BindButton(btnHome, () =>
            {
                GameManager.Instance.RequestBackHomeWithEnergyWarning();
            });
            BindButton(btnReplay, () =>
            {
                UIManager.Instance.ShowLoading(onCovered: () =>
                {
                    UIManager.Instance.HideLoading();
                    GameManager.Instance.RequestReloadLevelWithEnergyWarning();
                });
            });

            // Lưu lại vị trí chuẩn (lúc tim đang lành lặn) trên Scene
            if (heartLeft != null) _leftOriginPos = heartLeft.anchoredPosition;
            if (heartRight != null) _rightOriginPos = heartRight.anchoredPosition;

            ResetUIState();
        }

        private void ResetUIState()
        {
            if (contentPanel != null) contentPanel.localScale = Vector3.zero;
            if (btnHomeRect != null) btnHomeRect.localScale = Vector3.zero;
            if (btnReplayRect != null) btnReplayRect.localScale = Vector3.zero;

            if (heartLeft != null)
            {
                heartLeft.anchoredPosition = _leftOriginPos;
                heartLeft.localRotation = Quaternion.identity;
            }
            if (heartRight != null)
            {
                heartRight.anchoredPosition = _rightOriginPos;
                heartRight.localRotation = Quaternion.identity;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
        

        protected override void PlayShowAnimation()
        {
            KillAllTweens();
            ResetUIState();

            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.DOFade(1f, 0.3f);

            _showSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _showSequence.Append(contentPanel.DOScale(1f, popupDuration).SetEase(Ease.OutBack));

            if (heartLeft != null && heartRight != null)
            {
                _showSequence.Append(heartLeft.DOAnchorPosX(_leftOriginPos.x - breakDistance, 0.3f).SetEase(Ease.OutBack));
                _showSequence.Join(heartLeft.DOLocalRotate(new Vector3(0, 0, breakRotation), 0.3f).SetEase(Ease.OutQuad));

                _showSequence.Join(heartRight.DOAnchorPosX(_rightOriginPos.x + breakDistance, 0.3f).SetEase(Ease.OutBack));
                _showSequence.Join(heartRight.DOLocalRotate(new Vector3(0, 0, -breakRotation), 0.3f).SetEase(Ease.OutQuad));
            }

            if (btnHomeRect != null && btnReplayRect != null)
            {
                _showSequence.Append(btnHomeRect.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
                _showSequence.Insert(_showSequence.Duration() - 0.15f, btnReplayRect.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            }

            _showSequence.OnComplete(() =>
            {
                _canvasGroup.interactable = true;
            });
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            
            KillAllTweens();
            
            _hideSequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _hideSequence.Join(_canvasGroup.DOFade(0f, 0.2f));
            _hideSequence.Join(contentPanel.DOScale(0.8f, 0.2f).SetEase(Ease.InQuad));

            _hideSequence.OnComplete(() =>
            {
                Hide();
                onComplete?.Invoke();
            });
        }

        private void KillAllTweens()
        {
            _showSequence?.Kill();
            _hideSequence?.Kill();
            contentPanel.DOKill();
            heartLeft.DOKill();
            heartRight.DOKill();
            btnHomeRect.DOKill();
            btnReplayRect.DOKill();
            _canvasGroup.DOKill();
        }

        protected void OnDestroy()
        {
            if (btnReplay != null) btnReplay.onClick.RemoveAllListeners();
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();
            KillAllTweens();
        }
    }
}
