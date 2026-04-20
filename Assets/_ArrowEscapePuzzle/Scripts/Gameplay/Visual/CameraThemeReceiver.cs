using UnityEngine;
using DG.Tweening;
using ArrowGame.Data.Events;
using GameCore.Utils.DesignPattern.Events;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Managers;

namespace ArrowGame.Gameplay.Visual
{
    [RequireComponent(typeof(Camera))] 
    public class CameraThemeReceiver : MonoBehaviour
    {
        [Tooltip("Thời gian chuyển màu (giây)")]
        public float transitionDuration = 0.4f;

        private Camera _camera;
        private Tween _colorTween;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            // Đăng ký nghe loa thông báo
            EventManager<VisualEventID>.AddListener<ThemeConfigSO>(VisualEventID.ThemeChanged, OnThemeChanged);

            // Bắt màu ngay lập tức khi vừa bật lên (chống chớp màu cũ)
            if (ThemeManager.Instance != null && ThemeManager.Instance.CurrentTheme != null)
            {
                ApplyColor(ThemeManager.Instance.CurrentTheme, 0f);
            }
        }

        private void OnDisable()
        {
            // Hủy đăng ký nghe
            EventManager<VisualEventID>.RemoveListener<ThemeConfigSO>(VisualEventID.ThemeChanged, OnThemeChanged);
            _colorTween?.Kill(); // Dọn dẹp tween nếu object bị tắt
        }

        private void OnThemeChanged(ThemeConfigSO newTheme)
        {
            ApplyColor(newTheme, transitionDuration);
        }

        private void ApplyColor(ThemeConfigSO theme, float duration)
        {
            if (theme == null || _camera == null) return;

            // Lấy thẳng màu Global Background từ SO
            Color targetColor = theme.backgroundColor;

            _colorTween?.Kill();

            if (duration <= 0f)
            {
                _camera.backgroundColor = targetColor;
            }
            else
            {
                // Dùng DOColor của DOTween để chuyển màu mượt mà
                _colorTween = _camera.DOColor(targetColor, duration).SetId(this).SetLink(gameObject);
            }
        }
    }
}