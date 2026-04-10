using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ArrowGame.UI.VFX
{
    public class UISparkleEffect : MonoBehaviour
    {
        [Header("--- Components ---")]
        [SerializeField] private Image starGlow;
        [SerializeField] private List<Image> glintImages;
        
        [Header("--- Settings ---")]
        [SerializeField] private float glintSpawnRadius = 25f;

        private Sequence _fxSequence;

        // Gọi hàm này khi muốn bắt đầu hiệu ứng lấp lánh
        public void PlayEffect()
        {
            // Dọn dẹp state cũ trước khi play
            StopEffect();
            
            _fxSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);

            // 1. Logic cho Glow (Xoay + Pulsing)
            if (starGlow != null)
            {
                starGlow.gameObject.SetActive(true);
                starGlow.transform.localScale = Vector3.zero;
                
                // Scale up ban đầu
                starGlow.transform.DOScale(1.5f, 0.4f).SetEase(Ease.OutQuad).SetUpdate(true);
                
                // Xoay vô tận
                starGlow.transform.DORotate(new Vector3(0, 0, -360f), 4f, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true);

                // Mờ tỏ
                Sequence pulseSeq = DOTween.Sequence().SetUpdate(true).SetLink(starGlow.gameObject);
                pulseSeq.Append(starGlow.DOFade(1f, 0.5f).From(0.4f).SetEase(Ease.InOutQuad));
                pulseSeq.Append(starGlow.DOFade(0.4f, 0.8f).SetEase(Ease.InOutQuad));
                pulseSeq.SetLoops(-1, LoopType.Restart);
            }

            // 2. Logic cho các hạt Glint (Random vị trí)
            if (glintImages != null && glintImages.Count > 0)
            {
                foreach (var glint in glintImages)
                {
                    if (glint == null) continue;

                    float fadeTime = Random.Range(0.2f, 0.5f);
                    float intervalTime = Random.Range(0.5f, 1.5f);

                    Sequence glintSeq = DOTween.Sequence().SetUpdate(true).SetLink(glint.gameObject);

                    glintSeq.AppendCallback(() => 
                    {
                        glint.gameObject.SetActive(true);
                        glint.rectTransform.anchoredPosition = new Vector2(
                            Random.Range(-glintSpawnRadius, glintSpawnRadius),
                            Random.Range(-glintSpawnRadius, glintSpawnRadius)
                        );
                        glint.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 90f));
                    });
                    
                    glintSeq.Append(glint.DOFade(1f, fadeTime).SetEase(Ease.OutFlash));
                    glintSeq.Join(glint.transform.DOScale(1.2f, fadeTime).SetEase(Ease.OutFlash));
                    glintSeq.Append(glint.DOFade(0f, fadeTime).SetEase(Ease.InFlash));
                    glintSeq.Join(glint.transform.DOScale(0.5f, fadeTime).SetEase(Ease.InFlash));
                    glintSeq.AppendInterval(intervalTime);
                    glintSeq.SetLoops(-1, LoopType.Restart);
                }
            }
        }

        // Gọi hàm này khi ẩn UI, hoặc muốn tắt hiệu ứng
        public void StopEffect()
        {
            _fxSequence?.Kill();
            
            // Xóa mọi tween trên các object con
            if (starGlow != null) starGlow.transform.DOKill();
            if (glintImages != null)
            {
                foreach (var glint in glintImages)
                {
                    if (glint != null) glint.transform.DOKill();
                }
            }
            
            ResetVisuals();
        }

        private void ResetVisuals()
        {
            if (starGlow != null)
            {
                starGlow.gameObject.SetActive(false);
                starGlow.color = new Color(1, 1, 1, 0);
            }

            if (glintImages != null)
            {
                foreach (var glint in glintImages)
                {
                    if (glint != null)
                    {
                        glint.gameObject.SetActive(false);
                        glint.color = new Color(1, 1, 1, 0);
                    }
                }
            }
        }

        private void OnDisable()
        {
            StopEffect(); // Rất quan trọng để tránh lỗi khi object bị tắt đột ngột
        }
    }
}