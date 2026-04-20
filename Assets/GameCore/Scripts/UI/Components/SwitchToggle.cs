using DG.Tweening;
using GameCore.Vibration;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameCore.UI.Components
{
    public class SwitchToggle : MonoBehaviour
    {
        [Header("Configuration")] [SerializeField]
        private float animDuration = 0.3f;

        [SerializeField] private Ease motionEase = Ease.OutBack;
        [SerializeField] private Vector3 punchScale = new Vector3(0.15f, 0.15f, 0);

        [Header("Handle (Cục tròn)")] [SerializeField]
        private RectTransform handleRect;

        [SerializeField] private Image handleImage;
        [SerializeField] private Color handleColorOff = Color.white;
        [SerializeField] private Color handleColorOn = Color.white;

        [Header("Background (Nền)")] [SerializeField]
        private Image backgroundImage;

        [SerializeField] private Color backColorOff = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private Color backColorOn = new Color(0.3f, 0.8f, 0.3f);

        [Header("Positions")] [SerializeField] private float handleXPosOff;
        [SerializeField] private float handleXPosOn;

        [Header("Events")] public UnityEvent<bool> onValueChanged;

        private bool _isOn = false;
        private Button _btn;
        private Sequence _seq;
        
        private Vector3 _currentHandleScale;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(ToggleState);
            _currentHandleScale = handleRect.localScale;

            UpdateVisual(false);
        }

        private void OnDestroy()
        {
            _seq?.Kill();
        }

        private void ToggleState()
        {
            _isOn = !_isOn;
            UpdateVisual(true);
            onValueChanged?.Invoke(_isOn);
            VibrationManager.Instance.HapticLight();
        }

        public void ForceSetState(bool isOn)
        {
            _isOn = isOn;
            UpdateVisual(false);
        }

        private void UpdateVisual(bool animate)
        {
            float targetX = _isOn ? handleXPosOn : handleXPosOff;
            Color targetBackColor = _isOn ? backColorOn : backColorOff;
            Color targetHandleColor = _isOn ? handleColorOn : handleColorOff;

            _seq?.Kill();

            if (animate)
            {
                _seq = DOTween.Sequence();
                _seq.SetUpdate(true);
                _seq.Join(handleRect.DOAnchorPosX(targetX, animDuration).SetEase(motionEase));
                _seq.Join(handleRect.DOPunchScale(punchScale, animDuration, 5, 0.5f));

                if (backgroundImage != null)
                    _seq.Join(backgroundImage.DOColor(targetBackColor, animDuration));

                if (handleImage != null)
                    _seq.Join(handleImage.DOColor(targetHandleColor, animDuration));

                _seq.Play();
            }
            else
            {
                handleRect.anchoredPosition = new Vector2(targetX, handleRect.anchoredPosition.y);

                if (backgroundImage != null)
                    backgroundImage.color = targetBackColor;
                if (handleImage != null)
                    handleImage.color = targetHandleColor;
            }
            handleRect.localScale = _currentHandleScale;
        }
    }
}