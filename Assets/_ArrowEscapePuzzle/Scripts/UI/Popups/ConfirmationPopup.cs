using System;
using ArrowGame.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ArrowGame.UI.Popups
{
    public class ConfirmationPopup : BasePopup 
    {
        [Header("--- UI References ---")]
        [SerializeField] private Transform panelContainer;
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtMessage;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private Button btnCancel;

        private Action _onConfirm;
        private Action _onCancel;

        protected override void Awake()
        {
            base.Awake();
            BindButton(btnConfirm, OnConfirmClicked);
            BindButton(btnCancel, OnCancelClicked);
        }

        private void BindButton(Button btn, Action onClickAction)
        {
            if (btn == null) return;

            btn.onClick.AddListener(() =>
            {
                btn.interactable = false;

                // Tối ưu mục 3: Thêm SetLink để an toàn vòng đời, tránh MissingReferenceException
                btn.transform.DOScale(0.9f, 0.1f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetUpdate(true)
                    .SetLink(btn.gameObject) 
                    .OnComplete(() => 
                    {
                        btn.interactable = true;
                        onClickAction?.Invoke();
                    });
            });
        }

        public void Setup(string title, string message, Action onConfirm, Action onCancel = null, Sprite iconSprite = null)
        {
            txtTitle.text = title;
            txtMessage.text = message;
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            if (imgIcon != null)
            {
                imgIcon.gameObject.SetActive(iconSprite != null);
                if (iconSprite != null) imgIcon.sprite = iconSprite;
            }

            if (btnCancel != null)
            {
                btnCancel.gameObject.SetActive(onCancel != null); 
            }
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