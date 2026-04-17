using ArrowGame.Data.Events;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Managers;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.Gameplay.Visual
{
    public class VisualThemeReceiver : MonoBehaviour
    {
        public ThemeColorType colorType;
        public float transitionDuration = 0.4f;

        private Graphic _uiGraphic;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _uiGraphic = GetComponent<Graphic>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            EventManager<VisualEventID>.AddListener<ThemeConfigSO>(VisualEventID.ThemeChanged, OnThemeChanged);
            if (ThemeManager.Instance != null)
                ApplyColor(ThemeManager.Instance.CurrentTheme, 0f); 
        }

        private void OnDisable()
        {
            EventManager<VisualEventID>.RemoveListener<ThemeConfigSO>(VisualEventID.ThemeChanged, OnThemeChanged);
            _uiGraphic?.DOKill();
            _spriteRenderer?.DOKill();
        }

        private void OnThemeChanged(ThemeConfigSO newTheme) => ApplyColor(newTheme, transitionDuration);

        private void ApplyColor(ThemeConfigSO theme, float duration)
        {
            if (theme == null) return;
            Color targetColor = theme.GetColorByType(colorType);

            if (duration <= 0f)
            {
                if (_uiGraphic != null) _uiGraphic.color = targetColor;
                if (_spriteRenderer != null) _spriteRenderer.color = targetColor;
            }
            else
            {
                if (_uiGraphic != null) _uiGraphic.DOColor(targetColor, duration)
                    // .SetUpdate(true)
                    .SetLink(gameObject);
                if (_spriteRenderer != null) _spriteRenderer.DOColor(targetColor, duration)
                    // .SetUpdate(true)
                    .SetLink(gameObject);
            }
        }
    }
}