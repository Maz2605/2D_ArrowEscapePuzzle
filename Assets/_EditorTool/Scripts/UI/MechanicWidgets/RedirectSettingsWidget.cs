using System;
using System.Collections.Generic;
using ShareCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI.MechanicWidgets
{
    public class RedirectSettingsWidget : MonoBehaviour
    {
        [Header("Redirect Settings")]
        [SerializeField] private TMP_InputField _idInput; // Bổ sung Input ID
        
        [Header("Redirect Direction")]
        [SerializeField] private Button _upBtn;
        [SerializeField] private Button _rightBtn;
        [SerializeField] private Button _downBtn;
        [SerializeField] private Button _leftBtn;

        public Action<string> OnIdChanged;
        public Action<Direction4> OnDirectionChanged;

        private readonly Dictionary<Direction4, Button> _dirButtons = new Dictionary<Direction4, Button>();

        public void Initialize()
        {
            _dirButtons[Direction4.Up] = _upBtn;
            _dirButtons[Direction4.Right] = _rightBtn;
            _dirButtons[Direction4.Down] = _downBtn;
            _dirButtons[Direction4.Left] = _leftBtn;

            if (_idInput != null) _idInput.readOnly = true;

            BindEvents();
        }

        private void BindEvents()
        {
            if (_idInput != null)
            {
                _idInput.onEndEdit.RemoveAllListeners();
                _idInput.onEndEdit.AddListener(val => OnIdChanged?.Invoke(NormalizeId(val)));
            }

            foreach (var kvp in _dirButtons)
            {
                Direction4 dir = kvp.Key;
                kvp.Value.onClick.RemoveAllListeners();
                kvp.Value.onClick.AddListener(() => OnDirectionChanged?.Invoke(dir));
            }
        }

        public void Refresh(string redirectId, Direction4 currentDir)
        {
            string normalized = NormalizeId(redirectId);
            if (_idInput != null && _idInput.text != normalized)
            {
                _idInput.SetTextWithoutNotify(normalized);
            }

            foreach (var pair in _dirButtons)
            {
                SetButtonVisual(pair.Value, pair.Key == currentDir);
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private static string NormalizeId(string value) => string.IsNullOrWhiteSpace(value) ? "A" : value.Trim().ToUpperInvariant();

        private static void SetButtonVisual(Button btn, bool isActive)
        {
            if (btn == null) return;
            if (btn.TryGetComponent(out Image img)) 
            {
                img.color = isActive ? new Color(0.22f, 0.66f, 0.95f, 1f) : new Color(0.16f, 0.2f, 0.27f, 1f);
            }
        }
    }
}