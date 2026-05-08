using ShareCore.Scripts.Data;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.Gameplay.Visual
{
    public abstract class SpecialCellViewBase : MonoBehaviour
    {
        private static Sprite _sharedSprite;

        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private TextMesh _label;

        private static readonly int FlashIntensityId = Shader.PropertyToID("_FlashIntensity");
        private MaterialPropertyBlock _mpb;
        private float _currentFlashIntensity;
        private Tween _flashTween;
        private Vector3 _targetScale;

        protected virtual void Awake()
        {
            EnsureReferences();
            _mpb = new MaterialPropertyBlock();
        }

        public void SetFlashIntensity(float intensity)
        {
            _currentFlashIntensity = intensity;
            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FlashIntensityId, _currentFlashIntensity);
                _backgroundRenderer.SetPropertyBlock(_mpb);
            }
        }

        public virtual void PlayHighlight()
        {
            _flashTween?.Kill();
            SetFlashIntensity(0f);
            
            _flashTween = DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x), 1f, 0.05f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _flashTween = DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x), 0f, 0.15f)
                        .SetEase(Ease.InQuad);
                });
        }

        protected virtual void EnsureReferences()
        {
            if (_backgroundRenderer == null) _backgroundRenderer = GetComponent<SpriteRenderer>();
            if (_backgroundRenderer == null) _backgroundRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (_backgroundRenderer != null)
            {
                if (_backgroundRenderer.sprite == null) _backgroundRenderer.sprite = GetSharedSprite();
            }

            if (_label != null) return;

            Transform labelTransform = transform.Find("Label");
            if (labelTransform != null) _label = labelTransform.GetComponent<TextMesh>();

            if (_label == null)
            {
                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(transform, false);
                _label = labelObject.AddComponent<TextMesh>();
            }

            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 48;
            _label.characterSize = 0.075f;
            _label.color = Color.white;
        }

        public void Setup(SpecialCellSaveData specialCell, float cellSize, Color color)
        {
            if (specialCell == null) return;
            EnsureReferences();

            transform.localPosition = new Vector3(specialCell.Position.x * cellSize, specialCell.Position.y * cellSize, 0f);
            _targetScale = Vector3.one * (cellSize * DefaultScaleMultiplier);
            transform.localScale = _targetScale;
            
            // Set Flash Color bằng màu của view
            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(Shader.PropertyToID("_FlashColor"), color);
                _backgroundRenderer.SetPropertyBlock(_mpb);
            }

            ApplyVisual(specialCell, color);
        }

        public void PlaySpawnAnimation(float delay, float duration)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(_targetScale, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void PlayWinAnimation(float delay, float winJumpHeight, float winJumpUpDuration, float winFallDownDuration, float winScaleMax)
        {
            Vector3 originalPos = transform.localPosition;
            Sequence seq = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            
            seq.Append(transform.DOLocalMoveY(originalPos.y + winJumpHeight, winJumpUpDuration).SetEase(Ease.OutQuad));
            seq.Join(transform.DOScale(_targetScale * winScaleMax, winJumpUpDuration).SetEase(Ease.OutQuad));
            seq.Append(transform.DOLocalMoveY(originalPos.y, winFallDownDuration).SetEase(Ease.InQuad));
            seq.Join(transform.DOScale(_targetScale, winFallDownDuration).SetEase(Ease.OutBounce));
            
            seq.SetDelay(delay);
        }

        public void PlayLoseAnimation(float duration, float scaleTarget, Color loseColor)
        {
            transform.DOScale(_targetScale * scaleTarget, duration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                
            if (_backgroundRenderer != null)
            {
                _backgroundRenderer.DOColor(loseColor, duration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        protected SpriteRenderer BackgroundRenderer => _backgroundRenderer;
        protected TextMesh Label => _label;
        protected virtual float DefaultScaleMultiplier => 0.65f;
        protected abstract void ApplyVisual(SpecialCellSaveData specialCell, Color color);

        private static Sprite GetSharedSprite()
        {
            if (_sharedSprite != null) return _sharedSprite;

            Texture2D texture = Texture2D.whiteTexture;
            Rect rect = new Rect(0f, 0f, texture.width, texture.height);
            _sharedSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), texture.width);
            return _sharedSprite;
        }
    }
}
