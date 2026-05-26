using ArrowGame.Data.Events;
using ShareCore.Scripts.Data;
using UnityEngine;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;

namespace ArrowGame.Gameplay.Visual
{
    public abstract class SpecialCellViewBase : MonoBehaviour, IPoolable
    {
        [Header("Scale Settings")]
        [SerializeField] protected float defaultScaleMultiplier = 0.65f;

        private float _currentFlashIntensity;
        private Tween _flashTween;
        private SpecialCellSaveData _boundSpecialCell;
        
        protected Vector3 TargetScale;
        protected float CellSize;

        public SpecialCellSaveData BoundSpecialCell => _boundSpecialCell;
        protected virtual float DefaultScaleMultiplier => defaultScaleMultiplier;

        protected virtual void Awake()
        {
            // Base class không còn quản lý MaterialPropertyBlock nữa
        }

        public virtual void PlayHighlight()
        {
            _flashTween?.Kill();
            ApplyFlashIntensity(0f);
            
            _currentFlashIntensity = 0f;
            _flashTween = DOTween.To(() => _currentFlashIntensity, x => 
                {
                    _currentFlashIntensity = x;
                    ApplyFlashIntensity(x);
                }, 1f, 0.05f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _flashTween = DOTween.To(() => _currentFlashIntensity, x => 
                        {
                            _currentFlashIntensity = x;
                            ApplyFlashIntensity(x);
                        }, 0f, 0.15f)
                        .SetEase(Ease.InQuad);
                });
        }

        /// <summary>
        /// Các class con (như Redirect) nếu cần nháy sáng (Flash) thì override hàm này để đổi Material
        /// </summary>
        protected virtual void ApplyFlashIntensity(float intensity) { }
        
        public virtual void PlayRejectionAnimation()
        {
            transform.DOKill();
            transform.localScale = TargetScale;
            Vector3 originalLocalPosition = transform.localPosition;
            transform.localPosition = originalLocalPosition;
            
            // Lắc từ chối (shake side to side)
            transform.DOPunchPosition(new Vector3(0.12f, 0f, 0f), 0.3f, 15, 1f)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(() => transform.localPosition = originalLocalPosition)
                .OnComplete(() => transform.localPosition = originalLocalPosition);
            
            // Bắn sự kiện visual để controller tự xử lý âm thanh và haptic
            EventManager<VisualEventID>.Post(VisualEventID.SpecialCellRejection);
        }

        public void Setup(SpecialCellSaveData specialCell, float cellSize, Color color)
        {
            if (specialCell == null) return;
            _boundSpecialCell = specialCell;
            CellSize = cellSize;

            // Tính toán vị trí và scale chung cho mọi ô đặc biệt
            transform.localPosition = new Vector3(specialCell.Position.x * cellSize, specialCell.Position.y * cellSize, 0f);
            TargetScale = Vector3.one * (cellSize * DefaultScaleMultiplier);
            transform.localScale = TargetScale;
            
            // Gọi lớp con tự xử lý màu sắc và các visual đặc thù
            ApplyVisual(specialCell, color);
        }

        public void RefreshVisualColor(Color color)
        {
            // Lớp con tự bắt sự kiện và đổi màu theo ý muốn
            OnVisualColorChanged(color);
        }

        public void PlaySpawnAnimation(float delay, float duration)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(TargetScale, duration)
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
            seq.Join(transform.DOScale(TargetScale * winScaleMax, winJumpUpDuration).SetEase(Ease.OutQuad));
            seq.Append(transform.DOLocalMoveY(originalPos.y, winFallDownDuration).SetEase(Ease.InQuad));
            seq.Join(transform.DOScale(TargetScale, winFallDownDuration).SetEase(Ease.OutBounce));
            
            seq.SetDelay(delay);
        }

        public virtual void PlayLoseAnimation(float duration, float scaleTarget, Color loseColor)
        {
            transform.DOScale(TargetScale * scaleTarget, duration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
                
            // Báo cho lớp con tự đổi màu lose nếu cần
            OnLoseColorChanged(loseColor, duration);
        }

        public virtual void OnSpawn()
        {
            transform.DOKill();
            ApplyFlashIntensity(0f);
            OnSpawnedFromPool();
        }

        public virtual void OnDespawn()
        {
            transform.DOKill();
            _flashTween?.Kill();

            _boundSpecialCell = null;
            ApplyFlashIntensity(0f);
            
            // Reset các giá trị Transform về mặc định
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            
            OnDespawnedToPool();
        }

        protected virtual void OnSpawnedFromPool() { }
        protected virtual void OnDespawnedToPool() { }
        protected virtual void OnVisualColorChanged(Color color) { }
        protected virtual void OnLoseColorChanged(Color loseColor, float duration) { }
        protected abstract void ApplyVisual(SpecialCellSaveData specialCell, Color color);
    }
}