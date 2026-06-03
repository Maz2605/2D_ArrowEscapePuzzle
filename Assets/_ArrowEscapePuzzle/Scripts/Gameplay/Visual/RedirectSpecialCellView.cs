using DG.Tweening;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public class RedirectSpecialCellView : SpecialCellViewBase
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer glowRenderer;

        private Tween _flashTween;
        private Tween _glowScaleTween;
        private Tween _glowFadeTween;
        private Tween _colorTween;
        private Color _baseColor = Color.white;

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

        protected override void Awake()
        {
            base.Awake();

            if (backgroundRenderer == null)
            {
                backgroundRenderer = GetComponent<SpriteRenderer>();
            }
        }

        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            _baseColor = color;
            _colorTween?.Kill();

            float rotation = specialCell.ExitDirection switch
            {
                Direction4.Up => 0f,
                Direction4.Right => -90f,
                Direction4.Down => 180f,
                Direction4.Left => 90f,
                _ => 0f
            };
            transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            ApplyDisplayColor(_baseColor);

            if (glowRenderer != null)
            {
                glowRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
                glowRenderer.transform.localScale = Vector3.one;
            }
        }

        public override void PlayHighlight()
        {
            _flashTween?.Kill();
            _glowScaleTween?.Kill();
            _glowFadeTween?.Kill();
            transform.DOKill(true);

            ApplyFlashIntensity(0f);
            _flashTween = DOVirtual.Float(0f, 1f, flashDuration, t =>
            {
                float intensity = flashCurve.Evaluate(t);
                ApplyFlashIntensity(intensity);
            }).SetEase(Ease.Linear);

            if (glowRenderer != null)
            {
                glowRenderer.transform.localScale = Vector3.one;
                glowRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, glowMaxAlpha);

                _glowScaleTween = glowRenderer.transform.DOScale(glowMaxScale, glowDuration).SetEase(Ease.OutQuad);
                _glowFadeTween = glowRenderer.DOFade(0f, glowDuration).SetEase(Ease.OutQuad);
            }

            transform.DOPunchScale(punchAmount, punchDuration, punchVibrato, punchElasticity)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        protected override void ApplyFlashIntensity(float intensity)
        {
            Color litColor = Color.Lerp(_baseColor, Color.white, Mathf.Clamp01(intensity * 0.85f));
            ApplyDisplayColor(litColor);
        }

        protected override void OnDespawnedToPool()
        {
            _flashTween?.Kill();
            _glowScaleTween?.Kill();
            _glowFadeTween?.Kill();
            _colorTween?.Kill();

            if (glowRenderer != null)
            {
                glowRenderer.DOKill();
                glowRenderer.transform.localScale = Vector3.one;
                glowRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
            }

            ApplyDisplayColor(_baseColor);
        }

        protected override void OnVisualColorChanged(Color color)
        {
            _baseColor = color;
            _colorTween?.Kill();
            ApplyDisplayColor(color);
        }

        protected override void OnLoseColorChanged(Color loseColor, float duration)
        {
            _colorTween?.Kill();
            Color startColor = backgroundRenderer != null ? backgroundRenderer.color : _baseColor;
            _colorTween = DOTween.To(() => startColor, ApplyDisplayColor, loseColor, duration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void ApplyDisplayColor(Color color)
        {
            if (backgroundRenderer != null)
            {
                backgroundRenderer.color = color;
            }

            if (glowRenderer != null)
            {
                glowRenderer.color = new Color(color.r, color.g, color.b, glowRenderer.color.a);
            }
        }
    }
}
