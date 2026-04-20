using DG.Tweening;
using UnityEngine;

namespace ArrowGame.VFX
{
    public class GateBlackHoleVFX : MonoBehaviour
    {
        [Header("--- Lớp Sprite ---")]
        [SerializeField] private Transform holeTransform; 
        [SerializeField] private Transform rimTransform;  

        [Header("--- 2.5D Perspective ---")]
        [SerializeField] private Vector3 targetScale = new Vector3(0.6f, 1.8f, 1f); 
        [SerializeField] private float appearDuration = 0.3f; 
        [SerializeField] private float disappearDuration = 0.3f; 

        private Sequence _visualSequence;

        private void OnEnable()
        {
            KillAllTweens();

            transform.localScale = Vector3.zero;
            
            holeTransform.DORotate(new Vector3(0, 0, -360f), 1f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);
            rimTransform.DORotate(new Vector3(0, 0, 180f), 1f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);

            _visualSequence = DOTween.Sequence()
                .Append(transform.DOScale(targetScale, appearDuration).SetEase(Ease.OutBack))
                .AppendInterval(1f - disappearDuration - appearDuration) 
                .Append(transform.DOScale(Vector3.zero, disappearDuration).SetEase(Ease.InQuad))
                .SetLink(gameObject);
        }

        private void OnDisable() => KillAllTweens();

        private void KillAllTweens()
        {
            _visualSequence?.Kill();
            transform.DOKill();
            holeTransform.DOKill();
            rimTransform.DOKill();
        }
    }
}