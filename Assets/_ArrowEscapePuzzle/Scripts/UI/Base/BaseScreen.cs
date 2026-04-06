using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace ArrowGame.UI.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseScreen : MonoBehaviour
    {
        [Header("--- Screen Settings ---")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float transitionDuration = 0.3f;

        protected virtual void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show(Action onShowComplete = null)
        {
            gameObject.SetActive(true);
            OnBeforeShow();

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, transitionDuration)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    OnAfterShow();
                    onShowComplete?.Invoke();
                });
        }
        
        public virtual void Hide(Action onHideComplete = null)
        {
            OnBeforeHide();

            canvasGroup.DOKill();
            canvasGroup.alpha = 1f;
            canvasGroup.DOFade(0f, transitionDuration)
                .SetUpdate(true)
                .SetLink(gameObject)
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
                btn.transform.DOKill();
                btn.transform.localScale = Vector3.one;
                btn.transform
                    .DOPunchScale(Vector3.one * -0.1f, 0.15f, 5) 
                    .SetUpdate(true)
                    .SetLink(btn.gameObject)
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