using System;
using ArrowGame.Data.Booster;
using ArrowGame.UI.Base;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Popups
{
    public class BoosterInstructionPopup : BasePopup
    {
        [Header("--- UI References ---")]
        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnConfirm;
        
        [Header("--- Texts ---")]
        [SerializeField] private TextMeshProUGUI txtTitle;        
        [SerializeField] private TextMeshProUGUI txtDescription;  
        [SerializeField] private TextMeshProUGUI txtInstruction;  
        
        [Header("--- Graphics ---")]
        [SerializeField] private Image imgIcon;
        [SerializeField] private RectTransform contentPanel;

        [Header("--- Animation Settings ---")]
        [SerializeField] private float popScale = 1.15f;     // Độ nảy lố của Icon
        [SerializeField] private float textOffset = 40f;     // Khoảng cách trượt của text

        // Cache Vị trí và Scale gốc
        private Vector3 _titleOrigPos, _descOrigPos, _instOrigPos;
        private Vector3 _iconOrigScale, _btnOrigScale;

        private Action _onConfirm, _onCancel;

        protected override void Awake()
        {
            base.Awake();
            
            // LƯU TRẠNG THÁI GỐC (Bắt buộc để kết hợp Scale + Trượt)
            if (txtTitle != null) _titleOrigPos = txtTitle.transform.localPosition;
            if (txtDescription != null) _descOrigPos = txtDescription.transform.localPosition;
            if (txtInstruction != null) _instOrigPos = txtInstruction.transform.localPosition;
            
            _iconOrigScale = imgIcon != null ? imgIcon.transform.localScale : Vector3.one;
            _btnOrigScale = btnConfirm != null ? btnConfirm.transform.localScale : Vector3.one;

            if (btnCancel != null)
            {
                btnCancel.onClick.RemoveAllListeners();
                btnCancel.onClick.AddListener(OnCancelClicked);
            }
            BindButton(btnConfirm, OnConfirmClicked);
        }

        public void Setup(BoosterConfigSO boosterConfig, bool showConfirmButton, Action onConfirm, Action onCancel)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            if (btnConfirm != null) btnConfirm.gameObject.SetActive(showConfirmButton);

            if (boosterConfig != null)
            {
                if (txtTitle != null) txtTitle.text = string.IsNullOrEmpty(boosterConfig.boosterName) ? "BOOSTER" : boosterConfig.boosterName.ToUpper();
                if (txtDescription != null) txtDescription.text = boosterConfig.description; 
                
                if (imgIcon != null && boosterConfig.boosterIcon != null)
                {
                    imgIcon.gameObject.SetActive(true);
                    imgIcon.sprite = boosterConfig.boosterIcon;
                }
            }

            if (txtInstruction != null)
            {
                txtInstruction.text = showConfirmButton
                    ? "Tap Launch to use booster\n<size=85%>Tap outside to cancel</size>"
                    : "Select a target to use booster\n<size=85%>Tap outside to cancel</size>"; 
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

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();

            if (contentPanel != null)
            {
                contentPanel.DOKill();
                contentPanel.localScale = Vector3.one;
                contentPanel.localRotation = Quaternion.identity; 
            }
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            // --- RESET TỪNG THÀNH PHẦN THEO TÍNH CÁCH RIÊNG ---
            
            // 1. Title: Tàng hình, chốt trên cao để chuẩn bị Rơi
            if (txtTitle != null) {
                txtTitle.transform.DOKill();
                txtTitle.alpha = 0f;
                txtTitle.transform.localScale = Vector3.zero;
                txtTitle.transform.localPosition = _titleOrigPos + new Vector3(0, textOffset, 0); 
            }

            // 2. Description: Tàng hình, nghiêng chéo để chuẩn bị Xoay nảy
            if (txtDescription != null) {
                txtDescription.transform.DOKill();
                txtDescription.alpha = 0f;
                txtDescription.transform.localScale = Vector3.zero;
                txtDescription.transform.localPosition = _descOrigPos;
                txtDescription.transform.localRotation = Quaternion.Euler(0, 0, -20f); 
            }

            // 3. Instruction (Chữ nhỏ): Mờ tịt, nằm dưới sâu để chuẩn bị Trượt êm
            if (txtInstruction != null) {
                txtInstruction.transform.DOKill();
                txtInstruction.alpha = 0f;
                txtInstruction.transform.localScale = Vector3.one; // Cố tình không scale để nó mượt
                txtInstruction.transform.localPosition = _instOrigPos + new Vector3(0, -textOffset, 0);
            }

            // 4. Icon & Button: Về 0
            if (imgIcon != null) { imgIcon.transform.DOKill(); imgIcon.transform.localScale = Vector3.zero; }
            if (btnConfirm != null) { btnConfirm.transform.DOKill(); btnConfirm.transform.localScale = Vector3.zero; }
        }

        protected override void PlayShowAnimation()
        {
            if (contentPanel == null) return;
            KillAllTweens();

            Sequence showSeq = DOTween.Sequence();
            showSeq.SetUpdate(true).SetLink(gameObject);

            // Tối nền
            if (canvasGroup != null) showSeq.Append(canvasGroup.DOFade(1f, animDuration * 0.3f));

            float timeStep = 0.12f; // Thời gian so le

            // --- 1. TITLE (Rơi phịch xuống và nảy tưng tưng) ---
            if (txtTitle != null) {
                showSeq.Insert(0f, txtTitle.DOFade(1f, animDuration * 0.4f));
                showSeq.Insert(0f, txtTitle.transform.DOScale(1f, animDuration * 0.5f).SetEase(Ease.OutBack));
                showSeq.Insert(0f, txtTitle.transform.DOLocalMoveY(_titleOrigPos.y, animDuration * 0.6f).SetEase(Ease.OutBounce)); // Hiệu ứng tưng quả bóng
            }

            // --- 2. DESCRIPTION (Lật ra nảy dứt khoát) ---
            if (txtDescription != null) {
                showSeq.Insert(timeStep, txtDescription.DOFade(1f, animDuration * 0.4f));
                showSeq.Insert(timeStep, txtDescription.transform.DOScale(1f, animDuration * 0.5f).SetEase(Ease.OutBack));
                showSeq.Insert(timeStep, txtDescription.transform.DORotate(Vector3.zero, animDuration * 0.5f).SetEase(Ease.OutBack));
            }

            // --- 3. ICON (Bùng nổ to nhất) ---
            if (imgIcon != null) {
                float iconTime = timeStep * 2;
                showSeq.Insert(iconTime, imgIcon.transform.DOScale(_iconOrigScale * popScale, animDuration * 0.5f).SetEase(Ease.OutBack));
                showSeq.Insert(iconTime + (animDuration * 0.4f), imgIcon.transform.DOScale(_iconOrigScale, animDuration * 0.2f));
                
                showSeq.OnComplete(() => {
                    imgIcon.transform.DOScale(_iconOrigScale * 1.05f, 1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
                });
            }

            // --- 4. INSTRUCTION (Trượt lên nhẹ nhàng, êm ái, không giật cục) ---
            if (txtInstruction != null) {
                float instTime = timeStep * 3;
                showSeq.Insert(instTime, txtInstruction.DOFade(1f, animDuration * 0.5f));
                showSeq.Insert(instTime, txtInstruction.transform.DOLocalMoveY(_instOrigPos.y, animDuration * 0.5f).SetEase(Ease.OutCubic)); // OutCubic là trượt thắng cực êm
            }

            // --- 5. BUTTON (Pop nảy nhẹ mời gọi) ---
            if (btnConfirm != null) {
                showSeq.Insert(timeStep * 4, btnConfirm.transform.DOScale(_btnOrigScale, animDuration * 0.5f).SetEase(Ease.OutBack));
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            if (contentPanel == null) { onComplete?.Invoke(); return; }
            KillAllTweens();

            Sequence hideSeq = DOTween.Sequence();
            hideSeq.SetUpdate(true).SetLink(gameObject);

            hideSeq.Append(contentPanel.DOScale(0.5f, animDuration * 0.5f).SetEase(Ease.InBack));
            if (canvasGroup != null) hideSeq.Join(canvasGroup.DOFade(0f, animDuration * 0.5f));
            hideSeq.OnComplete(() => onComplete?.Invoke());
        }

        private void KillAllTweens()
        {
            contentPanel.DOKill();
            if (imgIcon != null) imgIcon.transform.DOKill();
            if (btnConfirm != null) btnConfirm.transform.DOKill();
            if (txtTitle != null) txtTitle.transform.DOKill();
            if (txtDescription != null) txtDescription.transform.DOKill();
            if (txtInstruction != null) txtInstruction.transform.DOKill();
        }
    }
}