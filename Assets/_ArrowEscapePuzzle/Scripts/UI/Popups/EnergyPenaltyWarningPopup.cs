using System;
using ArrowGame.UI.Base;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace ArrowGame.UI.Popups
{
    public class EnergyPenaltyWarningPopup : BasePopup
    {
        [Header("--- UI References ---")]
        [SerializeField] private Transform panelContainer;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnBackground;

        [Header("--- Premium Animation References ---")]
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private RectTransform energyIconContainer;
        [SerializeField] private RectTransform lightningIcon;
        [SerializeField] private RectTransform badgeContainer;
        [SerializeField] private TextMeshProUGUI txtSubtitle;

        private Action _onConfirm;
        private Action _onCancel;

        // Cache positions and scales
        private Vector3 _titleOrigPos;
        private Vector3 _subtitleOrigPos;
        private Vector3 _iconOrigScale;
        private Vector3 _badgeOrigScale;
        private Vector3 _btnConfirmOrigScale;
        private Vector3 _btnCancelOrigScale;

        // Tween references to prevent memory leaks/overlapping
        private Tween _floatTween;
        private Tween _rotateTween;
        private Tween _badgePulseTween;

        protected override void Awake()
        {
            base.Awake();

            // Cache original positions and scales
            if (txtTitle != null) _titleOrigPos = txtTitle.transform.localPosition;
            if (txtSubtitle != null) _subtitleOrigPos = txtSubtitle.transform.localPosition;
            
            _iconOrigScale = energyIconContainer != null ? energyIconContainer.localScale : Vector3.one;
            _badgeOrigScale = badgeContainer != null ? badgeContainer.localScale : Vector3.one;
            _btnConfirmOrigScale = btnConfirm != null ? btnConfirm.transform.localScale : Vector3.one;
            _btnCancelOrigScale = btnCancel != null ? btnCancel.transform.localScale : Vector3.one;

            BindButton(btnConfirm, OnConfirmClicked);
            BindButton(btnCancel, OnCancelClicked);
            BindButton(btnBackground, OnCancelClicked);
        }

        public void SetupActions(Action onConfirm, Action onCancel = null)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
        }

        private void OnConfirmClicked()
        {
            _onConfirm?.Invoke();
            Hide();
        }

        private void OnCancelClicked()
        {
            _onCancel?.Invoke();
            Hide();
        }

        private void OnDisable()
        {
            KillAllAnimations();
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();

            KillAllAnimations();

            if (panelContainer != null)
            {
                panelContainer.localScale = Vector3.one * 0.5f;
            }

            // Set starting state for animation
            if (txtTitle != null)
            {
                txtTitle.alpha = 0f;
                txtTitle.transform.localScale = Vector3.zero;
                txtTitle.transform.localPosition = _titleOrigPos + new Vector3(0f, 30f, 0f);
            }

            if (energyIconContainer != null)
            {
                energyIconContainer.localScale = Vector3.zero;
            }

            if (lightningIcon != null)
            {
                lightningIcon.localScale = Vector3.one;
                lightningIcon.localRotation = Quaternion.identity;
                lightningIcon.localPosition = Vector3.zero;
            }

            if (badgeContainer != null)
            {
                badgeContainer.localScale = Vector3.zero;
            }

            if (txtSubtitle != null)
            {
                txtSubtitle.alpha = 0f;
                txtSubtitle.transform.localScale = Vector3.one;
                txtSubtitle.transform.localPosition = _subtitleOrigPos + new Vector3(0f, -30f, 0f);
            }

            if (btnConfirm != null) btnConfirm.transform.localScale = Vector3.zero;
            if (btnCancel != null) btnCancel.transform.localScale = Vector3.zero;
        }

        protected override void PlayShowAnimation()
        {
            if (panelContainer == null) return;

            KillAllAnimations();

            Sequence showSeq = DOTween.Sequence();
            showSeq.SetUpdate(true).SetLink(gameObject);

            // Pop open main panel
            showSeq.Append(panelContainer.DOScale(Vector3.one, animDuration * 1.5f).SetEase(Ease.OutBack));

            float timeStep = 0.08f;

            // 1. Title Animation (Fade in + Slide down + Scale pop)
            if (txtTitle != null)
            {
                showSeq.Insert(0.05f, txtTitle.DOFade(1f, animDuration * 0.5f));
                showSeq.Insert(0.05f, txtTitle.transform.DOScale(1f, animDuration * 0.5f).SetEase(Ease.OutBack));
                showSeq.Insert(0.05f, txtTitle.transform.DOLocalMoveY(_titleOrigPos.y, animDuration * 0.8f).SetEase(Ease.OutBounce));
            }

            // 2. Energy Icon Container (Bouncy scale up)
            if (energyIconContainer != null)
            {
                float iconTime = timeStep * 1.5f;
                showSeq.Insert(iconTime, energyIconContainer.DOScale(_iconOrigScale, animDuration * 1.5f).SetEase(Ease.OutElastic));
            }

            // 3. Subtitle (Fade in + Slide up)
            if (txtSubtitle != null)
            {
                float subTime = timeStep * 2.5f;
                showSeq.Insert(subTime, txtSubtitle.DOFade(1f, animDuration * 0.6f));
                showSeq.Insert(subTime, txtSubtitle.transform.DOLocalMoveY(_subtitleOrigPos.y, animDuration * 0.6f).SetEase(Ease.OutCubic));
            }

            // 4. Badge (Scale pop shortly after icon)
            if (badgeContainer != null)
            {
                float badgeTime = timeStep * 3f;
                showSeq.Insert(badgeTime, badgeContainer.DOScale(_badgeOrigScale * 1.2f, animDuration * 0.8f).SetEase(Ease.OutBack));
                showSeq.Insert(badgeTime + animDuration * 0.8f, badgeContainer.DOScale(_badgeOrigScale, animDuration * 0.3f));
            }

            // 5. Buttons (Staggered pop)
            if (btnConfirm != null)
            {
                showSeq.Insert(timeStep * 3.5f, btnConfirm.transform.DOScale(_btnConfirmOrigScale, animDuration * 0.6f).SetEase(Ease.OutBack));
            }
            if (btnCancel != null)
            {
                showSeq.Insert(timeStep * 4.0f, btnCancel.transform.DOScale(_btnCancelOrigScale, animDuration * 0.6f).SetEase(Ease.OutBack));
            }

            // Trigger continuous idle animations on completion
            showSeq.OnComplete(StartIdleAnimations);
        }

        private void StartIdleAnimations()
        {
            // Floating & rotating lightning bolt
            if (lightningIcon != null)
            {
                _floatTween = lightningIcon.DOLocalMoveY(5f, 1.8f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(lightningIcon.gameObject);

                _rotateTween = lightningIcon.DOLocalRotate(new Vector3(0, 0, 4f), 2.2f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(lightningIcon.gameObject);
            }

            // Pulse badge size
            if (badgeContainer != null)
            {
                _badgePulseTween = badgeContainer.DOScale(_badgeOrigScale * 1.12f, 1.1f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(badgeContainer.gameObject);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillAllAnimations();

            if (panelContainer == null)
            {
                onComplete?.Invoke();
                return;
            }

            panelContainer.DOScale(Vector3.one * 0.5f, animDuration * 0.8f)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void KillAllAnimations()
        {
            _floatTween?.Kill();
            _rotateTween?.Kill();
            _badgePulseTween?.Kill();

            if (panelContainer != null) panelContainer.DOKill();
            if (txtTitle != null) txtTitle.transform.DOKill();
            if (txtSubtitle != null) txtSubtitle.transform.DOKill();
            if (energyIconContainer != null) energyIconContainer.DOKill();
            if (lightningIcon != null) lightningIcon.DOKill();
            if (badgeContainer != null) badgeContainer.DOKill();
            if (btnConfirm != null) btnConfirm.transform.DOKill();
            if (btnCancel != null) btnCancel.transform.DOKill();
        }
    }
}
