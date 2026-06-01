using System;
using ArrowGame.Data.Booster;
using ArrowGame.UI.Base;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Popups
{
    public class BoosterInstructionPopup : BasePopup
    {
        [SerializeField] private Button btnBackground;
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private TextMeshProUGUI txtInstruction;
        [SerializeField] private Image imgIcon;
        [SerializeField] private RectTransform contentPanel;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float titleOffset = 30f;
        [SerializeField] private float descriptionOffset = 18f;
        [SerializeField] private float instructionOffset = 24f;
        [SerializeField] private float iconPopScale = 1.12f;
        [SerializeField] private float iconIdleScale = 1.04f;
        [SerializeField] private float iconIdleMoveY = 6f;
        [SerializeField] private float iconIdleDuration = 1.4f;
        [SerializeField] private float tapPulseScale = 1.05f;
        [SerializeField] private float tapPulseAlpha = 0.45f;
        [SerializeField] private float tapPulseDuration = 0.8f;

        private Action _onClose;
        private Vector3 _titleOriginalPosition;
        private Vector3 _descriptionOriginalPosition;
        private Vector3 _instructionOriginalPosition;
        private Vector3 _iconOriginalScale;
        private Vector3 _iconOriginalPosition;
        private Vector3 _contentOriginalScale;
        private Tween _iconIdleScaleTween;
        private Tween _iconIdleMoveTween;
        private Tween _tapFadeTween;
        private Tween _tapScaleTween;

        // Vị trí screen của icon được capture ngay tại thời điểm user tap, trước khi popup đóng
        private Vector2 _capturedIconScreenPos;

        protected override void Awake()
        {
            base.Awake();
            CacheOriginalState();

            if (btnBackground != null)
            {
                btnBackground.onClick.RemoveAllListeners();
                btnBackground.onClick.AddListener(HandleCloseClicked);
            }
        }

        public void Setup(BoosterConfigSO boosterConfig, bool showConfirmButton, Action onConfirm, Action onCancel)
        {
            _onClose = showConfirmButton ? onConfirm : onCancel;

            ApplyContent(
                title: boosterConfig != null && !string.IsNullOrWhiteSpace(boosterConfig.boosterName)
                    ? boosterConfig.boosterName
                    : "Booster Name",
                description: boosterConfig != null ? boosterConfig.description : string.Empty,
                icon: boosterConfig != null ? boosterConfig.boosterIcon : null,
                instruction: showConfirmButton ? "Tap to use booster" : "Tap to continue"
            );
        }

        public void SetupIntroduction(BoosterConfigSO boosterConfig, Action onConfirm)
        {
            _onClose = onConfirm;

            ApplyContent(
                title: boosterConfig != null ? boosterConfig.GetUnlockTitle() : "Booster Name",
                description: boosterConfig != null ? boosterConfig.GetUnlockDescription() : "New booster unlocked",
                icon: boosterConfig != null ? boosterConfig.boosterIcon : null,
                instruction: "Tap to continue"
            );
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            KillAllTweens();

            if (contentPanel != null)
            {
                contentPanel.localScale = _contentOriginalScale;
            }

            if (txtTitle != null)
            {
                txtTitle.alpha = 0f;
                txtTitle.transform.localScale = Vector3.one * 0.94f;
                txtTitle.transform.localPosition = _titleOriginalPosition + new Vector3(0f, titleOffset, 0f);
            }

            if (txtDescription != null)
            {
                txtDescription.alpha = 0f;
                txtDescription.transform.localScale = Vector3.one * 0.96f;
                txtDescription.transform.localPosition = _descriptionOriginalPosition + new Vector3(0f, descriptionOffset, 0f);
            }

            if (imgIcon != null)
            {
                imgIcon.transform.localPosition = _iconOriginalPosition;
                imgIcon.transform.localScale = Vector3.zero;
                Color color = imgIcon.color;
                color.a = 0f;
                imgIcon.color = color;
            }

            if (txtInstruction != null)
            {
                txtInstruction.alpha = 0f;
                txtInstruction.transform.localScale = Vector3.one;
                txtInstruction.transform.localPosition = _instructionOriginalPosition + new Vector3(0f, -instructionOffset, 0f);
            }
        }

        protected override void PlayShowAnimation()
        {
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);

            if (canvasGroup != null)
            {
                sequence.Append(canvasGroup.DOFade(1f, animDuration * 0.6f));
            }

            if (txtTitle != null)
            {
                sequence.Insert(0f, txtTitle.DOFade(1f, 0.22f));
                sequence.Insert(0f, txtTitle.transform.DOLocalMove(_titleOriginalPosition, 0.3f).SetEase(Ease.OutCubic));
                sequence.Insert(0f, txtTitle.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            }

            if (txtDescription != null)
            {
                sequence.Insert(0.08f, txtDescription.DOFade(1f, 0.2f));
                sequence.Insert(0.08f, txtDescription.transform.DOLocalMove(_descriptionOriginalPosition, 0.28f).SetEase(Ease.OutCubic));
                sequence.Insert(0.08f, txtDescription.transform.DOScale(1f, 0.28f).SetEase(Ease.OutQuad));
            }

            if (imgIcon != null)
            {
                sequence.Insert(0.16f, imgIcon.DOFade(1f, 0.18f));
                sequence.Insert(0.16f, imgIcon.transform.DOScale(_iconOriginalScale * iconPopScale, 0.34f).SetEase(Ease.OutBack));
                sequence.Insert(0.42f, imgIcon.transform.DOScale(_iconOriginalScale, 0.16f).SetEase(Ease.OutQuad));
            }

            if (txtInstruction != null)
            {
                sequence.Insert(0.24f, txtInstruction.DOFade(1f, 0.2f));
                sequence.Insert(0.24f, txtInstruction.transform.DOLocalMove(_instructionOriginalPosition, 0.28f).SetEase(Ease.OutCubic));
            }

            sequence.OnComplete(() =>
            {
                StartIconIdle();
                StartTapPulse();
            });
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillAllTweens();

            Sequence sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject);

            if (contentPanel != null)
            {
                sequence.Join(contentPanel.DOScale(_contentOriginalScale * 0.96f, animDuration).SetEase(Ease.InQuad));
            }

            if (canvasGroup != null)
            {
                sequence.Join(canvasGroup.DOFade(0f, animDuration));
            }

            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void ApplyContent(string title, string description, Sprite icon, string instruction)
        {
            if (txtTitle != null)
            {
                txtTitle.text = string.IsNullOrWhiteSpace(title) ? "Booster Name" : title;
            }

            if (txtDescription != null)
            {
                txtDescription.text = string.IsNullOrWhiteSpace(description) ? "New booster unlocked" : description;
            }

            if (txtInstruction != null)
            {
                txtInstruction.text = instruction;
            }

            if (imgIcon != null)
            {
                imgIcon.sprite = icon;
                imgIcon.gameObject.SetActive(icon != null);
                Color color = imgIcon.color;
                color.a = 1f;
                imgIcon.color = color;
            }
        }

        private void HandleCloseClicked()
        {
            // Capture vị trí screen của icon TRƯỚC KHI popup bắt đầu đóng/fade
            // Lúc này icon vẫn đang ở đúng vị trí hiển thị
            _capturedIconScreenPos = Vector2.zero;
            if (imgIcon != null && imgIcon.gameObject.activeInHierarchy)
            {
                // Lấy canvas của popup để truy xuất worldCamera chính xác
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    ? canvas.worldCamera
                    : null;

                _capturedIconScreenPos = RectTransformUtility.WorldToScreenPoint(
                    uiCam,
                    imgIcon.transform.position
                );
            }

            _onClose?.Invoke();
            Hide();
        }

        /// <summary>
        /// Spawn một clone của icon ngay tại vị trí hiện tại của imgIcon (trước khi popup đóng).
        /// Clone được đặt trên flyRoot và có idle bobbing animation.
        /// UIStateController sử dụng clone này để trigger animation sau khi BottomHUD slide vào.
        /// </summary>
        public GameObject SpawnIconClone(Transform flyRoot)
        {
            if (imgIcon == null || imgIcon.sprite == null || flyRoot == null)
                return null;

            // Lấy canvas của flyRoot để convert toạ độ
            Canvas canvas = flyRoot.GetComponentInParent<Canvas>();
            Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            // Lấy screen pos của icon (dùng giá trị đã capture hoặc lấy lại ngay bây giờ)
            Vector2 iconScreenPos = _capturedIconScreenPos != Vector2.zero
                ? _capturedIconScreenPos
                : RectTransformUtility.WorldToScreenPoint(uiCam, imgIcon.transform.position);

            // Tạo clone
            GameObject cloneGo = new GameObject("BoosterUnlockIcon_Fly");
            cloneGo.transform.SetParent(flyRoot, false);

            Image cloneImg = cloneGo.AddComponent<Image>();
            cloneImg.sprite = imgIcon.sprite;
            cloneImg.raycastTarget = false;

            RectTransform cloneRect = cloneGo.GetComponent<RectTransform>();
            
            // Dùng kích cỡ thực tế từ icon gốc của popup thay vì set cứng 80x80
            cloneRect.sizeDelta = imgIcon.rectTransform.sizeDelta;
            cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
            cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
            cloneRect.pivot = new Vector2(0.5f, 0.5f);

            // Đặt clone đúng vị trí icon trên flyRoot
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)flyRoot,
                iconScreenPos,
                uiCam,
                out Vector2 localPos
            );
            cloneRect.anchoredPosition = localPos;
            cloneRect.localScale = imgIcon.transform.localScale;

            // Idle bobbing nhẹ trong khi chờ BottomHUD slide vào
            // Dùng DOAnchorPosY với anchoredPosition để tránh xung đột hệ tọa độ
            cloneRect.DOKill();
            float baseAnchoredY = cloneRect.anchoredPosition.y;
            cloneRect.DOAnchorPosY(baseAnchoredY + 10f, 0.7f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(cloneGo);

            return cloneGo;
        }

        private void StartTapPulse()
        {
            if (txtInstruction == null) return;

            _tapFadeTween = txtInstruction.DOFade(tapPulseAlpha, tapPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(txtInstruction.gameObject, LinkBehaviour.KillOnDisable);

            _tapScaleTween = txtInstruction.transform.DOScale(tapPulseScale, tapPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(txtInstruction.gameObject, LinkBehaviour.KillOnDisable);
        }

        private void StartIconIdle()
        {
            if (imgIcon == null || !imgIcon.gameObject.activeInHierarchy) return;

            _iconIdleScaleTween = imgIcon.transform.DOScale(_iconOriginalScale * iconIdleScale, iconIdleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(imgIcon.gameObject, LinkBehaviour.KillOnDisable);

            _iconIdleMoveTween = imgIcon.transform.DOLocalMoveY(_iconOriginalPosition.y + iconIdleMoveY, iconIdleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(imgIcon.gameObject, LinkBehaviour.KillOnDisable);
        }

        private void CacheOriginalState()
        {
            if (txtTitle != null) _titleOriginalPosition = txtTitle.transform.localPosition;
            if (txtDescription != null) _descriptionOriginalPosition = txtDescription.transform.localPosition;
            if (txtInstruction != null) _instructionOriginalPosition = txtInstruction.transform.localPosition;
            if (imgIcon != null)
            {
                _iconOriginalScale = imgIcon.transform.localScale;
                _iconOriginalPosition = imgIcon.transform.localPosition;
            }
            if (contentPanel != null) _contentOriginalScale = contentPanel.localScale;

            if (_iconOriginalScale == Vector3.zero) _iconOriginalScale = Vector3.one;
            if (_contentOriginalScale == Vector3.zero) _contentOriginalScale = Vector3.one;
        }

        private void KillAllTweens()
        {
            contentPanel?.DOKill();
            txtTitle?.DOKill();
            txtTitle?.transform.DOKill();
            txtDescription?.DOKill();
            txtDescription?.transform.DOKill();
            txtInstruction?.DOKill();
            txtInstruction?.transform.DOKill();
            imgIcon?.DOKill();
            imgIcon?.transform.DOKill();

            _iconIdleScaleTween?.Kill();
            _iconIdleMoveTween?.Kill();
            _iconIdleScaleTween = null;
            _iconIdleMoveTween = null;
            _tapFadeTween?.Kill();
            _tapScaleTween?.Kill();
            _tapFadeTween = null;
            _tapScaleTween = null;
        }
    }
}
