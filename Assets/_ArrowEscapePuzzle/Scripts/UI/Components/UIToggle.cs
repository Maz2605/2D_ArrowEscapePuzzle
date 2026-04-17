using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Components
{
    [RequireComponent(typeof(Button))] 
    public class UIToggle : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private RectTransform handleTransform;
        [SerializeField] private Image backgroundImage;

        [Header("--- Settings ---")]
        [Tooltip("Khoảng cách thụt lề của cục tròn so với viền")]
        [SerializeField] private float padding = 5f;
        [SerializeField] private float animDuration = 0.2f;
        [SerializeField] private Color colorOn = new Color(0.35f, 0.45f, 0.95f);
        [SerializeField] private Color colorOff = new Color(0.3f, 0.3f, 0.35f);

        public bool IsOn { get; private set; }
        public Action<bool> OnValueChanged;

        private Button _button;
        private Tween _moveTween;
        private Tween _colorTween;
        private RectTransform _bgRect;
        
        // Cờ dùng để chờ Layout Group dựng xong UI
        private bool _isPendingVisualUpdate = false;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);

            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
            _bgRect = backgroundImage.GetComponent<RectTransform>();
            
            // Tự động ép pivot và anchor về giữa để công thức tính toán luôn đúng
            // Tránh việc bạn lỡ tay kéo sai Anchor trên Inspector làm loạn UI
            _bgRect.pivot = new Vector2(0.5f, 0.5f);
            handleTransform.pivot = new Vector2(0.5f, 0.5f);
            handleTransform.anchorMin = new Vector2(0.5f, 0.5f);
            handleTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }

        public void InitState(bool isOn)
        {
            IsOn = isOn;
            
            // Nếu UI chưa kịp dựng (chiều rộng = 0 do Layout Group đang xử lý)
            if (_bgRect.rect.width <= 0.1f)
            {
                _isPendingVisualUpdate = true; // Đưa vào hàng chờ
            }
            else
            {
                SetVisuals(IsOn, false);
            }
        }

        private void Update()
        {
            // Tự động set lại vị trí ngay khi Layout Group vừa chia xong kích thước
            if (_isPendingVisualUpdate && _bgRect.rect.width > 0.1f)
            {
                _isPendingVisualUpdate = false;
                SetVisuals(IsOn, false);
            }
        }

        private void OnClick()
        {
            IsOn = !IsOn;
            
            _moveTween?.Kill();
            _colorTween?.Kill();
            
            SetVisuals(IsOn, true);
            OnValueChanged?.Invoke(IsOn);
        }

        private float CalculateTargetX(bool state)
        {
            // Tự động tính quãng đường dựa trên kích thước thật của Background và Handle
            float bgWidth = _bgRect.rect.width;
            float handleWidth = handleTransform.rect.width;
            
            float maxOffset = (bgWidth - handleWidth) / 2f - padding;
            return state ? maxOffset : -maxOffset;
        }

        private void SetVisuals(bool state, bool playAnim)
        {
            float targetX = CalculateTargetX(state);
            Color targetColor = state ? colorOn : colorOff;

            if (playAnim)
            {
                _moveTween = handleTransform.DOAnchorPosX(targetX, animDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true) // Vẫn đảm bảo chạy mượt khi timeScale = 0
                    .SetLink(gameObject);

                _colorTween = backgroundImage.DOColor(targetColor, animDuration)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
            else
            {
                Vector2 pos = handleTransform.anchoredPosition;
                pos.x = targetX;
                handleTransform.anchoredPosition = pos;
                backgroundImage.color = targetColor;
            }
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnClick);
            _moveTween?.Kill();
            _colorTween?.Kill();
            OnValueChanged = null;
        }
    }
}