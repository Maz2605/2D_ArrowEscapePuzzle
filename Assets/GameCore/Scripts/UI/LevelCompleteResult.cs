using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GameCore.UI
{
    public class LevelCompleteResult : MonoBehaviour
    {
        [Header("--- UI COMPONENTS ---")]
        [SerializeField] private RectTransform panelRoot; 
        [SerializeField] private CanvasGroup bgDim;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private TextMeshProUGUI txtLevelName;

        [Header("--- ANIM CONFIG ---")]
        [SerializeField] private float slideDuration = 0.8f;
        [SerializeField] private float stayDuration = 2.0f;
        [SerializeField] private Ease enterEase = Ease.OutExpo;
        [SerializeField] private Ease exitEase = Ease.InBack;

        private Tween _activeTween;
        private float _screenWidth;

        private void Awake()
        {
            _screenWidth = Screen.width;
            
            gameObject.SetActive(false);
            if (panelRoot) panelRoot.anchoredPosition = new Vector2(_screenWidth, 0);
            if (bgDim)
            {
                bgDim.gameObject.SetActive(false);
                bgDim.alpha = 0;
            }
        }

        public void Show(string levelName, Action onCoveredCallback = null, Action onFinishedCallback = null)
        {
            gameObject.SetActive(true);
            if (bgDim) bgDim.gameObject.SetActive(true);
            
            if (txtLevelName) txtLevelName.text = "COMPLETED\n" + levelName;
            
            if (panelRoot) panelRoot.anchoredPosition = new Vector2(_screenWidth, 0);
            if (contentContainer) contentContainer.localScale = Vector3.zero;

            _activeTween?.Kill();
            Sequence seq = DOTween.Sequence();
            
            if (bgDim) seq.Append(bgDim.DOFade(1f, 0.3f));
            if (panelRoot) seq.Join(panelRoot.DOAnchorPosX(0, slideDuration).SetEase(enterEase));
            if (contentContainer) seq.Append(contentContainer.DOScale(1f, 0.6f).SetEase(Ease.OutElastic));
            
            seq.AppendInterval(stayDuration);
            
            seq.AppendCallback(() => {
                onCoveredCallback?.Invoke(); 
            });
            
            if (contentContainer) seq.Append(contentContainer.DOScale(0f, 0.2f).SetEase(Ease.InBack));
            if (panelRoot) seq.Join(panelRoot.DOAnchorPosX(-_screenWidth, 0.6f).SetEase(exitEase));
            if (bgDim) seq.Join(bgDim.DOFade(0f, 0.4f));
            
            seq.OnComplete(() => {
                gameObject.SetActive(false);
                if (bgDim) bgDim.gameObject.SetActive(false);
                if (panelRoot) panelRoot.anchoredPosition = new Vector2(_screenWidth, 0);
                
                onFinishedCallback?.Invoke();
            });

            _activeTween = seq;
        }

        private void OnDestroy()
        {
            _activeTween?.Kill();
        }
    }
}