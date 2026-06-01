using System;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Manager;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Popups
{
    public sealed class OutOfEnergyPopup : BasePopup
    {
        [Header("--- UI References ---")]
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtEnergyValue;
        [SerializeField] private TextMeshProUGUI txtCountdown;
        [SerializeField] private Button btnBuyFullEnergy;
        [SerializeField] private TextMeshProUGUI txtBuyFullEnergyPrice;
        [SerializeField] private Button btnBackground;

        [Header("--- Premium Animation References ---")]
        [SerializeField] private RectTransform energyIconContainer;
        [SerializeField] private RectTransform energyIcon;
        [SerializeField] private RectTransform badgeContainer;

        [Header("--- Content Settings ---")]
        [SerializeField] private string titleText = "Out of energy";
        [SerializeField] private string countdownPrefix = "Next energy in ";
        [SerializeField] private string fullEnergyLabel = "Energy is full.";
        [SerializeField] private int fullEnergyPrice = 500;

        // Cache positions and scales
        private Vector3 _titleOrigPos;
        private Vector3 _countdownOrigPos;
        private Vector3 _iconContainerOrigScale;
        private Vector3 _badgeOrigScale;
        private Vector3 _btnBuyOrigScale;

        private Vector2 _panelOriginPos;
        private bool _hasSavedOriginPos;

        // Tween references
        private Tween _floatTween;
        private Tween _rotateTween;

        protected override void Awake()
        {
            base.Awake();

            if (popupPanel != null)
            {
                _panelOriginPos = popupPanel.anchoredPosition;
                _hasSavedOriginPos = true;
            }

            if (txtTitle != null) _titleOrigPos = txtTitle.transform.localPosition;
            if (txtCountdown != null) _countdownOrigPos = txtCountdown.transform.localPosition;

            _iconContainerOrigScale = energyIconContainer != null ? energyIconContainer.localScale : Vector3.one;
            _badgeOrigScale = badgeContainer != null ? badgeContainer.localScale : Vector3.one;
            _btnBuyOrigScale = btnBuyFullEnergy != null ? btnBuyFullEnergy.transform.localScale : Vector3.one;

            BindButton(btnBuyFullEnergy, OnBuyFullEnergyClicked);
            
            // Bind background directly without visual punch animation
            if (btnBackground != null)
            {
                btnBackground.onClick.RemoveAllListeners();
                btnBackground.onClick.AddListener(Hide);
            }
        }

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.EnergyChanged, OnEnergyChanged);
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.EnergyTimerChanged, OnEnergyTimerChanged);
            RefreshContent();
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.EnergyChanged, OnEnergyChanged);
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.EnergyTimerChanged, OnEnergyTimerChanged);
            KillAllAnimations();
        }

        public void RefreshContent()
        {
            if (DataManager.Instance == null) return;

            int currentEnergy = DataManager.Instance.GetCurrentEnergy();
            int maxEnergy = DataManager.Instance.GetMaxEnergy();
            int remainingSeconds = DataManager.Instance.GetRemainingRecoverySeconds();
            bool isFull = currentEnergy >= maxEnergy;

            if (txtTitle != null)
            {
                txtTitle.text = titleText;
            }

            if (txtEnergyValue != null)
            {
                txtEnergyValue.SetText("{0}", currentEnergy);
            }

            if (txtBuyFullEnergyPrice != null)
            {
                txtBuyFullEnergyPrice.SetText("{0}", fullEnergyPrice);
                txtBuyFullEnergyPrice.color = DataManager.Instance.GetCurrentCoin() >= fullEnergyPrice ? Color.white : Color.red;
            }

            if (btnBuyFullEnergy != null)
            {
                btnBuyFullEnergy.interactable = !isFull;
            }

            UpdateCountdownLabel(remainingSeconds, isFull);
        }

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();

            KillAllAnimations();

            if (popupPanel != null)
            {
                if (!_hasSavedOriginPos)
                {
                    _panelOriginPos = popupPanel.anchoredPosition;
                    _hasSavedOriginPos = true;
                }
                popupPanel.localScale = Vector3.one * 0.8f;
                // Start below the screen to slide up (trượt lên khi xuất hiện)
                popupPanel.anchoredPosition = new Vector2(_panelOriginPos.x, _panelOriginPos.y - 1200f);
            }

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

            if (energyIcon != null)
            {
                energyIcon.localScale = Vector3.one;
                energyIcon.localRotation = Quaternion.identity;
                energyIcon.localPosition = Vector3.zero;
            }

            if (badgeContainer != null)
            {
                badgeContainer.localScale = Vector3.zero;
            }

            if (txtEnergyValue != null)
            {
                txtEnergyValue.alpha = 0f;
            }

            if (txtCountdown != null)
            {
                txtCountdown.alpha = 0f;
                txtCountdown.transform.localScale = Vector3.one;
                // Slides down from above (top-to-bottom casual feel)
                txtCountdown.transform.localPosition = _countdownOrigPos + new Vector3(0f, 20f, 0f);
            }

            if (btnBuyFullEnergy != null) btnBuyFullEnergy.transform.localScale = Vector3.zero;
        }

        protected override void PlayShowAnimation()
        {
            if (popupPanel == null) return;

            KillAllAnimations();

            Sequence showSeq = DOTween.Sequence();
            showSeq.SetUpdate(true).SetLink(gameObject);

            // Pop open main panel with a gentle top-to-bottom slide and soft scale up
            float panelDur = animDuration * 1.2f;
            showSeq.Append(popupPanel.DOAnchorPosY(_panelOriginPos.y, panelDur).SetEase(Ease.OutCubic));
            showSeq.Join(popupPanel.DOScale(Vector3.one, panelDur).SetEase(Ease.OutCubic));
            showSeq.AppendInterval(0.04f);

            // 1. Title
            if (txtTitle != null)
            {
                float titleDur = animDuration * 0.7f;
                showSeq.Append(txtTitle.DOFade(1f, titleDur));
                showSeq.Join(txtTitle.transform.DOScale(1f, titleDur).SetEase(Ease.OutCubic));
                showSeq.Join(txtTitle.transform.DOLocalMoveY(_titleOrigPos.y, titleDur).SetEase(Ease.OutCubic));
                showSeq.AppendInterval(0.04f);
            }

            // 2. Energy Icon Container (Gentle scale up with soft bounce)
            if (energyIconContainer != null)
            {
                float iconDur = animDuration * 0.8f;
                showSeq.Append(energyIconContainer.DOScale(_iconContainerOrigScale, iconDur).SetEase(Ease.OutBack));
                showSeq.AppendInterval(0.04f);
            }

            // 3. Badge (Scale pop shortly after icon)
            if (badgeContainer != null)
            {
                float badgeDur = animDuration * 0.6f;
                showSeq.Append(badgeContainer.DOScale(_badgeOrigScale * 1.15f, badgeDur).SetEase(Ease.OutBack));
                if (txtEnergyValue != null)
                {
                    showSeq.Join(txtEnergyValue.DOFade(1f, badgeDur * 0.8f));
                }
                showSeq.Append(badgeContainer.DOScale(_badgeOrigScale, badgeDur * 0.4f));
                showSeq.AppendInterval(0.04f);
            }

            // 4. Countdown Text (Fade in + Slide down)
            if (txtCountdown != null)
            {
                float countDur = animDuration * 0.7f;
                showSeq.Append(txtCountdown.DOFade(1f, countDur));
                showSeq.Join(txtCountdown.transform.DOLocalMoveY(_countdownOrigPos.y, countDur).SetEase(Ease.OutCubic));
                showSeq.AppendInterval(0.04f);
            }

            // 5. Buttons (Staggered pop)
            if (btnBuyFullEnergy != null)
            {
                float btnDur = animDuration * 0.6f;
                showSeq.Append(btnBuyFullEnergy.transform.DOScale(_btnBuyOrigScale, btnDur).SetEase(Ease.OutBack));
            }

            showSeq.OnComplete(StartIdleAnimations);
        }

        private void StartIdleAnimations()
        {
            if (energyIcon != null)
            {
                _floatTween = energyIcon.DOLocalMoveY(6f, 1.8f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(energyIcon.gameObject);

                _rotateTween = energyIcon.DOLocalRotate(new Vector3(0, 0, 3f), 2.4f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(energyIcon.gameObject);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillAllAnimations();

            if (popupPanel == null)
            {
                onComplete?.Invoke();
                return;
            }

            float hideDur = animDuration * 0.8f;
            popupPanel.DOScale(Vector3.one * 0.8f, hideDur)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetLink(gameObject);

            popupPanel.DOAnchorPosY(_panelOriginPos.y - 1200f, hideDur)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    popupPanel.anchoredPosition = _panelOriginPos;
                    onComplete?.Invoke();
                });
        }

        private void KillAllAnimations()
        {
            _floatTween?.Kill();
            _rotateTween?.Kill();

            if (popupPanel != null) popupPanel.DOKill();
            if (txtTitle != null) txtTitle.transform.DOKill();
            if (txtEnergyValue != null) txtEnergyValue.transform.DOKill();
            if (txtCountdown != null) txtCountdown.transform.DOKill();
            if (energyIconContainer != null) energyIconContainer.DOKill();
            if (energyIcon != null) energyIcon.DOKill();
            if (badgeContainer != null) badgeContainer.DOKill();
            if (btnBuyFullEnergy != null) btnBuyFullEnergy.transform.DOKill();
        }

        private void OnBuyFullEnergyClicked()
        {
            if (DataManager.Instance == null) return;

            if (DataManager.Instance.GetCurrentEnergy() >= DataManager.Instance.GetMaxEnergy())
            {
                UIManager.Instance?.ShowToast("ENERGY FULL!", 1.2f);
                RefreshContent();
                return;
            }

            if (!DataManager.Instance.TrySpendCoin(fullEnergyPrice))
            {
                UIManager.Instance?.ShowToast("NOT ENOUGH COINS!", 1.5f);

                if (txtBuyFullEnergyPrice != null)
                {
                    txtBuyFullEnergyPrice.transform.DOKill();
                    txtBuyFullEnergyPrice.transform.localScale = Vector3.one;
                    txtBuyFullEnergyPrice.transform
                        .DOPunchScale(Vector3.one * 0.25f, 0.2f, 8, 1f)
                        .SetUpdate(true)
                        .SetLink(txtBuyFullEnergyPrice.gameObject, LinkBehaviour.KillOnDisable);
                }

                RefreshContent();
                return;
            }

            DataManager.Instance.RefillEnergyToMax();
            RefreshContent();
            Hide();
        }

        private void OnEnergyChanged(int _)
        {
            RefreshContent();
        }

        private void OnEnergyTimerChanged(int remainingSeconds)
        {
            if (DataManager.Instance == null) return;
            UpdateCountdownLabel(remainingSeconds, DataManager.Instance.GetCurrentEnergy() >= DataManager.Instance.GetMaxEnergy());
        }

        private void UpdateCountdownLabel(int remainingSeconds, bool isFull)
        {
            if (txtCountdown == null) return;

            if (isFull)
            {
                txtCountdown.text = fullEnergyLabel;
                return;
            }

            txtCountdown.text = $"{countdownPrefix}{FormatClock(remainingSeconds)}";
        }

        private static string FormatClock(int remainingSeconds)
        {
            int safeSeconds = Mathf.Max(0, remainingSeconds);
            int minutes = safeSeconds / 60;
            int seconds = safeSeconds % 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
