using System;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Manager;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Popups
{
    public sealed class RequestBuyHeartPopup : BasePopup
    {
        [Header("--- UI References ---")]
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private Button btnAddLives;
        [SerializeField] private Button btnTryAgain;
        [SerializeField] private TextMeshProUGUI txtPrice;

        [Header("--- Premium Animation References ---")]
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private RectTransform heartIconContainer;
        [SerializeField] private RectTransform heartIcon;
        [SerializeField] private RectTransform badgeContainer;
        [SerializeField] private RectTransform pricePanel;

        private int _coinPrice = 100;
        private Action _onBuySuccess;
        private Action _onTryAgain;

        private Vector2 _panelOriginPos;
        private bool _hasSavedOriginPos;

        // Cache positions and scales
        private Vector3 _titleOrigPos;
        private Vector3 _pricePanelOrigPos;
        private Vector3 _heartContainerOrigScale;
        private Vector3 _badgeOrigScale;
        private Vector3 _btnBuyOrigScale;
        private Vector3 _btnTryAgainOrigScale;

        // Tween references
        private Tween _heartBeatTween;
        private Tween _badgePulseTween;
        private Tween _floatTween;
        private Tween _btnBuyPulseTween;

        protected override void Awake()
        {
            base.Awake();

            if (popupPanel != null)
            {
                _panelOriginPos = popupPanel.anchoredPosition;
                _hasSavedOriginPos = true;
            }

            if (txtTitle != null) _titleOrigPos = txtTitle.transform.localPosition;
            if (pricePanel != null) _pricePanelOrigPos = pricePanel.localPosition;

            _heartContainerOrigScale = heartIconContainer != null ? heartIconContainer.localScale : Vector3.one;
            _badgeOrigScale = badgeContainer != null ? badgeContainer.localScale : Vector3.one;
            _btnBuyOrigScale = btnAddLives != null ? btnAddLives.transform.localScale : Vector3.one;
            _btnTryAgainOrigScale = btnTryAgain != null ? btnTryAgain.transform.localScale : Vector3.one;

            BindButton(btnAddLives, OnBuyLivesClicked);
            BindButton(btnTryAgain, OnTryAgainClicked);
        }

        public void Setup(int price, Action onBuySuccess, Action onTryAgain)
        {
            _coinPrice = price;
            _onBuySuccess = onBuySuccess;
            _onTryAgain = onTryAgain;

            if (txtPrice != null)
            {
                txtPrice.SetText("{0}", price);
                bool hasEnoughCoins = DataManager.Instance != null && DataManager.Instance.GetCurrentCoin() >= price;
                txtPrice.color = hasEnoughCoins ? Color.white : Color.red;
            }
        }

        private void OnDisable()
        {
            KillAllAnimations();
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
                popupPanel.anchoredPosition = new Vector2(_panelOriginPos.x, _panelOriginPos.y - 1200f);
            }

            if (txtTitle != null)
            {
                txtTitle.alpha = 0f;
                txtTitle.transform.localScale = Vector3.zero;
                txtTitle.transform.localPosition = _titleOrigPos + new Vector3(0f, 30f, 0f);
            }

            if (heartIconContainer != null)
            {
                heartIconContainer.localScale = Vector3.zero;
            }

            if (heartIcon != null)
            {
                heartIcon.localScale = Vector3.one;
                heartIcon.localRotation = Quaternion.identity;
                heartIcon.localPosition = Vector3.zero;
            }

            if (badgeContainer != null)
            {
                badgeContainer.localScale = Vector3.zero;
            }

            if (pricePanel != null)
            {
                pricePanel.localScale = Vector3.zero;
                pricePanel.localPosition = _pricePanelOrigPos + new Vector3(0f, -20f, 0f);
            }

            if (btnAddLives != null) btnAddLives.transform.localScale = Vector3.zero;
            if (btnTryAgain != null) btnTryAgain.transform.localScale = Vector3.zero;
        }

        protected override void PlayShowAnimation()
        {
            if (popupPanel == null) return;

            KillAllAnimations();

            Sequence showSeq = DOTween.Sequence();
            showSeq.SetUpdate(true).SetLink(gameObject);

            // 1. Slide up the main panel
            showSeq.Append(popupPanel.DOAnchorPosY(_panelOriginPos.y, animDuration).SetEase(Ease.OutCubic));
            showSeq.Join(popupPanel.DOScale(Vector3.one, animDuration).SetEase(Ease.OutCubic));

            float timeStep = 0.08f;

            // 2. Title Animation
            if (txtTitle != null)
            {
                showSeq.Insert(0.05f, txtTitle.DOFade(1f, animDuration * 0.5f));
                showSeq.Insert(0.05f, txtTitle.transform.DOScale(1f, animDuration * 0.5f).SetEase(Ease.OutBack));
                showSeq.Insert(0.05f, txtTitle.transform.DOLocalMoveY(_titleOrigPos.y, animDuration * 0.8f).SetEase(Ease.OutBounce));
            }

            // 3. Heart Container (Elastic Scale Up)
            if (heartIconContainer != null)
            {
                float iconTime = timeStep * 1.5f;
                showSeq.Insert(iconTime, heartIconContainer.DOScale(_heartContainerOrigScale, animDuration * 1.5f).SetEase(Ease.OutElastic));
            }

            // 4. Price Panel
            if (pricePanel != null)
            {
                float priceTime = timeStep * 2.5f;
                showSeq.Insert(priceTime, pricePanel.DOScale(Vector3.one, animDuration * 0.6f).SetEase(Ease.OutBack));
                showSeq.Insert(priceTime, pricePanel.DOLocalMoveY(_pricePanelOrigPos.y, animDuration * 0.6f).SetEase(Ease.OutCubic));
            }

            // 5. Badge (Scale pop shortly after container)
            if (badgeContainer != null)
            {
                float badgeTime = timeStep * 3f;
                showSeq.Insert(badgeTime, badgeContainer.DOScale(_badgeOrigScale * 1.2f, animDuration * 0.8f).SetEase(Ease.OutBack));
                showSeq.Insert(badgeTime + animDuration * 0.8f, badgeContainer.DOScale(_badgeOrigScale, animDuration * 0.3f));
            }

            // 6. Buttons
            if (btnAddLives != null)
            {
                showSeq.Insert(timeStep * 3.5f, btnAddLives.transform.DOScale(_btnBuyOrigScale, animDuration * 0.6f).SetEase(Ease.OutBack));
            }
            if (btnTryAgain != null)
            {
                showSeq.Insert(timeStep * 4.0f, btnTryAgain.transform.DOScale(_btnTryAgainOrigScale, animDuration * 0.6f).SetEase(Ease.OutBack));
            }

            showSeq.OnComplete(StartIdleAnimations);
        }

        private void StartIdleAnimations()
        {
            // Heartbeat Pulse
            if (heartIcon != null)
            {
                Sequence hbSeq = DOTween.Sequence();
                hbSeq.Append(heartIcon.DOScale(1.22f, 0.15f).SetEase(Ease.OutQuad))
                     .Append(heartIcon.DOScale(1.05f, 0.12f).SetEase(Ease.InQuad))
                     .Append(heartIcon.DOScale(1.15f, 0.12f).SetEase(Ease.OutQuad))
                     .Append(heartIcon.DOScale(1.0f, 0.25f).SetEase(Ease.InQuad))
                     .AppendInterval(0.6f)
                     .SetLoops(-1)
                     .SetUpdate(true)
                     .SetLink(heartIcon.gameObject);

                _heartBeatTween = hbSeq;

                _floatTween = heartIcon.DOLocalMoveY(12f, 1.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(heartIcon.gameObject);
            }

            // Pulse badge
            if (badgeContainer != null)
            {
                _badgePulseTween = badgeContainer.DOScale(_badgeOrigScale * 1.15f, 1.0f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(badgeContainer.gameObject);
            }

            // Pulse accept button size gently
            if (btnAddLives != null)
            {
                _btnBuyPulseTween = btnAddLives.transform.DOScale(_btnBuyOrigScale * 1.1f, 1.0f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(btnAddLives.gameObject);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillAllAnimations();

            if (popupPanel != null)
            {
                float hideDur = animDuration * 0.8f;
                popupPanel.DOScale(Vector3.one * 0.8f, hideDur)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject);

                popupPanel.DOAnchorPosY(_panelOriginPos.y - 1200f, hideDur)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() =>
                    {
                        popupPanel.anchoredPosition = _panelOriginPos;
                        onComplete?.Invoke();
                    });
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private void KillAllAnimations()
        {
            _heartBeatTween?.Kill();
            _floatTween?.Kill();
            _badgePulseTween?.Kill();
            _btnBuyPulseTween?.Kill();

            if (popupPanel != null) popupPanel.DOKill();
            if (txtTitle != null) txtTitle.transform.DOKill();
            if (heartIconContainer != null) heartIconContainer.DOKill();
            if (heartIcon != null) heartIcon.DOKill();
            if (badgeContainer != null) badgeContainer.DOKill();
            if (pricePanel != null) pricePanel.DOKill();
            if (btnAddLives != null) btnAddLives.transform.DOKill();
            if (btnTryAgain != null) btnTryAgain.transform.DOKill();
        }

        private void OnBuyLivesClicked()
        {
            if (DataManager.Instance == null) return;

            if (!DataManager.Instance.TrySpendCoin(_coinPrice))
            {
                UIManager.Instance?.ShowToast("NOT ENOUGH COINS!", 1.5f);
                
                // Shake button animation when coins are not enough
                if (btnAddLives != null)
                {
                    btnAddLives.transform.DOKill();
                    btnAddLives.transform.localScale = Vector3.one;
                    btnAddLives.transform
                        .DOPunchPosition(new Vector3(15f, 0f, 0f), 0.3f, 10, 1f)
                        .SetUpdate(true)
                        .SetLink(btnAddLives.gameObject, LinkBehaviour.KillOnDisable);
                }
                return;
            }

            _onBuySuccess?.Invoke();
            Hide();
        }

        private void OnTryAgainClicked()
        {
            _onTryAgain?.Invoke();
        }
    }
}
