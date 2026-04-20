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
    [Serializable]
    public class BoosterSlotUI
    {
        public BoosterType type;
        public Button button;
        public Image iconBg; 
        public TextMeshProUGUI txtCount;

        [Header("--- Mask Filling (For Toggle Type) ---")]
        public RectTransform fillRect;      
        public CanvasGroup fillCanvasGroup; 
        
        [HideInInspector] public Tween pulseTween;
    }

    public class BottomHUD : BaseHUD
    {
        [Header("--- Configuration ---")]
        [SerializeField] private List<BoosterSlotUI> boosterSlots = new List<BoosterSlotUI>();

        [Header("--- Animation Settings ---")]
        [SerializeField] private float fillDuration = 0.35f;
        [SerializeField] private float pulseScale = 1.06f;
        [SerializeField] private float pulseSpeed = 0.8f;

        private Dictionary<BoosterType, BoosterSlotUI> _slotCache;

        private void Awake()
        {
            _slotCache = new Dictionary<BoosterType, BoosterSlotUI>();
            foreach (var slot in boosterSlots)
            {
                if (slot?.button == null) continue;
                _slotCache[slot.type] = slot;

                BindButton(slot.button, () => BoosterManager.Instance.RequestUseBooster(slot.type));

                if (slot.fillRect != null)
                {
                    slot.fillRect.localScale = Vector3.zero;
                    if (slot.fillCanvasGroup != null) slot.fillCanvasGroup.alpha = 0;
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
            
            KillAllPulseTweens();
        }

        private void OnLineGuideToggled(bool isOn)
        {
            if (!_slotCache.TryGetValue(BoosterType.LineGuide, out var slot)) return;
            if (slot.fillRect == null) return;

            slot.fillRect.DOKill();
            slot.pulseTween?.Kill();
            if (slot.fillCanvasGroup != null) slot.fillCanvasGroup.DOKill();

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

        private void StartPulseAnimation(BoosterSlotUI slot)
        {
            if (slot.fillRect == null) return;

            slot.pulseTween = slot.fillRect.DOScale(pulseScale, pulseSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(slot.fillRect.gameObject, LinkBehaviour.KillOnDisable);
        }

        private void OnBoosterCountChanged(BoosterType type)
        {
            if (_slotCache.TryGetValue(type, out var slot)) UpdateSlotText(slot);
        }

        private void RefreshAllVisuals()
        {
            foreach (var slot in boosterSlots)
            {
                UpdateSlotText(slot);
            }
        }

        private void UpdateSlotText(BoosterSlotUI slot)
        {
            if (slot.txtCount == null) return;
            
            var config = BoosterManager.Instance.GetBoosterConfig(slot.type);

            if (config != null && !config.isConsumable)
            {
                slot.txtCount.text = ""; 
                return;
            }

            int count = DataManager.Instance.GetBoosterCount(slot.type);
            slot.txtCount.text = count > 0 ? count.ToString() : "+";
        }

        private void KillAllPulseTweens()
        {
            foreach (var slot in boosterSlots)
            {
                slot.pulseTween?.Kill();
                if (slot.fillRect != null) slot.fillRect.DOKill();
            }
        }
    }
}