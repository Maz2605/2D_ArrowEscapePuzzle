using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Data.Events;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Manager;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ArrowGame.UI.HUD
{
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

        [Header("--- Booster Unlock Animation ---")]
        [SerializeField] private float flyDuration = 1.0f;    // Thời gian bay (giây)
        [SerializeField] private float bezierHeight = 200f;   // Chiều cao đỉnh cung Bezier (canvas units)
        [SerializeField] private float cloneStartScale = 1.2f;
        [SerializeField] private float cloneEndScale = 0.55f;

        private static readonly HashSet<BoosterType> _animatingUnlockBoosters = new HashSet<BoosterType>();

        public static void RegisterAnimatingUnlock(BoosterType type)
        {
            _animatingUnlockBoosters.Add(type);
        }

        public static void UnregisterAnimatingUnlock(BoosterType type)
        {
            _animatingUnlockBoosters.Remove(type);
        }

        public static bool IsAnimatingUnlock(BoosterType type)
        {
            return _animatingUnlockBoosters.Contains(type);
        }

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
            EventManager<VisualEventID>.AddListener<BoosterUnlockAnimPayload>(VisualEventID.PlayBoosterUnlockAnimation, OnPlayBoosterUnlockAnimation);

            if (BoosterManager.Instance != null)
            {
                BoosterManager.Instance.BoosterToggleChanged += OnBoosterToggleChanged;
            }

            RefreshAllVisuals();
            if (BoosterManager.Instance != null)
            {
                OnLineGuideToggled(BoosterManager.Instance.IsLineGuideActive());
            }
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<BoosterType>(LogicGameEventID.BoosterChanged, OnBoosterCountChanged);
            EventManager<VisualEventID>.RemoveListener<BoosterUnlockAnimPayload>(VisualEventID.PlayBoosterUnlockAnimation, OnPlayBoosterUnlockAnimation);

            if (BoosterManager.Instance != null)
            {
                BoosterManager.Instance.BoosterToggleChanged -= OnBoosterToggleChanged;
            }
            
            KillToggleTween();
            _animatingUnlockBoosters.Clear();
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
            bool isUnlocked = BoosterManager.Instance.IsBoosterUnlocked(slot.type);
            
            if (slot == lineGuideSlot)
            {
                slot.gameObject.SetActive(isUnlocked);
            }

            // Gọi hàm UpdateSlot của chính slot đó để cập nhật giao diện
            slot.UpdateSlot();
        }

        // --------------------------------------------------------
        // ICON BAY VỀ BOTTOM HUD - BEZIER CURVE ANIMATION
        // --------------------------------------------------------

        private void OnPlayBoosterUnlockAnimation(BoosterUnlockAnimPayload payload)
        {
            // Tìm slot đích tương ứng với boosterType
            BoosterSlotUI targetSlot = FindSlotByType(payload.BoosterType);
            if (targetSlot == null)
            {
                if (payload.ExistingClone != null) Destroy(payload.ExistingClone);
                payload.OnAnimComplete?.Invoke();
                return;
            }

            // Đặt clone trên TopRoot (trên cùng mọi UI)
            Transform flyRoot = UIManager.Instance != null ? UIManager.Instance.TopRoot : transform;
            RectTransform flyRootRect = flyRoot as RectTransform;

            // Lấy Canvas và camera của flyRoot để convert toạ độ
            Canvas topCanvas = flyRoot.GetComponentInParent<Canvas>();
            if (topCanvas == null) topCanvas = FindAnyObjectByType<Canvas>();

            // Camera: null nếu ScreenSpaceOverlay, worldCamera nếu Camera Space
            Camera uiCamera = (topCanvas != null && topCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? topCanvas.worldCamera
                : null;

            // ── Lấy hoặc tạo clone
            GameObject cloneGo;
            RectTransform cloneRect;

            if (payload.ExistingClone != null)
            {
                // Dùng clone đã spawn sẵn - kill bobbing tween trước khi fly
                cloneGo = payload.ExistingClone;
                cloneGo.transform.DOKill(); // Kill idle bobbing
                cloneRect = cloneGo.GetComponent<RectTransform>();
            }
            else
            {
                // Fallback: tạo clone mới nếu không có sẵn
                if (payload.Icon == null)
                {
                    payload.OnAnimComplete?.Invoke();
                    return;
                }

                cloneGo = new GameObject("BoosterUnlockIcon_Fly");
                cloneGo.transform.SetParent(flyRoot, false);
                Image cloneImg = cloneGo.AddComponent<Image>();
                cloneImg.sprite = payload.Icon;
                cloneImg.raycastTarget = false;

                cloneRect = cloneGo.GetComponent<RectTransform>();
                cloneRect.sizeDelta = new Vector2(80f, 80f);
                cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
                cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
                cloneRect.pivot = new Vector2(0.5f, 0.5f);

                Vector2 startLocalPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    flyRootRect, payload.StartScreenPos, uiCamera, out startLocalPos);
                cloneRect.anchoredPosition = startLocalPos;
                cloneRect.localScale = Vector3.one * cloneStartScale;
            }

            // ── Lấy vị trí xuất phát hiện tại của clone (đang đứng yên ở đó)
            Vector2 startPos = cloneRect.anchoredPosition;

            // ── Convert END position (vị trí slot icon → screen → local trong flyRoot)
            Transform iconTarget = targetSlot.iconImage != null
                ? targetSlot.iconImage.transform
                : targetSlot.transform;
            Vector2 targetScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, iconTarget.position);

            Vector2 endLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                flyRootRect, targetScreenPos, uiCamera, out endLocalPos);

            // ── Điểm điều khiển Bezier: giữa đường, lệch lên trên
            Vector2 midLocalPos = (startPos + endLocalPos) * 0.5f + new Vector2(0f, bezierHeight);

            // ── Scale xuất phát từ scale hiện tại của clone
            float currentScale = cloneRect.localScale.x;

            // Tính toán scale đích động dựa trên kích thước thực tế của slot icon để khớp hoàn hảo
            float endScale = cloneEndScale;
            if (targetSlot.iconImage != null && cloneRect.sizeDelta.x > 0f)
            {
                float targetSlotVisualWidth = targetSlot.iconImage.rectTransform.sizeDelta.x * targetSlot.iconImage.transform.localScale.x;
                endScale = targetSlotVisualWidth / cloneRect.sizeDelta.x;
            }

            // ── Animate theo Quadratic Bezier (Ease.Linear để rawT không bị modify)
            Tween flyTween = DOVirtual.Float(0f, 1f, flyDuration, (float rawT) =>
            {
                float t = EaseInOutCubic(rawT);

                // Quadratic Bezier: P(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
                float oneMinusT = 1f - t;
                Vector2 pos = (oneMinusT * oneMinusT) * startPos
                            + (2f * oneMinusT * t) * midLocalPos
                            + (t * t) * endLocalPos;
                cloneRect.anchoredPosition = pos;

                // Scale: từ currentScale → endScale (InQuad)
                float scaleT = rawT * rawT;
                cloneRect.localScale = Vector3.one * Mathf.Lerp(currentScale, endScale, scaleT);
            }).SetEase(Ease.Linear)
              .SetUpdate(true);

            flyTween.OnComplete(() =>
            {
                if (cloneGo != null) Destroy(cloneGo);

                // Gỡ trạng thái đang bay để slot hiển thị unlock thực tế
                UnregisterAnimatingUnlock(payload.BoosterType);

                // Cập nhật slot UI hiển thị trạng thái đã unlock
                UpdateSlotData(targetSlot);

                // Chơi hiệu ứng co giãn nảy mạnh khi nhận icon thành công
                targetSlot.PlayUnlockBounce();

                // Gọi callback để UIStateController tiếp tục flow
                payload.OnAnimComplete?.Invoke();
            });
        }

        /// <summary>Cubic ease in-out: chậm đầu, nhanh giữa, chậm cuối.</summary>
        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f
                ? 4f * t * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>
        /// Tìm BoosterSlotUI trong danh sách slot theo type.
        /// </summary>
        private BoosterSlotUI FindSlotByType(BoosterType type)
        {
            if (lineGuideSlot != null && lineGuideSlot.type == type)
                return lineGuideSlot;

            foreach (var slot in standardSlots)
            {
                if (slot != null && slot.type == type)
                    return slot;
            }
            return null;
        }

        // --------------------------------------------------------
        // TOGGLE LINE GUIDE
        // --------------------------------------------------------

        private void OnLineGuideToggled(bool isOn)
        {
            var slot = lineGuideSlot;
            if (slot == null || slot.fillRect == null) return;

            // Nếu LineGuide đang bị khóa, không chạy tween hoạt họa
            if (BoosterManager.Instance != null && !BoosterManager.Instance.IsBoosterUnlocked(slot.type))
            {
                KillToggleTween();
                return;
            }

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

        private void OnBoosterToggleChanged(BoosterType type, bool isOn)
        {
            if (lineGuideSlot == null || lineGuideSlot.type != type) return;
            OnLineGuideToggled(isOn);
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
