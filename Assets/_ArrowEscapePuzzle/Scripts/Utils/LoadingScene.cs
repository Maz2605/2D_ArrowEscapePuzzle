using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace ArrowGame.Utils
{
    public class LoadingSceneVisual : MonoBehaviour
    {
        [Header("Main Elements")]
        [SerializeField] private CanvasGroup mainGroup;
        [SerializeField] private RectTransform logoTransform;

        [Header("Fast Wavy Text Effect")]
        [SerializeField] private RectTransform[] letterRects; // Danh sách các chữ cái: L, O, A, D, I, N, G

        [Header("Wavy Settings")]
        [SerializeField] private float bobHeight = 30f;      // Độ cao nảy lên (tăng lên cho rõ)
        [SerializeField] private float jumpDuration = 0.3f;  // Tốc độ nảy của 1 chữ cái (nhanh)
        [SerializeField] private float staggerDelay = 0.05f; // Độ trễ giữa các chữ (tạo hiệu ứng sóng)
        [SerializeField] private float cycleDelay = 0.5f;    // Thời gian nghỉ giữa các đợt sóng

        private void Start()
        {
            // 1. Logo đập nhẹ nhàng làm nền
            // if (logoTransform != null)
            // {
            //     logoTransform.DOScale(1f, 0.5f)
            //         .SetEase(Ease.InOutQuad)
            //         .SetLoops(-1, LoopType.Yoyo)
            //         .SetUpdate(true);
            // }

            // 2. Chạy hiệu ứng sóng chữ
            StartCoroutine(AnimateFastWaveRoutine());
        }

        private IEnumerator AnimateFastWaveRoutine()
        {
            while (true)
            {
                // Chạy qua từng chữ cái để tạo sóng
                for (int i = 0; i < letterRects.Length; i++)
                {
                    if (letterRects[i] != null)
                    {
                        AnimateLetter(letterRects[i]);
                    }
                    
                    // Độ trễ cực nhỏ để tạo cảm giác các chữ đuổi theo nhau (sóng)
                    yield return new WaitForSecondsRealtime(staggerDelay);
                }

                // Chờ một chút trước khi đợt sóng tiếp theo bắt đầu
                yield return new WaitForSecondsRealtime(cycleDelay);
            }
        }

        private void AnimateLetter(RectTransform letter)
        {
            // Sử dụng Sequence để chữ nảy lên và hạ xuống thật "mẩy"
            Sequence s = DOTween.Sequence();
            
            // Nảy lên nhanh với Ease.OutQuad
            s.Append(letter.DOAnchorPosY(bobHeight, jumpDuration / 2).SetEase(Ease.OutQuad));
            
            // Rơi xuống lại vị trí 0 với Ease.InQuad
            s.Append(letter.DOAnchorPosY(0f, jumpDuration / 2).SetEase(Ease.InQuad));
            
            s.SetUpdate(true);
            s.SetLink(letter.gameObject);
        }

        public void FadeOutAndDestroy(float duration, System.Action onComplete)
        {
            // Dừng toàn bộ Tween trên object này trước khi biến mất
            StopAllCoroutines();
            mainGroup.DOKill();
            
            mainGroup.DOFade(0f, duration).OnComplete(() => {
                onComplete?.Invoke();
            });
        }
    }
}