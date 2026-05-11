using DG.Tweening;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public class RedirectSpecialCellView : SpecialCellViewBase
    {
        [Header("References (Optional)")]
        [Tooltip("Nếu không kéo thả, code sẽ tự sinh ra Glow từ Sprite của Background")]
        [SerializeField] private SpriteRenderer glowRenderer;
        
        private Tween _flashTween;
        private Tween _glowScaleTween;
        private Tween _glowFadeTween;

        [Header("Flash Effect")]
        public float flashDuration = 0.2f;
        public AnimationCurve flashCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)
        );

        [Header("Glow Effect")]
        public float glowDuration = 0.4f;
        public float glowMaxScale = 1.5f;
        public float glowMaxAlpha = 0.6f;

        [Header("Punch Effect")]
        public float punchDuration = 0.3f;
        public Vector3 punchAmount = Vector3.one * 0.2f;
        public int punchVibrato = 10;
        public float punchElasticity = 1f;

        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            if (BackgroundRenderer != null) BackgroundRenderer.color = color;
            
            // Xoay prefab theo hướng ExitDirection
            float rotation = specialCell.ExitDirection switch
            {
                Direction4.Up => 0f,
                Direction4.Right => -90f,
                Direction4.Down => 180f,
                Direction4.Left => 90f,
                _ => 0f
            };
            transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            if (Label != null)
            {
                Label.gameObject.SetActive(false);
            }

            // Tạo object glow nếu chưa có và không được kéo thả
            if (glowRenderer == null && BackgroundRenderer != null)
            {
                GameObject go = new GameObject("Glow");
                go.transform.SetParent(transform, false);
                glowRenderer = go.AddComponent<SpriteRenderer>();
                glowRenderer.sprite = BackgroundRenderer.sprite;
                glowRenderer.sortingOrder = BackgroundRenderer.sortingOrder - 1; // Đằng sau
            }
            
            if (glowRenderer != null)
            {
                glowRenderer.color = new Color(color.r, color.g, color.b, 0f); // Mặc định ẩn
                glowRenderer.transform.localScale = Vector3.one;
            }
        }

        public override void PlayHighlight()
        {
            // Kill các tween cũ để tránh tranh chấp
            _flashTween?.Kill();
            _glowScaleTween?.Kill();
            _glowFadeTween?.Kill();
            transform.DOKill(true); // Hoàn thành nhanh punch cũ và trả về scale gốc
            
            // Tự xử lý Flash bằng curve
            SetFlashIntensity(0f);
            _flashTween = DOVirtual.Float(0f, 1f, flashDuration, t => 
            {
                float intensity = flashCurve.Evaluate(t);
                SetFlashIntensity(intensity);
            }).SetEase(Ease.Linear);
            
            if (glowRenderer != null)
            {
                glowRenderer.transform.localScale = Vector3.one;
                glowRenderer.color = new Color(glowRenderer.color.r, glowRenderer.color.g, glowRenderer.color.b, glowMaxAlpha);
                
                _glowScaleTween = glowRenderer.transform.DOScale(glowMaxScale, glowDuration).SetEase(Ease.OutQuad);
                _glowFadeTween = glowRenderer.DOFade(0f, glowDuration).SetEase(Ease.OutQuad);
            }
            
            // Hiệu ứng bóp méo (Punch Scale) khi mũi tên đi qua
            transform.DOPunchScale(punchAmount, punchDuration, punchVibrato, punchElasticity);
        }
    }
}
