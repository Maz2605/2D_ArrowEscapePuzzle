using System;
using DG.Tweening;
using UnityEngine;

namespace ArrowGame.UI.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        [Header("--- Base Popup Settings ---")] 
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float animDuration = 0.25f;

        public Action OnOpened;
        public Action OnClosed;

        protected virtual void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show(Action onOpenedCallback = null)
        {
            gameObject.SetActive(true);
            OnOpened = onOpenedCallback;

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animDuration).SetUpdate(true); 
            
            PlayShowAnimation();
        }

        public virtual void Hide()
        {
            canvasGroup.DOKill();
            
            PlayHideAnimation(() => 
            {
                gameObject.SetActive(false);
                OnClosed?.Invoke();
                OnOpened = null;
                OnClosed = null;
            });
        }

        // Bắt buộc các Popup con (Setting, Confirm) phải tự định nghĩa anim
        protected abstract void PlayShowAnimation();
        protected abstract void PlayHideAnimation(Action onComplete);
    }
}