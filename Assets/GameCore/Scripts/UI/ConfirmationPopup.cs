using System;
using System.Collections;
using DG.Tweening;
using GameCore.UI.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class ConfirmationPopup: BasePopup
    {
        [Header("Components")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        
        [SerializeField] private TextMeshProUGUI confirmLabelText; 
        [SerializeField] private TextMeshProUGUI cancelLabelText;
        
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backgroundButton;
        
        [Header("--- Animation ---")]
        [SerializeField] private Transform panelContent;
        
        [Header("--- Layout Fix ---")]
        [SerializeField] private RectTransform mainContentRect;
        
        private Action _onConfirmAction;
        private Action _onCancelAction;

        private void Start()
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            cancelButton.onClick.AddListener(OnCancelClicked);
            if(backgroundButton) backgroundButton.onClick.AddListener(OnCancelClicked);
        }

        public void Setup(string title, string message, Action onConfirm, Action onCancel = null, string confirmLabel = null, string cancelLabel = null)
        {
            if(titleText) titleText.text = title;
            if(messageText) messageText.text = message;

            _onConfirmAction = onConfirm;
            _onCancelAction = onCancel;
            
            if (confirmLabelText != null && !string.IsNullOrEmpty(confirmLabel))
            {
                confirmLabelText.text = confirmLabel;
            }

            if (cancelLabelText != null && !string.IsNullOrEmpty(cancelLabel))
            {
                cancelLabelText.text = cancelLabel;
            }
        }

        public override void Show()
        {
            base.Show();

            StartCoroutine(RebuildLayoutRoutine());
        }

        private IEnumerator RebuildLayoutRoutine()
        {
            yield return new WaitForEndOfFrame();
            
            if (mainContentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainContentRect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainContentRect); //2 lần cho chắc cú vì cả cha và con đều có auto layout
            }
        }
        private void OnConfirmClicked()
        {
            _onConfirmAction?.Invoke();
            Hide();
        }

        private void OnCancelClicked()
        {
            _onCancelAction?.Invoke();
            Hide();
        }

        // --- ANIMATION (Override BasePopup) ---
        // Popup này sẽ phóng to từ giữa màn hình (Pop Up)
        protected override void PlayShowAnimation()
        {
            if (panelContent == null) return;
            panelContent.localScale = Vector3.zero;
            panelContent.DOScale(1f, animDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            if (panelContent == null) { onComplete?.Invoke(); return; }
            panelContent.DOScale(0f, animDuration).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
    
}