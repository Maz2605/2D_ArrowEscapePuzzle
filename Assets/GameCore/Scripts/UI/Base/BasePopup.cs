using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace GameCore.UI.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopup : MonoBehaviour
    {
        [Header("Base Settings")] 
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float animDuration = 0.2f;

        public UnityEvent OnOpen;
        public UnityEvent OnClose;

        protected virtual void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnOpen?.Invoke();

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
                OnClose?.Invoke();
            });
        }

        protected abstract void PlayShowAnimation();
        protected abstract void PlayHideAnimation(Action onComplete);
    }
}