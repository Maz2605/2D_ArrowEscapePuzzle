using DG.Tweening;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public class PortalSpecialCellView : SpecialCellViewBase
    {
        [Header("Portal Settings")]
        [SerializeField] private Transform visualWrapper;
        
        [Tooltip("Chỉ cần kéo đúng 1 cái Prefab Master vào đây")]
        [SerializeField] private GameObject portalMasterPrefab; 
        
        [Header("Effect Tweaks")]
        [SerializeField] private float entryEffectDuration = 0.24f;
        [SerializeField] private float exitEffectDuration = 0.28f;
        [SerializeField] private float effectOffsetFactor = 0.18f;
        [SerializeField] private float idlePulseStrength = 0.08f;
        [SerializeField] private float idlePulseDuration = 1.45f;

        private PortalVisualMaster _masterVisual; // Chỉ lưu 1 object duy nhất
        
        private Sequence _portalEffectSequence;
        private Tween _idlePulseTween;
        private Tween _alphaPulseTween;
        
        private Vector3 _baseWrapperLocalPosition;
        private Quaternion _baseWrapperLocalRotation;
        private Vector3 _baseWrapperLocalScale = Vector3.one;
        private float _alphaMultiplier = 1f;

        protected override void Awake()
        {
            base.Awake();
            EnsureWrapperReference();
            CacheWrapperState();
        }

        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            EnsureWrapperReference();
            ApplyPortalRotation(specialCell.PortalDirection);
            StartIdlePulse();
        }

        // Đã xóa các hàm OnVisualColorChanged vì ta không xài Tint đổi màu nữa

        protected override void OnSpawnedFromPool()
        {
            EnsureWrapperReference();
            CacheWrapperState();
            ResetAnimatedVisualState();
        }

        protected override void OnDespawnedToPool()
        {
            KillPortalTweens();

            // Trả cục Master về Pool
            if (_masterVisual != null)
            {
                PoolingManager.Instance.Despawn(_masterVisual.gameObject);
                _masterVisual = null;
            }

            if (visualWrapper != null)
            {
                visualWrapper.localPosition = _baseWrapperLocalPosition;
                visualWrapper.localRotation = _baseWrapperLocalRotation;
                visualWrapper.localScale = _baseWrapperLocalScale;
            }
        }

        public void SetExactVariant(int exactIndex)
        {
            if (portalMasterPrefab == null || !EnsureWrapperReference()) return;

            if (_masterVisual == null)
            {
                GameObject spawnedObj = PoolingManager.Instance.Spawn(portalMasterPrefab, Vector3.zero, Quaternion.identity, visualWrapper);
                _masterVisual = spawnedObj.GetComponent<PortalVisualMaster>();
            }

            // Bảo mật: Lấy modulo theo số lượng màu thực tế của Prefab (VD: 5 màu)
            // Giúp game không bị crash nếu Level Designer lỡ tay đặt 6 cặp portal vào map.
            int safeIndex = _masterVisual.VariantCount > 0 ? (exactIndex % _masterVisual.VariantCount) : 0;
            _masterVisual.ShowVariant(safeIndex);
        }

        public void PlayPortalEntryEffect(Direction4 travelDirection)
        {
            if (!EnsureWrapperReference()) return;

            KillPortalTweens();
            CacheWrapperState();

            Vector3 offset = GetTravelOffset(travelDirection);
            visualWrapper.localPosition = _baseWrapperLocalPosition - (offset * 0.16f);
            visualWrapper.localScale = _baseWrapperLocalScale * 1.06f;
            ApplyAlphaMultiplier(0.9f);

            _portalEffectSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _portalEffectSequence.Append(visualWrapper.DOLocalMove(_baseWrapperLocalPosition + (offset * 0.08f), entryEffectDuration * 0.32f).SetEase(Ease.InQuad));
            _portalEffectSequence.Join(visualWrapper.DOScale(_baseWrapperLocalScale * 0.76f, entryEffectDuration * 0.32f).SetEase(Ease.InQuad));
            
            if (_masterVisual != null)
                _portalEffectSequence.Join(_masterVisual.transform.DOScale(0.9f, entryEffectDuration * 0.28f).SetEase(Ease.InQuad));
            
            _portalEffectSequence.Append(visualWrapper.DOLocalMove(_baseWrapperLocalPosition, entryEffectDuration * 0.68f).SetEase(Ease.OutBack));
            _portalEffectSequence.Join(visualWrapper.DOScale(_baseWrapperLocalScale * 1.08f, entryEffectDuration * 0.44f).SetEase(Ease.OutBack));
            
            if (_masterVisual != null)
                _portalEffectSequence.Join(_masterVisual.transform.DOScale(1.08f, entryEffectDuration * 0.4f).SetEase(Ease.OutBack));
            
            _portalEffectSequence.Append(visualWrapper.DOScale(_baseWrapperLocalScale, entryEffectDuration * 0.18f).SetEase(Ease.OutSine));
            
            if (_masterVisual != null)
                _portalEffectSequence.Join(_masterVisual.transform.DOScale(1f, entryEffectDuration * 0.18f).SetEase(Ease.OutSine));
            
            _portalEffectSequence.Join(DOTween.To(() => _alphaMultiplier, ApplyAlphaMultiplier, 1.12f, entryEffectDuration * 0.24f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            _portalEffectSequence.OnComplete(StartIdlePulse);
        }

        public void PlayPortalExitEffect(Direction4 travelDirection)
        {
            if (!EnsureWrapperReference()) return;

            KillPortalTweens();
            CacheWrapperState();

            Vector3 offset = GetTravelOffset(travelDirection);
            visualWrapper.localPosition = _baseWrapperLocalPosition;
            visualWrapper.localScale = _baseWrapperLocalScale * 0.82f;
            ApplyAlphaMultiplier(0.95f);

            _portalEffectSequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _portalEffectSequence.Append(visualWrapper.DOScale(_baseWrapperLocalScale * 1.24f, exitEffectDuration * 0.34f).SetEase(Ease.OutBack));
            _portalEffectSequence.Join(visualWrapper.DOLocalMove(_baseWrapperLocalPosition + (offset * 0.16f), exitEffectDuration * 0.34f).SetEase(Ease.OutQuad));
            
            if (_masterVisual != null)
                _portalEffectSequence.Join(_masterVisual.transform.DOScale(1.14f, exitEffectDuration * 0.36f).SetEase(Ease.OutBack));
            
            _portalEffectSequence.Append(visualWrapper.DOLocalMove(_baseWrapperLocalPosition, exitEffectDuration * 0.42f).SetEase(Ease.OutSine));
            _portalEffectSequence.Join(visualWrapper.DOScale(_baseWrapperLocalScale, exitEffectDuration * 0.42f).SetEase(Ease.OutQuad));
            
            if (_masterVisual != null)
                _portalEffectSequence.Join(_masterVisual.transform.DOScale(1f, exitEffectDuration * 0.32f).SetEase(Ease.OutSine));
            
            _portalEffectSequence.Join(DOTween.To(() => _alphaMultiplier, ApplyAlphaMultiplier, 1.18f, exitEffectDuration * 0.2f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            _portalEffectSequence.OnComplete(StartIdlePulse);
        }

        private void StartIdlePulse()
        {
            _idlePulseTween?.Kill();
            _alphaPulseTween?.Kill();
            if (!EnsureWrapperReference()) return;

            visualWrapper.localScale = _baseWrapperLocalScale;
            _idlePulseTween = visualWrapper.DOScale(_baseWrapperLocalScale * (1f + idlePulseStrength), idlePulseDuration)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            _alphaPulseTween = DOTween.To(() => _alphaMultiplier, ApplyAlphaMultiplier, 1.06f, idlePulseDuration * 0.5f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void ApplyAlphaMultiplier(float multiplier)
        {
            _alphaMultiplier = multiplier;
            if (_masterVisual != null) _masterVisual.ApplyAlpha(_alphaMultiplier);
        }

        // --- Các hàm Utility giữ nguyên ---
        private void ApplyPortalRotation(Direction4 direction)
        {
            if (!EnsureWrapperReference()) return;
            Vector3 rotationEuler = direction switch
            {
                Direction4.Up => new Vector3(26f, 0f, 0f),
                Direction4.Down => new Vector3(-26f, 0f, 180f),
                Direction4.Left => new Vector3(0f, 26f, 90f),
                Direction4.Right => new Vector3(0f, -26f, -90f),
                _ => Vector3.zero
            };
            visualWrapper.localRotation = Quaternion.Euler(rotationEuler);
            _baseWrapperLocalRotation = visualWrapper.localRotation;
        }

        private int ResolvePrefabIndex(string portalId)
        {
            // Trong thực tế, bạn có thể hard-code count = 5 nếu biết chắc số lượng màu
            int count = 5; 
            if (int.TryParse(portalId, out int idNumber)) return Mathf.Abs(idNumber - 1) % count;
            if (!string.IsNullOrEmpty(portalId)) return (portalId.GetHashCode() & 0x7fffffff) % count;
            return 0;
        }

        private void KillPortalTweens()
        {
            _portalEffectSequence?.Kill();
            _idlePulseTween?.Kill();
            _alphaPulseTween?.Kill();
            
            if (visualWrapper != null)
            {
                visualWrapper.DOKill();
                visualWrapper.localPosition = _baseWrapperLocalPosition;
                visualWrapper.localRotation = _baseWrapperLocalRotation;
                visualWrapper.localScale = _baseWrapperLocalScale;
            }

            if (_masterVisual != null)
            {
                _masterVisual.transform.DOKill();
                _masterVisual.transform.localPosition = Vector3.zero;
                _masterVisual.transform.localRotation = Quaternion.identity;
                _masterVisual.transform.localScale = Vector3.one;
            }

            _alphaMultiplier = 1f;
            ApplyAlphaMultiplier(1f);
        }

        private void CacheWrapperState()
        {
            if (!EnsureWrapperReference()) return;
            _baseWrapperLocalPosition = visualWrapper.localPosition;
            _baseWrapperLocalRotation = visualWrapper.localRotation;
            _baseWrapperLocalScale = visualWrapper.localScale;
        }

        private bool EnsureWrapperReference()
        {
            if (visualWrapper != null) return true;
            Transform wrapperCandidate = transform.Find("VisualWrapper");
            visualWrapper = wrapperCandidate != null ? wrapperCandidate : transform;
            return visualWrapper != null;
        }

        private void ResetAnimatedVisualState()
        {
            _alphaMultiplier = 1f;
            if (visualWrapper != null)
            {
                visualWrapper.localPosition = _baseWrapperLocalPosition;
                visualWrapper.localRotation = _baseWrapperLocalRotation;
                visualWrapper.localScale = _baseWrapperLocalScale;
            }

            if (_masterVisual != null)
            {
                _masterVisual.transform.localPosition = Vector3.zero;
                _masterVisual.transform.localRotation = Quaternion.identity;
                _masterVisual.transform.localScale = Vector3.one;
            }
        }

        private Vector3 GetTravelOffset(Direction4 travelDirection)
        {
            Vector3 direction = travelDirection.ToVector3();
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector3.up;
            return direction.normalized * (CellSize * effectOffsetFactor);
        }
        
        public override void PlayHighlight() => PlayPortalEntryEffect(Direction4.Up);
    }
}