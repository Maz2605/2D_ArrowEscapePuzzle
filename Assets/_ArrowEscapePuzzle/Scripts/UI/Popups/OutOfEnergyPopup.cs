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
        [SerializeField] private Button btnClose;
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
        private Vector3 _btnCloseOrigScale;

        // Tween references
        private Tween _floatTween;
        private Tween _rotateTween;
        private Tween _badgePulseTween;

        protected override void Awake()
        {
            base.Awake();

            if (txtTitle != null) _titleOrigPos = txtTitle.transform.localPosition;
            if (txtCountdown != null) _countdownOrigPos = txtCountdown.transform.localPosition;

            _iconContainerOrigScale = energyIconContainer != null ? energyIconContainer.localScale : Vector3.one;
            _badgeOrigScale = badgeContainer != null ? badgeContainer.localScale : Vector3.one;
            _btnBuyOrigScale = btnBuyFullEnergy != null ? btnBuyFullEnergy.transform.localScale : Vector3.one;
            _btnCloseOrigScale = btnClose != null ? btnClose.transform.localScale : Vector3.one;

            BindButton(btnBuyFullEnergy, OnBuyFullEnergyClicked);
            BindButton(btnClose, Hide);
            BindButton(btnBackground, Hide);
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
                popupPanel.localScale = Vector3.one * 0.5f;
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
                txtCountdown.transform.localPosition = _countdownOrigPos + new Vector3(0f, -20f, 0f);
            }

            if (btnBuyFullEnergy != null) btnBuyFullEnergy.transform.localScale = Vector3.zero;
            if (btnClose != null) btnClose.transform.localScale = Vector3.zero;
        }

        protected override void PlayShowAnimation()
        {
            if (popupPanel == null) return;

            KillAllAnimations();

            Sequence showSeq = DOTween.Sequence();
            showSeq.SetUpdate(true).SetLink(gameObject);

            // Pop open main panel
            showSeq.Append(popupPanel.DOScale(Vector3.one, animDuration * 1.5f).SetEase(Ease.OutBack));

            float timeStep = 0.08f;

            // 1. Title
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
                showSeq.Insert(iconTime, energyIconContainer.DOScale(_iconContainerOrigScale, animDuration * 1.5f).SetEase(Ease.OutElastic));
            }

            // 3. Countdown Text (Fade in + Slide up)
            if (txtCountdown != null)
            {
                float subTime = timeStep * 2.5f;
                showSeq.Insert(subTime, txtCountdown.DOFade(1f, animDuration * 0.6f));
                showSeq.Insert(subTime, txtCountdown.transform.DOLocalMoveY(_countdownOrigPos.y, animDuration * 0.6f).SetEase(Ease.OutCubic));
            }

            // 4. Badge (Scale pop shortly after icon)
            if (badgeContainer != null)
            {
                float badgeTime = timeStep * 3f;
                showSeq.Insert(badgeTime, badgeContainer.DOScale(_badgeOrigScale * 1.2f, animDuration * 0.8f).SetEase(Ease.OutBack));
                showSeq.Insert(badgeTime + animDuration * 0.8f, badgeContainer.DOScale(_badgeOrigScale, animDuration * 0.3f));
            }
            if (txtEnergyValue != null)
            {
                showSeq.Insert(timeStep * 3f, txtEnergyValue.DOFade(1f, animDuration * 0.5f));
            }

            // 5. Buttons (Staggered pop)
            if (btnBuyFullEnergy != null)
            {
                showSeq.Insert(timeStep * 3.5f, btnBuyFullEnergy.transform.DOScale(_btnBuyOrigScale, animDuration * 0.6f).SetEase(Ease.OutBack));
            }
            if (btnClose != null)
            {
                showSeq.Insert(timeStep * 4.0f, btnClose.transform.DOScale(_btnCloseOrigScale, animDuration * 0.6f).SetEase(Ease.OutBack));
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

            if (popupPanel == null)
            {
                onComplete?.Invoke();
                return;
            }

            popupPanel.DOScale(Vector3.one * 0.5f, animDuration * 0.8f)
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

            if (popupPanel != null) popupPanel.DOKill();
            if (txtTitle != null) txtTitle.transform.DOKill();
            if (txtEnergyValue != null) txtEnergyValue.transform.DOKill();
            if (txtCountdown != null) txtCountdown.transform.DOKill();
            if (energyIconContainer != null) energyIconContainer.DOKill();
            if (energyIcon != null) energyIcon.DOKill();
            if (badgeContainer != null) badgeContainer.DOKill();
            if (btnBuyFullEnergy != null) btnBuyFullEnergy.transform.DOKill();
            if (btnClose != null) btnClose.transform.DOKill();
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
