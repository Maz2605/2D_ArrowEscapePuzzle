using ArrowGame.Data.Events;
using ShareCore.Scripts.Data;
using UnityEngine;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro; 
namespace ArrowGame.Gameplay.Visual
{
    public abstract class SpecialCellViewBase : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected SpriteRenderer backgroundRenderer;
        [SerializeField] protected TextMeshPro label;

        [Header("Scale Settings")]
        [SerializeField] protected float defaultScaleMultiplier = 0.65f;

        public enum SpecialCellColorMode
        {
            UseTheme,       // Không đè màu (để VisualThemeReceiver tự gán)
            CustomOverride  // Tự đè màu (gán biến customColor)
        }

        [Header("Color Settings")]
        [SerializeField] protected SpecialCellColorMode colorMode = SpecialCellColorMode.UseTheme;
        [SerializeField] protected Color customColor = Color.gray;

        private static readonly int FlashIntensityId = Shader.PropertyToID("_FlashIntensity");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        
        private MaterialPropertyBlock _mpb;
        private float _currentFlashIntensity;
        private Tween _flashTween;
        protected Vector3 TargetScale;

        protected virtual void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            
            // Backup reference tìm tự động nếu quên kéo thả trên Inspector
            if (backgroundRenderer == null) backgroundRenderer = GetComponentInChildren<SpriteRenderer>();
            if (label == null) label = GetComponentInChildren<TextMeshPro>();
        }

        public void SetFlashIntensity(float intensity)
        {
            _currentFlashIntensity = intensity;
            if (backgroundRenderer != null)
            {
                backgroundRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FlashIntensityId, _currentFlashIntensity);
                backgroundRenderer.SetPropertyBlock(_mpb);
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
        
        public virtual void PlayRejectionAnimation()
        {
            transform.DOKill();
            transform.localScale = TargetScale;
            
            // Lắc từ chối (shake side to side)
            transform.DOPunchPosition(new Vector3(0.12f, 0f, 0f), 0.3f, 15, 1f);
            
            // Bắn sự kiện visual để controller tự xử lý âm thanh và haptic
            EventManager<VisualEventID>.Post(VisualEventID.SpecialCellRejection);
        }

        public void Setup(SpecialCellSaveData specialCell, float cellSize, Color color)
        {
            if (specialCell == null) return;

            transform.localPosition = new Vector3(specialCell.Position.x * cellSize, specialCell.Position.y * cellSize, 0f);
            TargetScale = Vector3.one * (cellSize * DefaultScaleMultiplier);
            transform.localScale = TargetScale;
            
            Color finalColor = color;
            
            if (colorMode == SpecialCellColorMode.CustomOverride)
            {
                finalColor = customColor;
                if (backgroundRenderer != null)
                {
                    backgroundRenderer.color = finalColor;
                }
            }
            else
            {
                // Nếu dùng UseTheme, ta ưu tiên lấy màu hiện tại của SpriteRenderer (do VisualThemeReceiver gán)
                if (backgroundRenderer != null)
                {
                    finalColor = backgroundRenderer.color;
                    // Nếu màu hiện tại là trắng hoặc trong suốt (chưa khởi tạo), dùng màu hệ thống
                    if (finalColor == Color.white || finalColor.a == 0f)
                        finalColor = color;
                }
            }
            
            // Set Flash Color bằng màu cuối cùng
            if (backgroundRenderer != null)
            {
                backgroundRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(FlashColorId, finalColor);
                backgroundRenderer.SetPropertyBlock(_mpb);
            }

            ApplyVisual(specialCell, finalColor);
        }

        public void PlaySpawnAnimation(float delay, float duration)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(TargetScale, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        // --- GIỮ NGUYÊN WIN ANIMATION ---
        public void PlayWinAnimation(float delay, float winJumpHeight, float winJumpUpDuration, float winFallDownDuration, float winScaleMax)
        {
            Vector3 originalPos = transform.localPosition;
            Sequence seq = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            
            seq.Append(transform.DOLocalMoveY(originalPos.y + winJumpHeight, winJumpUpDuration).SetEase(Ease.OutQuad));
            seq.Join(transform.DOScale(TargetScale * winScaleMax, winJumpUpDuration).SetEase(Ease.OutQuad));
            seq.Append(transform.DOLocalMoveY(originalPos.y, winFallDownDuration).SetEase(Ease.InQuad));
            seq.Join(transform.DOScale(TargetScale, winFallDownDuration).SetEase(Ease.OutBounce));
            
            seq.SetDelay(delay);
        }

        // --- GIỮ NGUYÊN LOSE ANIMATION ---
        public void PlayLoseAnimation(float duration, float scaleTarget, Color loseColor)
        {
            transform.DOScale(TargetScale * scaleTarget, duration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                
            if (backgroundRenderer != null)
            {
                backgroundRenderer.DOColor(loseColor, duration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
        }

        protected SpriteRenderer BackgroundRenderer => backgroundRenderer;
        protected TextMeshPro Label => label;
        protected virtual float DefaultScaleMultiplier => defaultScaleMultiplier;
        protected abstract void ApplyVisual(SpecialCellSaveData specialCell, Color color);
    }
}