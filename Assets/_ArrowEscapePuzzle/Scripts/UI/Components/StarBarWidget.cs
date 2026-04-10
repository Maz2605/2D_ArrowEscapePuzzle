using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.UI.Components
{
    public class StarBarWidget : MonoBehaviour
    {
        [Header("--- Nodes ---")]
        [SerializeField] private List<StarNode> starNodes = new List<StarNode>();

        public void ResetAllStars()
        {
            foreach (var node in starNodes)
            {
                if (node != null) node.ResetForAnimation();
            }
        }
        
        public void DisplayStars(int earnedStars)
        {
            if (starNodes == null) return;

            for (int i = 0; i < starNodes.Count; i++)
            {
                StarNode node = starNodes[i];
                if (node == null) continue;

                node.ResetForAnimation();

                if (i < earnedStars && node.FullStar != null)
                {
                    node.FullStar.gameObject.SetActive(true);
                    node.FullStar.transform.localScale = Vector3.one;
                }
            }
        }

        public void AppendAnimationToSequence(Sequence seq, int targetStars)
        {
            if (starNodes == null || starNodes.Count == 0) return;

            for (int i = 0; i < starNodes.Count; i++)
            {
                int index = i; 
                StarNode node = starNodes[index];
                
                if (index >= targetStars)
                {
                    seq.AppendInterval(0.3f);
                    continue;
                }

                if (node.FullStar == null || node.Container == null) continue;

                // 1. Hiệu ứng bật sao chính
                seq.AppendCallback(() => node.FullStar.gameObject.SetActive(true));
                seq.Append(node.FullStar.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
                seq.Join(node.Container.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f));

                // 2. Hiệu ứng Glow (Xoay và mờ/tỏ)
                if (node.StarGlow != null)
                {
                    seq.AppendCallback(() => 
                    {
                        node.StarGlow.gameObject.SetActive(true);
                        
                        // Scale to ra
                        node.StarGlow.transform.DOScale(1.5f, 0.4f).SetEase(Ease.OutQuad);
                        
                        // Xoay vô tận
                        node.StarGlow.transform.DORotate(new Vector3(0, 0, -360f), 4f, RotateMode.FastBeyond360)
                            .SetEase(Ease.Linear)
                            .SetLoops(-1, LoopType.Restart)
                            .SetUpdate(true)
                            .SetLink(node.StarGlow.gameObject);

                        Sequence pulseSeq = DOTween.Sequence()
                            .SetUpdate(true)
                            .SetLink(node.StarGlow.gameObject);

                        pulseSeq.Append(node.StarGlow.DOFade(1f, 0.5f).From(0.4f).SetEase(Ease.InOutQuad));
                        pulseSeq.Append(node.StarGlow.DOFade(0.4f, 0.8f).SetEase(Ease.InOutQuad));
                        pulseSeq.SetLoops(-1, LoopType.Restart);
                    });
                }

                if (node.GlintImages != null && node.GlintImages.Count > 0)
                {
                    seq.AppendCallback(() => 
                    {
                        foreach (var glint in node.GlintImages)
                        {
                            if (glint == null) continue;

                            float fadeTime = Random.Range(0.2f, 0.6f);
                            float intervalTime = Random.Range(0.5f, 1.5f);
                            
                            // Bán kính vùng xuất hiện của hạt lấp lánh (tính từ tâm ngôi sao)
                            // Bạn có thể tăng giảm số này trên Editor nếu muốn nó bay xa hơn
                            float posVariance = 60f; 

                            Sequence glintSeq = DOTween.Sequence()
                                .SetUpdate(true)
                                .SetLink(glint.gameObject);

                            // MỖI LẦN LẶP: Đổi vị trí ngẫu nhiên trước khi bật lên
                            glintSeq.AppendCallback(() => 
                            {
                                glint.gameObject.SetActive(true);
                                
                                // Reset vị trí về ngẫu nhiên xung quanh tâm (0,0) của ngôi sao
                                glint.rectTransform.anchoredPosition = new Vector2(
                                    Random.Range(-posVariance, posVariance),
                                    Random.Range(-posVariance, posVariance)
                                );
                                
                                // Thêm chút random góc xoay cho tự nhiên
                                glint.rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 90f));
                            });
                            
                            // Fade in + Nở to
                            glintSeq.Append(glint.DOFade(0.8f, fadeTime).SetEase(Ease.OutFlash));
                            glintSeq.Join(glint.transform.DOScale(1f, fadeTime).SetEase(Ease.OutFlash));
                            
                            // Fade out + Thu nhỏ
                            glintSeq.Append(glint.DOFade(0f, fadeTime).SetEase(Ease.InFlash));
                            glintSeq.Join(glint.transform.DOScale(0.4f, fadeTime).SetEase(Ease.InFlash));
                            
                            // Thời gian chờ ngẫu nhiên trước khi lấp lánh lại ở chỗ mới
                            glintSeq.AppendInterval(intervalTime);
                            
                            // Lặp lại vô tận
                            glintSeq.SetLoops(-1, LoopType.Restart);
                        }
                    });
                }

                seq.AppendInterval(0.1f); 
            }
        }
    }
}