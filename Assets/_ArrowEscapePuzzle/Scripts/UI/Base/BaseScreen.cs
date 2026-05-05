using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using ArrowGame.Haptic;

namespace ArrowGame.UI.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseScreen : MonoBehaviour
    {
        [Header("--- Screen Settings ---")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float transitionDuration = 0.3f;
        
        protected Tween _animTween;

        protected virtual void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show(Action onShowComplete = null)
        {
            gameObject.SetActive(true);
            OnBeforeShow();

            _animTween?.Kill();
            canvasGroup.alpha = 0f;
            _animTween = canvasGroup.DOFade(1f, transitionDuration)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    OnAfterShow();
                    onShowComplete?.Invoke();
                });
        }
        
        public virtual void Hide(Action onHideComplete = null)
        {
            OnBeforeHide();

            _animTween?.Kill();
            canvasGroup.alpha = 1f;
            _animTween = canvasGroup.DOFade(0f, transitionDuration)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    OnAfterHide();
                    onHideComplete?.Invoke();
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
        
        // Các hook để class con override, giống quy trình OnEnable/OnDisable nhưng an toàn với Animation
        protected virtual void OnBeforeShow() { }
        protected virtual void OnAfterShow() { }
        protected virtual void OnBeforeHide() { }
        protected virtual void OnAfterHide() { }
    }
}