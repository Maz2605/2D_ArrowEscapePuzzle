using System;
using DG.Tweening;
using UnityEngine;

namespace GameCore.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("UI References")] 
        [SerializeField] private RectTransform image;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation Config")]
        [SerializeField] private float duration = 0.8f; 
        [SerializeField] private Ease showEase = Ease.OutBack; 
        [SerializeField] private Ease hideEase = Ease.InBack;  
        
        private float _targetScale;

        
        public void ShowLoading(Action onCovered)
        {
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true; 
            
            transform.SetAsLastSibling();

            CalculateTargetScale();

            image.localScale = Vector3.zero;
            image.DOKill();
            
            image.DOScale(_targetScale, duration)
                .SetEase(showEase) 
                .SetUpdate(true)
                .OnComplete(() => 
                {
                    onCovered?.Invoke();
                });
        }
        
        public void HideLoading()
        {
            image.DOKill();
            image.DOScale(0f, duration)
                .SetEase(hideEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = false; 
                    gameObject.SetActive(false); 
                });
        }

        private void CalculateTargetScale()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float width = canvasRect.rect.width;
            float height = canvasRect.rect.height;
            
            float diagonal = Mathf.Sqrt(width * width + height * height);
            
            float initialSize = image.rect.width; 
            if (initialSize == 0) initialSize = 100;
            
            _targetScale = (diagonal / initialSize) * 1.2f;
        }
    }
}