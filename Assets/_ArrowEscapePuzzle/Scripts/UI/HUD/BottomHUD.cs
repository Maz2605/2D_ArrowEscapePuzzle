using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ArrowGame.UI.HUD
{
    // CỤC 1: CLASS CƠ BẢN DÀNH CHO CÁC BOOSTER THÔNG THƯỜNG (Dùng 1 lần)
    [Serializable]
    public class BoosterSlotUI
    {
        public BoosterType type;
        public Button button;
        public Image iconImage; // Hình này sẽ được tự động fill từ ConfigSO
        public TextMeshProUGUI txtCount;
    }

    // CỤC 2: CLASS CHUYÊN BIỆT CHO BOOSTER DẠNG TOGGLE (Bật/Tắt)
    [Serializable]
    public class ToggleBoosterSlotUI : BoosterSlotUI
    {
        [Header("--- Toggle Visuals ---")]
        public RectTransform fillRect;      
        public CanvasGroup fillCanvasGroup; 
        
        [HideInInspector] public Tween pulseTween;
    }

    public class BottomHUD : BaseHUD
    {
        [Header("--- Standard Boosters ---")]
        [SerializeField] private List<BoosterSlotUI> standardSlots = new List<BoosterSlotUI>();

        [Header("--- Toggle Booster (Special) ---")]
        [SerializeField] private ToggleBoosterSlotUI lineGuideSlot;

        [Header("--- Toggle Animation Settings ---")]
        [SerializeField] private float fillDuration = 0.35f;
        [SerializeField] private float pulseScale = 1.06f;
        [SerializeField] private float pulseSpeed = 0.8f;

        private void Awake()
        {
            // 1. Setup các Booster thường
            foreach (var slot in standardSlots)
            {
                if (slot?.button == null) continue;
                BindButton(slot.button, () => BoosterManager.Instance.RequestUseBooster(slot.type));
            }

            // 2. Setup cục Toggle riêng biệt
            if (lineGuideSlot?.button != null)
            {
                BindButton(lineGuideSlot.button, () => BoosterManager.Instance.RequestUseBooster(lineGuideSlot.type));
                
                if (lineGuideSlot.fillRect != null)
                {
                    lineGuideSlot.fillRect.localScale = Vector3.zero;
                    if (lineGuideSlot.fillCanvasGroup != null) lineGuideSlot.fillCanvasGroup.alpha = 0;
                }
            }
        }

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<BoosterType>(LogicGameEventID.BoosterChanged, OnBoosterCountChanged);
            EventManager<LogicGameEventID>.AddListener<bool>(LogicGameEventID.LineGuideToggle, OnLineGuideToggled);

            RefreshAllVisuals();
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<BoosterType>(LogicGameEventID.BoosterChanged, OnBoosterCountChanged);
            EventManager<LogicGameEventID>.RemoveListener<bool>(LogicGameEventID.LineGuideToggle, OnLineGuideToggled);
            
            KillToggleTween();
        }

        // --------------------------------------------------------
        // PHẦN LOGIC: CẬP NHẬT DATA & HÌNH ẢNH
        // --------------------------------------------------------

        private void RefreshAllVisuals()
        {
            // Update Standard Slots
            foreach (var slot in standardSlots) UpdateSlotData(slot);
            
            // Update Toggle Slot
            UpdateSlotData(lineGuideSlot);
        }

        private void OnBoosterCountChanged(BoosterType type)
        {
            if (lineGuideSlot.type == type)
            {
                UpdateSlotData(lineGuideSlot);
                return;
            }

            foreach (var slot in standardSlots)
            {
                if (slot.type == type)
                {
                    UpdateSlotData(slot);
                    break;
                }
            }
        }

        private void UpdateSlotData(BoosterSlotUI slot)
        {
            if (slot == null) return;

            var config = BoosterManager.Instance.GetBoosterConfig(slot.type);
            if (config == null) return;

            // 1. Fill hình ảnh trực tiếp từ ScriptableObject Config
            if (slot.iconImage != null && config.boosterIcon != null)
            {
                slot.iconImage.sprite = config.boosterIcon;
            }

            // 2. Cập nhật Text số lượng
            if (slot.txtCount != null)
            {
                if (!config.isConsumable)
                {
                    slot.txtCount.text = ""; // Nếu là Toggle không tốn lượt thì ẩn số
                }
                else
                {
                    int count = DataManager.Instance.GetBoosterCount(slot.type);
                    slot.txtCount.text = count > 0 ? count.ToString() : "+";
                }
            }
        }

        private void OnLineGuideToggled(bool isOn)
        {
            var slot = lineGuideSlot;
            if (slot == null || slot.fillRect == null) return;

            KillToggleTween();

            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(slot.fillRect.gameObject, LinkBehaviour.KillOnDisable);

            if (isOn)
            {
                if (slot.fillRect.localScale == Vector3.zero && slot.fillCanvasGroup != null) 
                    slot.fillCanvasGroup.alpha = 0f;

                seq.Append(slot.fillRect.DOScale(1f, fillDuration).SetEase(Ease.OutQuad));
                
                if (slot.fillCanvasGroup != null)
                    seq.Join(slot.fillCanvasGroup.DOFade(1f, fillDuration));

                seq.OnComplete(() => StartPulseAnimation(slot));
            }
            else
            {
                seq.Append(slot.fillRect.DOScale(0f, fillDuration).SetEase(Ease.InOutSine));
                
                if (slot.fillCanvasGroup != null)
                    seq.Join(slot.fillCanvasGroup.DOFade(0f, fillDuration));
            }
        }

        private void StartPulseAnimation(ToggleBoosterSlotUI slot)
        {
            if (slot.fillRect == null) return;

            slot.pulseTween = slot.fillRect.DOScale(pulseScale, pulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(slot.fillRect.gameObject, LinkBehaviour.KillOnDisable);
        }

        private void KillToggleTween()
        {
            if (lineGuideSlot != null)
            {
                lineGuideSlot.pulseTween?.Kill();
                if (lineGuideSlot.fillRect != null) lineGuideSlot.fillRect.DOKill();
                if (lineGuideSlot.fillCanvasGroup != null) lineGuideSlot.fillCanvasGroup.DOKill();
            }
        }
    }
}