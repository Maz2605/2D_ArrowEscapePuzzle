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
        [SerializeField] private RectTransform panelContainer;
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
        private Vector2 _panelOriginPos;
        private bool _hasSavedOriginPos;
        private Vector3 _titleOrigPos;
        private Vector3 _subtitleOrigPos;
        private Vector3 _iconOrigScale;
        private Vector3 _badgeOrigScale;
        private Vector3 _btnConfirmOrigScale;
        private Vector3 _btnCancelOrigScale;

        // Tween references to prevent memory leaks/overlapping
        private Tween _floatTween;
        private Tween _rotateTween;
        private Tween _btnConfirmPulseTween;

        protected override void Awake()
        {
            base.Awake();

            if (panelContainer != null)
            {
                _panelOriginPos = panelContainer.anchoredPosition;
                _hasSavedOriginPos = true;
            }

            // Cache original positions and scales
            if (txtTitle != null) _titleOrigPos = txtTitle.transform.localPosition;
            if (txtSubtitle != null) _subtitleOrigPos = txtSubtitle.transform.localPosition;
            
            _iconOrigScale = energyIconContainer != null ? energyIconContainer.localScale : Vector3.one;
            _badgeOrigScale = badgeContainer != null ? badgeContainer.localScale : Vector3.one;
            _btnConfirmOrigScale = btnConfirm != null ? btnConfirm.transform.localScale : Vector3.one;
            _btnCancelOrigScale = btnCancel != null ? btnCancel.transform.localScale : Vector3.one;

            BindButton(btnConfirm, OnConfirmClicked);
            BindButton(btnCancel, OnCancelClicked);
            
            if (btnBackground != null)
            {
                btnBackground.onClick.RemoveAllListeners();
                btnBackground.onClick.AddListener(OnCancelClicked);
            }
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
                if (!_hasSavedOriginPos)
                {
                    _panelOriginPos = panelContainer.anchoredPosition;
                    _hasSavedOriginPos = true;
                }
                panelContainer.localScale = Vector3.one * 0.8f;
                panelContainer.anchoredPosition = new Vector2(_panelOriginPos.x, _panelOriginPos.y - 1200f);
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

            // Pop open main panel with slide up and scale up
            float panelDur = animDuration * 1.2f;
            showSeq.Append(panelContainer.DOAnchorPosY(_panelOriginPos.y, panelDur).SetEase(Ease.OutCubic));
            showSeq.Join(panelContainer.DOScale(Vector3.one, panelDur).SetEase(Ease.OutCubic));

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

            // Pulse accept button size gently
            if (btnConfirm != null)
            {
                _btnConfirmPulseTween = btnConfirm.transform.DOScale(_btnConfirmOrigScale * 1.06f, 1.2f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(btnConfirm.gameObject);
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

            float hideDur = animDuration * 0.8f;
            panelContainer.DOScale(Vector3.one * 0.8f, hideDur)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetLink(gameObject);

            panelContainer.DOAnchorPosY(_panelOriginPos.y - 1200f, hideDur)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    panelContainer.anchoredPosition = _panelOriginPos;
                    onComplete?.Invoke();
                });
        }

        private void KillAllAnimations()
        {
            _floatTween?.Kill();
            _rotateTween?.Kill();
            _btnConfirmPulseTween?.Kill();

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
