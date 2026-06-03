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
        [SerializeField] private float highlightRadiusMultiplier = 1f;

        [Header("--- Hand Sprite ---")]
        [SerializeField] private Sprite handSprite;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float tapCycleDuration = 1.2f;
        [SerializeField] private Vector2 handOffset = new Vector2(30f, -30f);

        [Header("--- Tooltip Animation Settings ---")]
        [SerializeField] private float tooltipStartScale = 0.9f;
        [SerializeField] private float tooltipTargetScale = 1.1f;
        [SerializeField] private float tooltipAnimDuration = 0.4f;
        [SerializeField] private float tooltipIdleScalePulse = 1.05f; // Tỉ lệ scale nhấp nhô thêm khi idle (ví dụ: 1.05 lần)
        [SerializeField] private float tooltipIdleSpeed = 1.2f;       // Thời gian một chu kỳ nhịp thở idle

        private Sequence _handSequence;
        private RectTransform _parentRect;
        private Vector2 _targetScreenPos;
        private Vector2 _dialogTargetPos;
        private float _targetRadius;
        private float _currentRadius;
        private DG.Tweening.Tween _radiusTween;
        private Material _dimMaterialInstance;
        private RectTransform _secondHandTransform;
        private Image _secondHandImage;

        private bool _hasSecondTarget;
        private Vector3 _secondTargetWorldPos;
        private Vector2 _secondTargetScreenPos;
        private float _secondTargetRadius;
        private float _currentSecondRadius;
        private DG.Tweening.Tween _secondRadiusTween;
        private Sequence _secondHandSequence;

        private void EnsureSecondHandCreated()
        {
            if (_secondHandTransform == null && handTransform != null)
            {
                var go = Instantiate(handTransform.gameObject, handTransform.parent);
                _secondHandTransform = go.GetComponent<RectTransform>();
                _secondHandImage = go.GetComponent<Image>();
                _secondHandTransform.gameObject.name = "HandPointer_Second";
                _secondHandTransform.SetAsLastSibling();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _parentRect = GetComponent<RectTransform>();
            if (dialogContainer != null)
            {
                _dialogTargetPos = dialogContainer.anchoredPosition;
            }

            // Đảm bảo dimImage luôn vẽ ở dưới cùng (phông nền)
            if (dimImage != null)
            {
                dimImage.transform.SetAsFirstSibling();
            }
            // Đảm bảo dialogContainer (tooltip) luôn vẽ trên cùng
            if (dialogContainer != null)
            {
                dialogContainer.transform.SetAsLastSibling();
            }
            // Đảm bảo hand pointer luôn vẽ trên cùng
            if (handTransform != null)
            {
                handTransform.transform.SetAsLastSibling();
            }
        }

        private Vector3 _targetWorldPos;
        private bool _hasTarget;

        private bool IsCameraTutorialStep()
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive)
                return false;

            var config = TutorialManager.Instance.CurrentTutorialConfig;
            var stepIndex = TutorialManager.Instance.CurrentStepIndex;
            if (config != null && stepIndex >= 0 && stepIndex < config.steps.Count)
            {
                var step = config.steps[stepIndex];
                return step.targetGridPos.x < 0 || step.targetGridPos.y < 0;
            }
            return false;
        }

        private bool ShouldBlockOutsideClick()
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive)
                return true;

            var config = TutorialManager.Instance.CurrentTutorialConfig;
            var stepIndex = TutorialManager.Instance.CurrentStepIndex;
            if (config != null && stepIndex >= 0 && stepIndex < config.steps.Count)
            {
                var step = config.steps[stepIndex];
                return step.blockOutsideClick;
            }
            return true;
        }

        private int GetCameraStepType()
        {
            if (TutorialManager.Instance == null) return -1;
            return TutorialManager.Instance.CurrentStepIndex;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
            StopHandAnimation();
            if (_radiusTween != null)
            {
                _radiusTween.Kill();
                _radiusTween = null;
            }
            if (_secondRadiusTween != null)
            {
                _secondRadiusTween.Kill();
                _secondRadiusTween = null;
            }
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
            if (!_hasTarget || IsCameraTutorialStep()) return;

            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            Vector2 screenPos = mainCam.WorldToScreenPoint(_targetWorldPos);
            _targetScreenPos = screenPos;

            if (dimImage != null && _dimMaterialInstance != null)
            {
                _dimMaterialInstance.SetVector("_Center", new Vector4(screenPos.x, screenPos.y, 0, 0));
                _dimMaterialInstance.SetFloat("_Radius", _currentRadius);

                if (_hasSecondTarget)
                {
                    Vector2 secondScreenPos = mainCam.WorldToScreenPoint(_secondTargetWorldPos);
                    _secondTargetScreenPos = secondScreenPos;
                    _dimMaterialInstance.SetVector("_Center2", new Vector4(secondScreenPos.x, secondScreenPos.y, 0, 0));
                    _dimMaterialInstance.SetFloat("_Radius2", _currentSecondRadius);
                }
                else
                {
                    _dimMaterialInstance.SetVector("_Center2", Vector4.zero);
                    _dimMaterialInstance.SetFloat("_Radius2", 0f);
                }
            }
        }

        public void ShowStep(Vector3 worldPosition, string tooltipText, bool showHandPointer, float highlightSize = 120f, bool hasSecondTarget = false, Vector3 secondWorldPosition = default, float secondHighlightSize = 120f)
        {
            _targetWorldPos = worldPosition;
            _hasTarget = true;
            _targetRadius = highlightSize * 0.5f * highlightRadiusMultiplier;

            _hasSecondTarget = hasSecondTarget;
            _secondTargetWorldPos = secondWorldPosition;
            _secondTargetRadius = secondHighlightSize * 0.5f * highlightRadiusMultiplier;

            if (_radiusTween != null)
            {
                _radiusTween.Kill();
            }
            if (_secondRadiusTween != null)
            {
                _secondRadiusTween.Kill();
            }

            float startRadius = Mathf.Max(_targetRadius * 5f, 350f);
            _currentRadius = startRadius;

            _radiusTween = DG.Tweening.DOTween.To(() => _currentRadius, x => _currentRadius = x, _targetRadius, 0.45f)
                .SetEase(DG.Tweening.Ease.OutCubic)
                .SetUpdate(true)
                .SetLink(gameObject);

            if (_hasSecondTarget)
            {
                float startSecondRadius = Mathf.Max(_secondTargetRadius * 5f, 350f);
                _currentSecondRadius = startSecondRadius;

                _secondRadiusTween = DG.Tweening.DOTween.To(() => _currentSecondRadius, x => _currentSecondRadius = x, _secondTargetRadius, 0.45f)
                    .SetEase(DG.Tweening.Ease.OutCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }

            // Cập nhật lời thoại và chạy hiệu ứng xuất hiện tooltip (scale nhẹ & alpha trực tiếp trên Text)
            if (txtTooltip != null)
            {
                txtTooltip.text = tooltipText;
                
                txtTooltip.DOKill();
                txtTooltip.transform.DOKill();

                Color c = txtTooltip.color;
                c.a = 0f;
                txtTooltip.color = c;
                txtTooltip.DOFade(1f, tooltipAnimDuration * 0.75f)
                    .SetUpdate(true)
                    .SetLink(txtTooltip.gameObject);

                txtTooltip.transform.localScale = new Vector3(tooltipStartScale, tooltipStartScale, 1f);
                txtTooltip.transform.DOScale(tooltipTargetScale, tooltipAnimDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .SetLink(txtTooltip.gameObject)
                    .OnComplete(() =>
                    {
                        // Hiệu ứng Idle thở nhẹ (breathing) lặp vô hạn sau khi hiện xong
                        txtTooltip.transform.DOScale(tooltipTargetScale * tooltipIdleScalePulse, tooltipIdleSpeed * 0.5f)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetUpdate(true)
                            .SetLink(txtTooltip.gameObject);
                    });
            }

            Camera mainCam = Camera.main;
            Vector2 screenPos = mainCam != null ? (Vector2)mainCam.WorldToScreenPoint(worldPosition) : Vector2.zero;
            _targetScreenPos = screenPos;

            Vector2 secondScreenPos = Vector2.zero;
            if (_hasSecondTarget && mainCam != null)
            {
                secondScreenPos = mainCam.WorldToScreenPoint(_secondTargetWorldPos);
                _secondTargetScreenPos = secondScreenPos;
            }

            bool isCamStep = IsCameraTutorialStep();

            if (dialogContainer != null)
            {
                dialogContainer.DOKill();
                dialogContainer.localScale = Vector3.one;
                
                // Trả về đúng vị trí thiết kế ban đầu trong Inspector/Prefab
                dialogContainer.anchoredPosition = _dialogTargetPos;

                // Nếu có Background Image đang hoạt động, cũng chạy hiệu ứng fade cho nó
                var bgImage = dialogContainer.GetComponent<Image>();
                if (bgImage != null && bgImage.enabled)
                {
                    bgImage.DOKill();
                    Color bgC = bgImage.color;
                    bgC.a = 0f;
                    bgImage.color = bgC;
                    bgImage.DOFade(0.8f, tooltipAnimDuration * 0.75f) // Mặc định alpha là 0.8 như cấu hình gốc
                        .SetUpdate(true)
                        .SetLink(dialogContainer.gameObject);
                }
            }

            // Thiết lập vật liệu đục lỗ vòng tròn
            if (dimImage != null)
            {
                if (isCamStep)
                {
                    dimImage.gameObject.SetActive(false);
                }
                else
                {
                    dimImage.gameObject.SetActive(true);
                    if (_dimMaterialInstance == null && dimImage.material != null)
                    {
                        _dimMaterialInstance = Instantiate(dimImage.material);
                        dimImage.material = _dimMaterialInstance;
                    }

                    if (_dimMaterialInstance != null)
                    {
                        _dimMaterialInstance.SetVector("_Center", new Vector4(screenPos.x, screenPos.y, 0, 0));
                        _dimMaterialInstance.SetFloat("_Radius", _currentRadius);

                        if (_hasSecondTarget)
                        {
                            _dimMaterialInstance.SetVector("_Center2", new Vector4(secondScreenPos.x, secondScreenPos.y, 0, 0));
                            _dimMaterialInstance.SetFloat("_Radius2", _currentSecondRadius);
                        }
                        else
                        {
                            _dimMaterialInstance.SetVector("_Center2", Vector4.zero);
                            _dimMaterialInstance.SetFloat("_Radius2", 0f);
                        }
                    }
                }
            }

            // Cấu hình bàn tay chỉ dẫn
            if (showHandPointer && handTransform != null && handImage != null)
            {
                handTransform.gameObject.SetActive(true);
                if (isCamStep)
                {
                    StartCameraHandAnimation(GetCameraStepType());
                }
                else
                {
                    StartHandAnimation(screenPos);

                    if (_hasSecondTarget)
                    {
                        EnsureSecondHandCreated();
                        if (_secondHandTransform != null)
                        {
                            _secondHandTransform.gameObject.SetActive(true);
                            StartSecondHandAnimation(secondScreenPos);
                        }
                    }
                    else
                    {
                        if (_secondHandTransform != null)
                        {
                            _secondHandTransform.gameObject.SetActive(false);
                        }
                    }
                }
            }
            else
            {
                if (handTransform != null)
                {
                    handTransform.gameObject.SetActive(false);
                }
                if (_secondHandTransform != null)
                {
                    _secondHandTransform.gameObject.SetActive(false);
                }
                StopHandAnimation();
            }
        }

        private void StartHandAnimation(Vector2 targetScreenPosition)
        {
            StopHandAnimation();

            if (handTransform == null || handImage == null) return;

            handTransform.gameObject.SetActive(true);
            handImage.color = Color.white;
            handImage.raycastTarget = false; // Đảm bảo người chơi không bấm nhầm vào hình bàn tay

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

        private void StartCameraHandAnimation(int stepType)
        {
            StopHandAnimation();

            if (handTransform == null || handImage == null) return;

            handTransform.gameObject.SetActive(true);
            handImage.color = Color.white;
            if (_secondHandImage != null)
            {
                _secondHandImage.color = Color.white;
            }

            handTransform.localScale = Vector3.one;
            handTransform.localRotation = Quaternion.identity;

            if (handSprite != null)
            {
                handImage.sprite = handSprite;
            }

            _handSequence = DOTween.Sequence()
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);

            if (stepType == 0) // Zoom (Pinch/Scroll)
            {
                EnsureSecondHandCreated();

                handTransform.gameObject.SetActive(true);
                if (_secondHandTransform != null)
                {
                    _secondHandTransform.gameObject.SetActive(true);
                    // Lật ngược theo trục X thay vì xoay 180 độ
                    _secondHandTransform.localScale = new Vector3(-1f, 1f, 1f);
                    _secondHandTransform.localRotation = Quaternion.identity;
                    if (handSprite != null && _secondHandImage != null)
                    {
                        _secondHandImage.sprite = handSprite;
                    }
                }

                // Hand 1: bắt đầu ở góc trên phải, ra xa hơn nữa
                Vector2 hand1Start = new Vector2(50f, 50f);
                Vector2 hand1End = new Vector2(220f, 220f);

                // Hand 2: bắt đầu ở góc dưới trái (đối xứng), ra xa hơn nữa
                Vector2 hand2Start = new Vector2(-50f, -50f);
                Vector2 hand2End = new Vector2(-220f, -220f);

                handTransform.anchoredPosition = hand1Start;
                // Xoay ngón tay trên 90° để đối xứng với ngón tay dưới
                handTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                if (_secondHandTransform != null)
                {
                    _secondHandTransform.anchoredPosition = hand2Start;
                }

                // Reset màu / alpha về 1 mỗi chu kỳ
                _handSequence.AppendCallback(() =>
                {
                    if (handImage != null) handImage.color = Color.white;
                    if (_secondHandImage != null) _secondHandImage.color = Color.white;
                    
                    handTransform.anchoredPosition = hand1Start;
                    handTransform.localScale = Vector3.one;
                    handTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);

                    if (_secondHandTransform != null)
                    {
                        _secondHandTransform.anchoredPosition = hand2Start;
                        _secondHandTransform.localScale = new Vector3(-1f, 1f, 1f);
                    }
                });

                // 1. Chạm xuống (Co nhỏ scale)
                _handSequence.Append(handTransform.DOScale(0.75f, 0.25f).SetEase(Ease.InQuad));
                if (_secondHandTransform != null)
                {
                    _handSequence.Join(_secondHandTransform.DOScale(new Vector3(-0.75f, 0.75f, 1f), 0.25f).SetEase(Ease.InQuad));
                }

                // 2. Vuốt đối xứng ra hai phía (Pinch out)
                _handSequence.Append(handTransform.DOAnchorPos(hand1End, 0.8f).SetEase(Ease.OutQuad));
                if (_secondHandTransform != null)
                {
                    _handSequence.Join(_secondHandTransform.DOAnchorPos(hand2End, 0.8f).SetEase(Ease.OutQuad));
                }

                // 3. Nhấc tay và nhạt dần
                _handSequence.Append(handTransform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad));
                if (handImage != null)
                {
                    _handSequence.Join(handImage.DOFade(0f, 0.2f));
                }

                if (_secondHandTransform != null)
                {
                    _handSequence.Join(_secondHandTransform.DOScale(new Vector3(-1f, 1f, 1f), 0.2f).SetEase(Ease.OutQuad));
                    if (_secondHandImage != null)
                    {
                        _handSequence.Join(_secondHandImage.DOFade(0f, 0.2f));
                    }
                }

                _handSequence.AppendInterval(0.3f);
            }
            else if (stepType == 1) // Pan (Kéo thả)
            {
                Vector2 startPos = new Vector2(-150f, 0f);
                Vector2 endPos = new Vector2(150f, 0f);
                
                handTransform.anchoredPosition = startPos;

                _handSequence.AppendCallback(() =>
                {
                    if (handImage != null) handImage.color = Color.white;
                    handTransform.anchoredPosition = startPos;
                    handTransform.localScale = Vector3.one;
                });

                _handSequence.Append(handTransform.DOScale(0.75f, 0.2f).SetEase(Ease.InQuad));
                _handSequence.Append(handTransform.DOAnchorPos(endPos, 0.9f).SetEase(Ease.InOutQuad));
                _handSequence.Append(handTransform.DOScale(1f, 0.15f).SetEase(Ease.OutQuad));
                if (handImage != null)
                {
                    _handSequence.Join(handImage.DOFade(0f, 0.15f));
                }
                _handSequence.AppendInterval(0.3f);
            }
            else if (stepType == 2) // Reset Zoom (Chạm đúp)
            {
                Vector2 center = Vector2.zero;
                handTransform.anchoredPosition = center;

                _handSequence.AppendCallback(() =>
                {
                    if (handImage != null) handImage.color = Color.white;
                    handTransform.anchoredPosition = center;
                    handTransform.localScale = Vector3.one;
                });

                // Chạm lần 1
                _handSequence.Append(handTransform.DOScale(0.75f, 0.12f).SetEase(Ease.InQuad));
                _handSequence.Append(handTransform.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
                
                // Khoảng cách ngắn giữa 2 chạm
                _handSequence.AppendInterval(0.08f);

                // Chạm lần 2
                _handSequence.Append(handTransform.DOScale(0.75f, 0.12f).SetEase(Ease.InQuad));
                _handSequence.Append(handTransform.DOScale(1f, 0.12f).SetEase(Ease.OutQuad));
                
                _handSequence.AppendInterval(0.8f);
            }
        }

        private void StopHandAnimation()
        {
            if (_handSequence != null)
            {
                _handSequence.Kill();
                _handSequence = null;
            }

            if (_secondHandSequence != null)
            {
                _secondHandSequence.Kill();
                _secondHandSequence = null;
            }

            if (handTransform != null)
            {
                handTransform.gameObject.SetActive(false);
            }

            if (_secondHandTransform != null)
            {
                _secondHandTransform.gameObject.SetActive(false);
            }
        }

        private void StartSecondHandAnimation(Vector2 targetScreenPosition)
        {
            if (_secondHandSequence != null)
            {
                _secondHandSequence.Kill();
                _secondHandSequence = null;
            }

            if (_secondHandTransform == null || _secondHandImage == null) return;

            _secondHandTransform.gameObject.SetActive(true);
            _secondHandImage.color = Color.white;
            _secondHandImage.raycastTarget = false; // Đảm bảo người chơi không bấm nhầm vào hình bàn tay

            // Chuyển vị trí mục tiêu sang local coordinate của parent RectTransform dùng đúng UI Camera
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, targetScreenPosition, GetUICamera(), out localPos);

            // Đặt bàn tay vào vị trí mục tiêu + Offset
            Vector2 startPos = localPos + handOffset;
            _secondHandTransform.anchoredPosition = startPos;
            _secondHandTransform.localScale = Vector3.one;
            _secondHandTransform.localRotation = Quaternion.identity;

            if (handSprite != null)
            {
                _secondHandImage.sprite = handSprite;
            }

            // Tạo Sequence DOTween chạy lặp vô hạn giả lập động tác gõ (Tap) bằng 1 Sprite duy nhất
            _secondHandSequence = DOTween.Sequence()
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true)
                .SetLink(gameObject);

            // 1. Hover/Chỉ vào mục tiêu (Rơ tay đến gần hơn)
            _secondHandSequence.Append(_secondHandTransform.DOAnchorPos(localPos + handOffset * 0.8f, tapCycleDuration * 0.35f).SetEase(Ease.OutQuad));
            _secondHandSequence.Join(_secondHandTransform.DORotate(Vector3.zero, tapCycleDuration * 0.35f).SetEase(Ease.OutQuad));

            // 2. Nhấp xuống (Di chuyển sát hơn vào mục tiêu, co nhỏ scale, xoay nhẹ mô phỏng nhấn)
            _secondHandSequence.Append(_secondHandTransform.DOAnchorPos(localPos + handOffset * 0.4f, tapCycleDuration * 0.15f).SetEase(Ease.InQuad));
            _secondHandSequence.Join(_secondHandTransform.DOScale(0.75f, tapCycleDuration * 0.15f).SetEase(Ease.InQuad));
            _secondHandSequence.Join(_secondHandTransform.DORotate(new Vector3(0f, 0f, -10f), tapCycleDuration * 0.15f).SetEase(Ease.InQuad));
            _secondHandSequence.AppendInterval(tapCycleDuration * 0.1f);

            // 3. Nhả lên (Trả về vị trí bắt đầu, trả scale và góc xoay)
            _secondHandSequence.Append(_secondHandTransform.DOAnchorPos(startPos, tapCycleDuration * 0.25f).SetEase(Ease.OutQuad));
            _secondHandSequence.Join(_secondHandTransform.DOScale(1f, tapCycleDuration * 0.25f).SetEase(Ease.OutQuad));
            _secondHandSequence.Join(_secondHandTransform.DORotate(Vector3.zero, tapCycleDuration * 0.25f).SetEase(Ease.OutQuad));
            _secondHandSequence.AppendInterval(tapCycleDuration * 0.15f);
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
                dialogContainer.anchoredPosition = _dialogTargetPos;
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

            if (_secondHandTransform != null && _secondHandImage != null)
            {
                _secondHandImage.DOKill();
                _secondHandImage.color = Color.red;
                _secondHandImage.DOColor(Color.white, 0.4f).SetUpdate(true).SetLink(_secondHandImage.gameObject);
            }
        }

        // --- ICanvasRaycastFilter ---
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive)
            {
                return true;
            }

            if (IsCameraTutorialStep())
            {
                return false; // Trả về false để click/drag/zoom xuyên qua toàn bộ overlay
            }

            // Nếu click nằm bên trong vùng đục lỗ tròn, cho phép click xuyên qua xuống Grid dưới game
            float dist = Vector2.Distance(sp, _targetScreenPos);
            if (dist < _targetRadius)
            {
                return false; // Trả về false để click đi xuyên qua
            }

            if (_hasSecondTarget)
            {
                float dist2 = Vector2.Distance(sp, _secondTargetScreenPos);
                if (dist2 < _secondTargetRadius)
                {
                    return false; // Trả về false để click đi xuyên qua
                }
            }

            return true; // Click bị chặn
        }

        // --- IPointerClickHandler ---
        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsCameraTutorialStep()) return; // Không báo lỗi khi chạm trong chế độ camera tutorial

            // Được gọi khi người chơi chạm vào phần dimmer tối (ở ngoài vùng đục lỗ)
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                if (!ShouldBlockOutsideClick())
                {
                    TutorialManager.Instance.AdvanceToNextStep();
                }
                else
                {
                    TutorialManager.Instance.PlayErrorFeedback();
                }
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
