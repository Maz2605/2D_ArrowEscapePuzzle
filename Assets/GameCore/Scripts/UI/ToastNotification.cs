using DG.Tweening;
using GameCore.UI.Base;
using TMPro;
using UnityEngine;

namespace GameCore.UI
{
    public class ToastNotification : BasePopup
    {
        [Header("--- UI References ---")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private RectTransform contentPanel; 
        
        [Header("--- Animation Config ---")]
        [SerializeField] private float defaultDuration = 2f; 
        [SerializeField] private float slideDistance = 200f; 
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;  

        private Sequence _toastSequence;
        private Vector2 _hiddenPos; 
        private Vector2 _shownPos;  

        protected override void Awake()
        {
            base.Awake();
            _hiddenPos = new Vector2(0, contentPanel.rect.height + 50f); 
            _shownPos = new Vector2(0, -slideDistance);
        }

        protected override void PlayShowAnimation() { } 
        protected override void PlayHideAnimation(System.Action onComplete) { onComplete?.Invoke(); }

        // --- HÀM SHOW CUSTOM CHO TOAST ---
        
        /// <summary>
        /// Hiện thông báo Toast
        /// </summary>
        /// <param name="message">Nội dung</param>
        /// <param name="duration">Thời gian hiện (gửi -1 để dùng mặc định)</param>
        public void ShowToast(string message, float duration = -1f)
        {
            // 0. Setup dữ liệu
            if (messageText) messageText.text = message;
            float showTime = (duration < 0) ? defaultDuration : duration;

            gameObject.SetActive(true);

            _toastSequence?.Kill();
            _toastSequence = DOTween.Sequence();

            if (contentPanel) contentPanel.anchoredPosition = _hiddenPos;
            if (canvasGroup) canvasGroup.alpha = 0f;

            _toastSequence.Append(contentPanel.DOAnchorPos(_shownPos, animDuration).SetEase(showEase));
            _toastSequence.Join(canvasGroup.DOFade(1f, animDuration * 0.8f));

            _toastSequence.AppendInterval(showTime);

            _toastSequence.Append(contentPanel.DOAnchorPos(_hiddenPos, animDuration).SetEase(hideEase));
            _toastSequence.Join(canvasGroup.DOFade(0f, animDuration * 0.8f));

            _toastSequence.OnComplete(() => 
            {
                gameObject.SetActive(false);
            });
            
            _toastSequence.SetUpdate(true); 
        }
        
        public void ForceHide()
        {
            _toastSequence?.Kill();
            gameObject.SetActive(false);
        }
    }
}