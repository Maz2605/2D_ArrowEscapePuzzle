using System;
using ArrowGame.UI.Base;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ArrowGame.UI.Components;

namespace ArrowGame.UI.Popups
{
    public class WinPopup : BasePopup
    {
        [Header("--- Level Info ---")] [SerializeField]
        private TextMeshProUGUI txtLevel;

        [Header("--- UI Components ---")] [SerializeField]
        private StarBarWidget starBarWidget;

        [SerializeField] private TextMeshProUGUI txtCoinReward;
        [SerializeField] private CoinWidget totalCoinWidget;

        [Header("--- Buttons ---")] [SerializeField]
        private Button btnNextLevel;

        [SerializeField] private Button btnHome;

        [Header("VFX")] [SerializeField] private GameObject congratsVFX;

        private Sequence _winSequence;

        protected override void Awake()
        {
            base.Awake();
            if (btnNextLevel != null) btnNextLevel.onClick.AddListener(OnNextClicked);
            if (btnHome != null) btnHome.onClick.AddListener(OnHomeClicked);
        }

        public void SetupAndAnimate(int levelIndex, int targetStars, int targetCoins)
        {
            int currentTotalCoin = DataManager.Instance.GetCurrentCoin();
            int coinBeforeReward = currentTotalCoin - targetCoins;

            if (totalCoinWidget != null) totalCoinWidget.SetInitialValue(coinBeforeReward);
            if (txtLevel != null) txtLevel.text = $"LEVEL {levelIndex}";

            if (txtCoinReward != null)
            {
                txtCoinReward.text = "+0";
                txtCoinReward.transform.localScale = Vector3.zero;
            }

            if (starBarWidget != null) starBarWidget.ResetAllStars();

            _winSequence?.Kill();
            _winSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            _winSequence.AppendInterval(0.2f);

            if (starBarWidget != null)
            {
                starBarWidget.AppendAnimationToSequence(_winSequence, targetStars);
            }

            _winSequence.AppendInterval(0.1f);

            // 5. Sequence diễn đếm tiền thưởng
            if (txtCoinReward != null)
            {
                int displayReward = 0;
                _winSequence.Append(txtCoinReward.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                _winSequence.Append(
                    DOTween.To(() => displayReward, x =>
                    {
                        displayReward = x;
                        txtCoinReward.text = $"+{displayReward}";
                    }, targetCoins, 0.6f).SetEase(Ease.OutExpo)
                );
            }

            if (totalCoinWidget != null)
            {
                _winSequence.Append(totalCoinWidget.PlayCountAnimation(coinBeforeReward, currentTotalCoin, 0.5f));
            }
        }

        private void OnNextClicked()
        {
            Hide();
            EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel);
        }

        private void OnHomeClicked()
        {
            Hide();
            GameManager.Instance.RequestBackHome();
        }

        protected override void PlayShowAnimation()
        {
            transform.localScale = Vector3.one * 0.8f;
            transform.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack).SetUpdate(true);
            DOVirtual.DelayedCall(1.5f,
                () => GlobalVFXManager.Instance.PlayVFX(congratsVFX, congratsVFX.transform.position,
                    Quaternion.identity, 60f)
                );
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            transform.DOScale(Vector3.one * 0.8f, animDuration).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        protected void OnDestroy()
        {
            if (btnNextLevel != null) btnNextLevel.onClick.RemoveAllListeners();
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();

            _winSequence?.Kill();
            transform.DOKill();
        }
    }
}