using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using ArrowGame.Haptic;

namespace ArrowGame.UI.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        [Header("--- Base Popup Settings ---")] 
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float animDuration = 0.25f;
        
        protected Tween _animTween;

        public Action OnOpened;
        public Action OnClosed;

        protected virtual void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show(Action onOpenedCallback = null)
        {
            gameObject.SetActive(true);
            
            OnBeforeShow();
            canvasGroup.blocksRaycasts = false; 
            
            OnOpened = onOpenedCallback;

            _animTween?.Kill();
            canvasGroup.alpha = 0f;
            _animTween = canvasGroup.DOFade(1f, animDuration)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => 
                {
                    canvasGroup.blocksRaycasts = true; 
                }); 
            
            PlayShowAnimation();
        }

        public virtual void Hide()
        {
            canvasGroup.blocksRaycasts = false; 
            
            _animTween?.Kill();
            
            PlayHideAnimation(() => 
            {
                gameObject.SetActive(false);
                OnClosed?.Invoke();
                OnOpened = null;
                OnClosed = null;
            });
        }
        
        protected void BindButton(Button btn, Action onClickAction)
        {
            if (btn == null) return;
            
            btn.onClick?.RemoveAllListeners();
            btn.onClick?.AddListener(() =>
            {
                // Trigger Haptic
                HapticManager.Instance.Selection();
                
                btn.transform.DOKill();
                btn.transform.localScale = Vector3.one;
                btn.transform
                    .DOPunchScale(Vector3.one * -0.1f, 0.15f, 5) 
                    .SetUpdate(true) 
                    .SetLink(btn.gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() => onClickAction?.Invoke());
            });
        }

        // Bắt buộc các Popup con (Setting, Confirm) phải tự định nghĩa anim
        protected virtual void OnBeforeShow() { }
        protected abstract void PlayShowAnimation();
        protected abstract void PlayHideAnimation(Action onComplete);
    }
}