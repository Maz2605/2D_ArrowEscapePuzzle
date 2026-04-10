using System;
using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ArrowGame.UI.Popups
{
    public class BoosterBuyPopup : BasePopup
    {
        [Header("--- UI References ---")]
        [SerializeField] private Transform panelContainer;
        [SerializeField] private TextMeshProUGUI txtBoosterName;
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtPrice;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;

        private BoosterConfigSO _currentConfig;
        private Action _onBuySuccess;

        protected override void Awake()
        {
            base.Awake();
            BindButton(btnConfirm, OnConfirmClicked);
            BindButton(btnCancel, Hide); 
        }

        public void Setup(BoosterConfigSO config, Action onBuySuccess)
        {
            _currentConfig = config;
            _onBuySuccess = onBuySuccess;

            txtBoosterName.text = config.boosterName.ToUpper();
            txtDescription.text = config.description;
            txtPrice.text = config.price.ToString();

            if (imgIcon != null && config.boosterIcon != null)
            {
                imgIcon.sprite = config.boosterIcon;
            }

            bool canAfford = DataManager.Instance.GetCurrentCoin() >= config.price;
            txtPrice.color = canAfford ? Color.white : Color.red;
        }

        private void OnConfirmClicked()
        {
            if (_currentConfig == null) return;

            if (DataManager.Instance.TrySpendCoin(_currentConfig.price))
            {
                DataManager.Instance.AddBooster(_currentConfig.type, 1);
                Hide();
                _onBuySuccess?.Invoke(); 
            }
            else
            {
                txtPrice.transform.DOKill();
                txtPrice.transform.localScale = Vector3.one;
                txtPrice.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 10, 1f).SetUpdate(true);
                
                UIManager.Instance.ShowToast("NOT ENOUGH COINS!", 1.5f);
            }
        }

        // --- Tái sử dụng Animation của Confirm Popup ---
        protected override void PlayShowAnimation()
        {
            if (panelContainer != null)
            {
                panelContainer.DOKill();
                panelContainer.localScale = Vector3.one * 0.8f;
                panelContainer.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            if (panelContainer != null)
            {
                panelContainer.DOKill();
                panelContainer.DOScale(Vector3.one * 0.8f, animDuration).SetEase(Ease.InBack).SetUpdate(true).SetLink(gameObject)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}