using ArrowGame.Data.Events;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.VFX
{
    public class SuperHardVFXBehavior : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private RectTransform flameIcon; 
        [SerializeField] private RectTransform lightRays;
        [SerializeField] private Image glowBackground;
        [SerializeField] private ParticleSystem ghostParticles;
        [SerializeField] private TextMeshProUGUI superHardText; 

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
            if (flameIcon != null) flameIcon.localScale = Vector3.zero;
            if (superHardText != null) superHardText.transform.localScale = Vector3.zero;
            
            // Reset Alpha
            if (superHardText != null) superHardText.alpha = 0f;
            
            if (glowBackground != null)
            {
                Color glowColor = glowBackground.color;
                glowColor.a = 0f; 
                glowBackground.color = glowColor;
            }
        }

        private void PlayLoopingEffects()
        {
            if (ghostParticles != null)
            {
                ghostParticles.Play();
            }

            // 1. Tia sáng xoay vô tận
            if (lightRays != null)
            {
                _rayRotationTween = lightRays.DORotate(new Vector3(0, 0, -360), 3f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
            }

            // 2. Nền sáng (Glow) đập nhịp nhàng từ Alpha 0 -> 0.3
            if (glowBackground != null)
            {
                _glowPulseTween = glowBackground.DOFade(0.3f, 0.8f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        private void PlayIntroSequence()
        {
            _introSequence?.Kill(); 
            _introSequence = DOTween.Sequence();

            // Giai đoạn 1: Nảy lên xuất hiện (0.5s)
            if (flameIcon != null)
                _introSequence.Append(flameIcon.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack));
            
            if (superHardText != null)
            {
                _introSequence.Join(superHardText.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
                _introSequence.Join(superHardText.DOFade(1f, 0.3f));
            }

            // Giai đoạn 2: Rung chữ đe dọa (0.3s)
            if (superHardText != null)
                _introSequence.Append(superHardText.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.3f, 5, 1));

            // Giai đoạn 3: Giữ nguyên để người chơi đọc chữ (1.5s)
            _introSequence.AppendInterval(1.5f);

            // Giai đoạn 4: Thu nhỏ biến mất (0.3s)
            if (flameIcon != null)
                _introSequence.Append(flameIcon.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
            
            if (superHardText != null)
            {
                _introSequence.Join(superHardText.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
                _introSequence.Join(superHardText.DOFade(0f, 0.2f));
            }
            
            if (glowBackground != null)
                _introSequence.Join(glowBackground.DOFade(0f, 0.3f));

            // Giai đoạn 5: Báo cáo hoàn thành
            _introSequence.OnComplete(() =>
            {
                // Bắn Event thông báo Difficulty Intro đã hoàn thành xong animation
                EventManager<VisualEventID>.Post(VisualEventID.DifficultyIntroComplete);
            });
        }

        private void StopAllEffects()
        {
            if (ghostParticles != null)
            {
                ghostParticles.Stop();
                ghostParticles.Clear();
            }
            
            _introSequence?.Kill();
            _rayRotationTween?.Kill();
            _glowPulseTween?.Kill();
        }
    }
}