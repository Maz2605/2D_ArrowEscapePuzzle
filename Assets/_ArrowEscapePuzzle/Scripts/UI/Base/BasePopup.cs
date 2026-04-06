using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

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
            
            canvasGroup.blocksRaycasts = false; 
            
            OnOpened = onOpenedCallback;

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animDuration)
                .SetUpdate(true)
                .OnComplete(() => 
                {
                    canvasGroup.blocksRaycasts = true; 
                }); 
            
            PlayShowAnimation();
        }

        public virtual void Hide()
        {
            canvasGroup.blocksRaycasts = false; 
            
            canvasGroup.DOKill();
            
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
                btn.transform.DOKill();
                btn.transform.localScale = Vector3.one;
                btn.transform
                    .DOPunchScale(Vector3.one * -0.1f, 0.15f, 5) 
                    .SetUpdate(true) 
                    .SetLink(gameObject)
                    .OnComplete(() => onClickAction?.Invoke());
            });
        }

        // Bắt buộc các Popup con (Setting, Confirm) phải tự định nghĩa anim
        protected abstract void PlayShowAnimation();
        protected abstract void PlayHideAnimation(Action onComplete);
    }
}