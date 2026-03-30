using System;
using DG.Tweening;
using UnityEngine;

namespace ArrowGame.UI.TopLevels
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI References")] 
        [SerializeField] private RectTransform slidePanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Config")]
        [SerializeField] private float duration = 0.5f; 
        [SerializeField] private Ease showEase = Ease.OutQuad; 
        [SerializeField] private Ease hideEase = Ease.InQuad;  
        
        public void ShowLoading(Action onCovered)
        {
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true; 
            transform.SetAsLastSibling();

            float screenWidth = GetCanvasWidth();

            slidePanel.anchoredPosition = new Vector2(screenWidth, 0f);
            slidePanel.DOKill();
            
            slidePanel.DOAnchorPosX(0f, duration)
                .SetEase(showEase) 
                .SetUpdate(true)
                .OnComplete(() => 
                {
                    onCovered?.Invoke();
                });
        }
        
        public void HideLoading()
        {
            float screenWidth = GetCanvasWidth();

            slidePanel.DOKill();
            
            slidePanel.DOAnchorPosX(-screenWidth, duration)
                .SetEase(hideEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = false; 
                    gameObject.SetActive(false); 
                });
        }

        private float GetCanvasWidth()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                return canvas.GetComponent<RectTransform>().rect.width;
            }
            return Screen.width; // Fallback
        }
    }
}