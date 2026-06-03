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
        [SerializeField] private RectTransform panelContainer;
        [SerializeField] private TextMeshProUGUI txtBoosterName;
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtPrice;
        [SerializeField] private TextMeshProUGUI txtDescription;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;

        private BoosterConfigSO _currentConfig;
        private Action _onBuySuccess;

        // Cache positions and scales
        private Vector2 _panelOriginPos;
        private bool _hasSavedOriginPos;
        private Vector3 _titleOrigPos;
        private Vector3 _descOrigPos;
        private Vector3 _iconOrigPos;
        private Vector3 _iconOrigScale;
        private Vector3 _btnConfirmOrigScale;
        private Vector3 _btnCancelOrigScale;

        // Tween references
        private Tween _floatTween;
        private Tween _btnConfirmPulseTween;

        protected override void Awake()
        {
            base.Awake();
            BindButton(btnConfirm, OnConfirmClicked);
            BindButton(btnCancel, Hide); 

            if (panelContainer != null)
            {
                _panelOriginPos = panelContainer.anchoredPosition;
                _hasSavedOriginPos = true;
            }

            if (txtBoosterName != null) _titleOrigPos = txtBoosterName.transform.localPosition;
            if (txtDescription != null) _descOrigPos = txtDescription.transform.localPosition;

            if (imgIcon != null) 
            {
                _iconOrigPos = imgIcon.transform.localPosition;
                _iconOrigScale = imgIcon.transform.localScale;
            }

            _btnConfirmOrigScale = btnConfirm != null ? btnConfirm.transform.localScale : Vector3.one;
            _btnCancelOrigScale = btnCancel != null ? btnCancel.transform.localScale : Vector3.one;
        }

        public void Setup(BoosterConfigSO config, Action onBuySuccess)
        {
            _currentConfig = config;
            _onBuySuccess = onBuySuccess;

            if (txtBoosterName != null) txtBoosterName.text = config.boosterName.ToUpper();
            if (txtDescription != null) txtDescription.text = config.description;
            if (txtPrice != null) txtPrice.text = config.price.ToString();

            if (imgIcon != null && config.boosterIcon != null)
            {
                imgIcon.sprite = config.boosterIcon;
            }

            bool canAfford = DataManager.Instance.GetCurrentCoin() >= config.price;
            if (txtPrice != null) txtPrice.color = canAfford ? Color.white : Color.red;
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

            if (txtBoosterName != null)
            {
                txtBoosterName.alpha = 0f;
                txtBoosterName.transform.localScale = Vector3.zero;
                txtBoosterName.transform.localPosition = _titleOrigPos + new Vector3(0f, 30f, 0f);
            }

            if (txtDescription != null)
            {
                txtDescription.alpha = 0f;
                txtDescription.transform.localPosition = _descOrigPos + new Vector3(0f, -20f, 0f);
            }

            if (imgIcon != null)
            {
                imgIcon.transform.localScale = Vector3.zero;
                imgIcon.transform.localRotation = Quaternion.identity;
                imgIcon.transform.localPosition = _iconOrigPos;
            }

            if (txtPrice != null)
            {
                txtPrice.transform.localScale = Vector3.zero;
            }

            if (btnConfirm != null) btnConfirm.transform.localScale = Vector3.zero;
            if (btnCancel != null) btnCancel.transform.localScale = Vector3.zero;
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
                if (txtPrice != null)
                {
                    txtPrice.transform.DOKill();
                    txtPrice.transform.localScale = Vector3.one;
                    txtPrice.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 10, 1f).SetUpdate(true);
                }
                
                UIManager.Instance.ShowToast("NOT ENOUGH COINS!", 1.5f);
            }
        }

        protected override void PlayShowAnimation()
        {
            if (panelContainer == null) return;

            KillAllAnimations();

            Sequence showSeq = DOTween.Sequence();
            showSeq.SetUpdate(true).SetLink(gameObject);

            float panelDur = animDuration * 1.2f;
            showSeq.Append(panelContainer.DOAnchorPosY(_panelOriginPos.y, panelDur).SetEase(Ease.OutCubic));
            showSeq.Join(panelContainer.DOScale(Vector3.one, panelDur).SetEase(Ease.OutCubic));

            float timeStep = 0.08f;

            if (txtBoosterName != null)
            {
                showSeq.Insert(0.05f, txtBoosterName.DOFade(1f, animDuration * 0.5f));
                showSeq.Insert(0.05f, txtBoosterName.transform.DOScale(1f, animDuration * 0.5f).SetEase(Ease.OutBack));
                showSeq.Insert(0.05f, txtBoosterName.transform.DOLocalMoveY(_titleOrigPos.y, animDuration * 0.8f).SetEase(Ease.OutBounce));
            }

            if (imgIcon != null)
            {
                float iconTime = timeStep * 1.5f;
                showSeq.Insert(iconTime, imgIcon.transform.DOScale(_iconOrigScale, animDuration * 1.5f).SetEase(Ease.OutElastic));
            }

            if (txtPrice != null)
            {
                float priceTime = timeStep * 2.5f;
                showSeq.Insert(priceTime, txtPrice.transform.DOScale(Vector3.one, animDuration * 0.6f).SetEase(Ease.OutBack));
            }

            if (txtDescription != null)
            {
                float descTime = timeStep * 3f;
                showSeq.Insert(descTime, txtDescription.DOFade(1f, animDuration * 0.6f));
                showSeq.Insert(descTime, txtDescription.transform.DOLocalMoveY(_descOrigPos.y, animDuration * 0.6f).SetEase(Ease.OutCubic));
            }

            if (btnConfirm != null)
            {
                showSeq.Insert(timeStep * 3.5f, btnConfirm.transform.DOScale(_btnConfirmOrigScale, animDuration * 0.6f).SetEase(Ease.OutBack));
            }
            if (btnCancel != null)
            {
                showSeq.Insert(timeStep * 4.0f, btnCancel.transform.DOScale(_btnCancelOrigScale, animDuration * 0.6f).SetEase(Ease.OutBack));
            }

            showSeq.OnComplete(StartIdleAnimations);
        }

        private void StartIdleAnimations()
        {
            if (imgIcon != null)
            {
                _floatTween = imgIcon.transform.DOLocalMoveY(_iconOrigPos.y + 12f, 1.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(imgIcon.gameObject);
            }

            if (btnConfirm != null)
            {
                _btnConfirmPulseTween = btnConfirm.transform.DOScale(_btnConfirmOrigScale * 1.1f, 1.0f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(btnConfirm.gameObject);
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            KillAllAnimations();

            if (panelContainer != null)
            {
                float hideDur = animDuration * 0.8f;
                panelContainer.DOScale(Vector3.one * 0.8f, hideDur)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject);

                panelContainer.DOAnchorPosY(_panelOriginPos.y - 1200f, hideDur)
                    .SetEase(Ease.InCubic)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() =>
                    {
                        panelContainer.anchoredPosition = _panelOriginPos;
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
            _floatTween?.Kill();
            _btnConfirmPulseTween?.Kill();

            if (panelContainer != null) panelContainer.DOKill();
            if (txtBoosterName != null) txtBoosterName.transform.DOKill();
            if (imgIcon != null) imgIcon.transform.DOKill();
            if (txtPrice != null) txtPrice.transform.DOKill();
            if (txtDescription != null) txtDescription.transform.DOKill();
            if (btnConfirm != null) btnConfirm.transform.DOKill();
            if (btnCancel != null) btnCancel.transform.DOKill();
        }
    }
}