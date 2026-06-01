using System;
using ArrowGame.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using ArrowGame.Gameplay.Managers;

namespace ArrowGame.UI.Popups
{
    public class TutorialOverlayUI : BasePopup, ICanvasRaycastFilter, IPointerClickHandler
    {
        [Header("--- UI References ---")]
        [SerializeField] private RectTransform panelContainer;
        [SerializeField] private RectTransform handTransform;
        [SerializeField] private Image handImage;
        [SerializeField] private TextMeshProUGUI txtTooltip;
        [SerializeField] private RectTransform dialogContainer;

        [Header("--- Circular Dimmer Image ---")]
        [SerializeField] private Image dimImage;

        [Header("--- Hand Sprite ---")]
        [SerializeField] private Sprite handSprite;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float tapCycleDuration = 1.2f;
        [SerializeField] private Vector2 handOffset = new Vector2(30f, -30f);

        private Sequence _handSequence;
        private RectTransform _parentRect;
        private Vector2 _targetScreenPos;
        private float _targetRadius;
        private Material _dimMaterialInstance;

        protected override void Awake()
        {
            base.Awake();
            _parentRect = GetComponent<RectTransform>();
        }

        private Vector3 _targetWorldPos;
        private bool _hasTarget;

        private void OnDisable()
        {
            StopHandAnimation();
            _hasTarget = false;
        }

        private void OnDestroy()
        {
            if (_dimMaterialInstance != null)
            {
                Destroy(_dimMaterialInstance);
            }
        }

        private void LateUpdate()
        {
            if (!_hasTarget) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector2 screenPos = mainCam.WorldToScreenPoint(_targetWorldPos);
            _targetScreenPos = screenPos;

            if (dimImage != null && _dimMaterialInstance != null)
            {
                _dimMaterialInstance.SetVector("_Center", new Vector4(screenPos.x, screenPos.y, 0, 0));
                _dimMaterialInstance.SetFloat("_Radius", _targetRadius);
            }
        }

        public void ShowStep(Vector3 worldPosition, string tooltipText, bool showHandPointer, float highlightSize = 120f)
        {
            _targetWorldPos = worldPosition;
            _hasTarget = true;
            _targetRadius = highlightSize * 0.5f;

            // Cập nhật lời thoại
            if (txtTooltip != null)
            {
                txtTooltip.text = tooltipText;
            }

            Camera mainCam = Camera.main;
            Vector2 screenPos = mainCam != null ? (Vector2)mainCam.WorldToScreenPoint(worldPosition) : Vector2.zero;
            _targetScreenPos = screenPos;

            // Thiết lập vật liệu đục lỗ vòng tròn
            if (dimImage != null)
            {
                if (_dimMaterialInstance == null && dimImage.material != null)
                {
                    _dimMaterialInstance = Instantiate(dimImage.material);
                    dimImage.material = _dimMaterialInstance;
                }

                if (_dimMaterialInstance != null)
                {
                    _dimMaterialInstance.SetVector("_Center", new Vector4(screenPos.x, screenPos.y, 0, 0));
                    _dimMaterialInstance.SetFloat("_Radius", _targetRadius);
                }
            }

            // Cấu hình bàn tay chỉ dẫn
            if (showHandPointer && handTransform != null && handImage != null)
            {
                handTransform.gameObject.SetActive(true);
                StartHandAnimation(screenPos);
            }
            else
            {
                if (handTransform != null)
                {
                    handTransform.gameObject.SetActive(false);
                }
                StopHandAnimation();
            }
        }

        private void StartHandAnimation(Vector2 targetScreenPosition)
        {
            StopHandAnimation();

            if (handTransform == null || handImage == null) return;

            // Chuyển vị trí mục tiêu sang local coordinate của parent RectTransform dùng đúng UI Camera
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, targetScreenPosition, GetUICamera(), out localPos);

            // Đặt bàn tay vào vị trí mục tiêu + Offset
            Vector2 startPos = localPos + handOffset;
            handTransform.anchoredPosition = startPos;
            handTransform.localScale = Vector3.one;
            handTransform.localRotation = Quaternion.identity;

            if (handSprite != null)
            {
                handImage.sprite = handSprite;
            }

