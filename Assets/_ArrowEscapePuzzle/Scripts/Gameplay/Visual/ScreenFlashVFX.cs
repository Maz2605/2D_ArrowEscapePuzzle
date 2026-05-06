using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ArrowGame.Gameplay.Visual
{
    public class ScreenFlashVFX : MonoBehaviour
    {
        [Header("--- References ---")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image vignetteImage; // Thêm reference đến Image để đổi màu

        [Header("--- Animation Settings ---")]
        [SerializeField] private float fadeInDuration = 0.05f;
        [SerializeField] private float fadeOutDuration = 0.35f;
        [SerializeField] private float maxAlpha = 0.7f;
        
        [SerializeField]
        private Color _defaultColor = new Color(1f, 0.4f, 0f); // Màu cam mặc định
        
        [Header("--- Juice Settings ---")]
        [SerializeField] private bool useScaleImpact = true;
        [SerializeField] private float scalePunchAmount = 0.03f; // Giật nhẹ scale UI

        private Tween _flashTween;
        private Tween _scaleTween;
        

        private void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (vignetteImage == null) vignetteImage = GetComponent<Image>();

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _flashTween?.Kill();
            _scaleTween?.Kill();
        }
        
        public void PlayImpact(Color? flashColor = null)
        {
            if (canvasGroup == null) return;

            _flashTween?.Kill();
            _scaleTween?.Kill();
            
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            transform.localScale = Vector3.one;

            if (vignetteImage != null)
            {
                vignetteImage.color = flashColor ?? _defaultColor;
            }

            Sequence fadeSeq = DOTween.Sequence();
            fadeSeq.Append(canvasGroup.DOFade(maxAlpha, fadeInDuration).SetEase(Ease.OutQuad));
            fadeSeq.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InOutSine));
            fadeSeq.OnComplete(() => gameObject.SetActive(false));
            
            _flashTween = fadeSeq;

            // Xử lý Scale Giật nhẹ (Tạo cảm giác va đập mạnh)
            if (useScaleImpact)
            {
                _scaleTween = transform.DOPunchScale(Vector3.one * scalePunchAmount, fadeInDuration + fadeOutDuration, 2, 0.5f);
            }
        }
    }
}