using ArrowGame.Data.Events;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;

namespace ArrowGame.VFX
{
    public class SuperHardVFXBehavior : MonoBehaviour
    {
        [Header("--- References ---")]
        [SerializeField] private Transform flameIcon; 
        [SerializeField] private Transform lightRays;
        [SerializeField] private SpriteRenderer glowBackground;
        [SerializeField] private ParticleSystem ghostParticles;
        // Nếu bạn dùng Canvas UI thì để TextMeshProUGUI, dùng World Space thì để TextMeshPro
        [SerializeField] private TextMeshPro superHardText; 

        // Cache Tween để quản lý vòng đời (Memory Management)
        private Sequence _introSequence;
        private Tween _rayRotationTween;
        private Tween _glowPulseTween;

        /// <summary>
        /// Được gọi khi PoolingManager.Instance.Spawn() kích hoạt object này
        /// </summary>
        private void OnEnable()
        {
            ResetInitialState();
            PlayLoopingEffects();
            PlayIntroSequence();
        }

        /// <summary>
        /// Được gọi khi PoolingManager.Instance.Despawn() tắt object này
        /// </summary>
        private void OnDisable()
        {
            StopAllEffects();
        }

        private void ResetInitialState()
        {
            // Reset Scale
            flameIcon.localScale = Vector3.zero;
            superHardText.transform.localScale = Vector3.zero;
            
            // Reset Alpha
            superHardText.alpha = 0f;
            
            Color glowColor = glowBackground.color;
            glowColor.a = 0f; 
            glowBackground.color = glowColor;
        }

        private void PlayLoopingEffects()
        {
            if (ghostParticles != null)
            {
                ghostParticles.Play();
            }

            // 1. Tia sáng xoay vô tận
            _rayRotationTween = lightRays.DORotate(new Vector3(0, 0, -360), 3f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);

            // 2. Nền sáng (Glow) đập nhịp nhàng từ Alpha 0 -> 0.3
            _glowPulseTween = glowBackground.DOFade(0.3f, 0.8f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void PlayIntroSequence()
        {
            _introSequence?.Kill(); 
            _introSequence = DOTween.Sequence();

            // Giai đoạn 1: Nảy lên xuất hiện (0.5s)
            _introSequence.Append(flameIcon.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack));
            _introSequence.Join(superHardText.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
            _introSequence.Join(superHardText.DOFade(1f, 0.3f));

            // Giai đoạn 2: Rung chữ đe dọa (0.3s)
            _introSequence.Append(superHardText.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.3f, 5, 1));

            // Giai đoạn 3: Giữ nguyên để người chơi đọc chữ (1.5s)
            _introSequence.AppendInterval(1.5f);

            // Giai đoạn 4: Thu nhỏ biến mất (0.3s)
            _introSequence.Append(flameIcon.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
            _introSequence.Join(superHardText.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
            _introSequence.Join(superHardText.DOFade(0f, 0.2f));
            _introSequence.Join(glowBackground.DOFade(0f, 0.3f));

            // Giai đoạn 5: Báo cáo hoàn thành
            _introSequence.OnComplete(() =>
            {
                // // Bắn Event đi. Manager của bạn sẽ bắt được ở hàm HandleIntroAnimationComplete()
                // // và tự động gọi DespawnActiveInstance() thu hồi Prefab này về Pool.
                // EventManager<VisualEventID>.Post(VisualEventID.IntroAnimationComplete);
            });
        }

        private void StopAllEffects()
        {
            if (ghostParticles != null)
            {
                ghostParticles.Stop();
                ghostParticles.Clear(); // Dọn sạch các hạt đang bay dở để lần spawn sau bắt đầu lại sạch sẽ
            }
            
            // Phải Kill Tween để tránh memory leak và lỗi MissingReference
            _introSequence?.Kill();
            _rayRotationTween?.Kill();
            _glowPulseTween?.Kill();
        }
    }
}