            // Tạo Sequence DOTween chạy lặp vô hạn giả lập động tác gõ (Tap) bằng 1 Sprite duy nhất
            _handSequence = DOTween.Sequence()
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);

            // 1. Hover/Chỉ vào mục tiêu (Rơ tay đến gần hơn)
            _handSequence.Append(handTransform.DOAnchorPos(localPos + handOffset * 0.8f, tapCycleDuration * 0.35f).SetEase(Ease.OutQuad));
            _handSequence.Join(handTransform.DORotate(Vector3.zero, tapCycleDuration * 0.35f).SetEase(Ease.OutQuad));

            // 2. Nhấp xuống (Di chuyển sát hơn vào mục tiêu, co nhỏ scale, xoay nhẹ mô phỏng nhấn)
            _handSequence.Append(handTransform.DOAnchorPos(localPos + handOffset * 0.4f, tapCycleDuration * 0.15f).SetEase(Ease.InQuad));
            _handSequence.Join(handTransform.DOScale(0.75f, tapCycleDuration * 0.15f).SetEase(Ease.InQuad));
            _handSequence.Join(handTransform.DORotate(new Vector3(0f, 0f, -10f), tapCycleDuration * 0.15f).SetEase(Ease.InQuad));
            _handSequence.AppendInterval(tapCycleDuration * 0.1f);

            // 3. Nhả lên (Trả về vị trí bắt đầu, trả scale và góc xoay)
            _handSequence.Append(handTransform.DOAnchorPos(startPos, tapCycleDuration * 0.25f).SetEase(Ease.OutQuad));
            _handSequence.Join(handTransform.DOScale(1f, tapCycleDuration * 0.25f).SetEase(Ease.OutQuad));
            _handSequence.Join(handTransform.DORotate(Vector3.zero, tapCycleDuration * 0.25f).SetEase(Ease.OutQuad));
            _handSequence.AppendInterval(tapCycleDuration * 0.15f);
        }

        private void StopHandAnimation()
        {
            if (_handSequence != null)
            {
                _handSequence.Kill();
                _handSequence = null;
            }
        }

        private Camera GetUICamera()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }
            return null;
        }

        public void ShowErrorFeedback()
        {
            if (dialogContainer != null)
            {
                // Rung nhẹ hộp thoại báo hiệu lỗi click sai
                dialogContainer.DOKill();
                dialogContainer.anchoredPosition = Vector2.zero;
                dialogContainer.DOShakeAnchorPos(0.4f, new Vector2(15f, 0f), 15, 90f, false, true)
                    .SetUpdate(true)
                    .SetLink(dialogContainer.gameObject);
            }

            if (handTransform != null)
            {
                // Rung nhẹ bàn tay báo lỗi màu đỏ nếu được gán màu
                handImage.DOKill();
                handImage.color = Color.red;
                handImage.DOColor(Color.white, 0.4f).SetUpdate(true).SetLink(handImage.gameObject);
            }
        }

        // --- ICanvasRaycastFilter ---
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive)
            {
                return true;
            }

            // Nếu click nằm bên trong vùng đục lỗ tròn, cho phép click xuyên qua xuống Grid dưới game
            float dist = Vector2.Distance(sp, _targetScreenPos);
            if (dist < _targetRadius)
            {
                return false; // Trả về false để click đi xuyên qua
            }

            return true; // Click bị chặn
        }

        // --- IPointerClickHandler ---
        public void OnPointerClick(PointerEventData eventData)
        {
            // Được gọi khi người chơi chạm vào phần dimmer tối (ở ngoài vùng đục lỗ)
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                TutorialManager.Instance.PlayErrorFeedback();
            }
        }

        protected override void PlayShowAnimation()
        {
            if (panelContainer != null)
            {
                panelContainer.DOKill();
                panelContainer.localScale = Vector3.one * 0.9f;
                panelContainer.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            StopHandAnimation();

            if (panelContainer != null)
            {
                panelContainer.DOKill();
                panelContainer.DOScale(Vector3.one * 0.9f, animDuration).SetEase(Ease.InBack).SetUpdate(true).SetLink(gameObject)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}
