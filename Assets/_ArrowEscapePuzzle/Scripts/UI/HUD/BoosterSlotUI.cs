using System;
using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ArrowGame.UI.HUD
{
    public class BoosterSlotUI : MonoBehaviour
    {
        public BoosterType type;
        public Button button;
        public Image iconImage; // Hình này sẽ được tự động fill từ ConfigSO
        public TextMeshProUGUI txtCount;
        public GameObject lockedOverlay;
        public TextMeshProUGUI txtUnlockLevel;
        public CanvasGroup canvasGroup;
        
        public virtual void UpdateSlot()
        {
            var config = BoosterManager.Instance.GetBoosterConfig(type);
            if (config == null) return;
            bool isUnlocked = BoosterManager.Instance.IsBoosterUnlocked(type);

            // Nếu đang chạy hoạt ảnh bay mở khóa, đè hiển thị trực quan thành khoá (Locked)
            if (isUnlocked && BottomHUD.IsAnimatingUnlock(type))
            {
                isUnlocked = false;
            }

            // 1. Cập nhật và tắt/bật Icon gốc
            if (iconImage != null)
            {
                if (config.boosterIcon != null)
                {
                    iconImage.sprite = config.boosterIcon;
                }
                iconImage.gameObject.SetActive(isUnlocked);
            }

            SetLockedVisual(config, isUnlocked);

            // 2. Cập nhật Text số lượng / Notify
            if (txtCount != null)
            {
                GameObject notifyGo = txtCount.gameObject;
                if (txtCount.transform.parent != null && txtCount.transform.parent.name == "Notify")
                {
                    notifyGo = txtCount.transform.parent.gameObject;
                }

                if (!isUnlocked)
                {
                    notifyGo.SetActive(false); // Ẩn hẳn notify badge khi khóa
                }
                else if (!config.isConsumable)
                {
                    notifyGo.SetActive(false); // Ẩn notify badge đối với toggle booster
                }
                else
                {
                    notifyGo.SetActive(true);
                    int count = DataManager.Instance.GetBoosterCount(type);
                    txtCount.text = count > 0 ? count.ToString() : "+";
                }
            }
        }

        private void SetLockedVisual(BoosterConfigSO config, bool isUnlocked)
        {
            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!isUnlocked);

                if (!isUnlocked)
                {
                    Graphic[] overlayGraphics = lockedOverlay.GetComponentsInChildren<Graphic>(true);
                    foreach (Graphic graphic in overlayGraphics)
                    {
                        graphic.raycastTarget = false;
                    }
                }
            }

            if (txtUnlockLevel != null)
            {
                txtUnlockLevel.gameObject.SetActive(!isUnlocked);
                txtUnlockLevel.text = $"LV {Mathf.Max(1, config.unlockLevel)}";
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (button != null)
            {
                // Keep locked buttons clickable so BoosterManager can show explicit unlock feedback.
                button.interactable = true;
            }
        }

        public void PlayUnlockBounce()
        {
            transform.DOKill();
            transform.localScale = Vector3.one;

            // Sử dụng Sequence để tạo hiệu ứng co giãn đàn hồi (squash & stretch) hoạt họa, nảy hơn rất nhiều
            Sequence bounceSeq = DOTween.Sequence();
            
            // 1. Phình to cực nhanh kèm overshoot nhẹ (OutBack) để tạo xung lực va chạm ban đầu cực mạnh
            bounceSeq.Append(transform.DOScale(1.5f, 0.15f).SetEase(Ease.OutBack));
            
            // 2. Co bóp mạnh xuống dưới kích thước chuẩn (squash) để tạo độ dẻo đàn hồi
            bounceSeq.Append(transform.DOScale(0.8f, 0.12f).SetEase(Ease.OutQuad));
            
            // 3. Nảy nhẹ trở lại trên mức trung bình để ổn định dao động
            bounceSeq.Append(transform.DOScale(1.15f, 0.1f).SetEase(Ease.InOutQuad));
            
            // 4. Trở về trạng thái chuẩn 1.0
            bounceSeq.Append(transform.DOScale(1.0f, 0.08f).SetEase(Ease.OutQuad));

            bounceSeq.SetUpdate(true)
                     .SetLink(gameObject);
        }
    }
}
