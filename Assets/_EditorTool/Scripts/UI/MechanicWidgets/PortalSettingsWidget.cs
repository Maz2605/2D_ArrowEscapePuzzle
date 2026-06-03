using System;
using System.Collections.Generic;
using ShareCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI.MechanicWidgets
{
    public class PortalSettingsWidget : MonoBehaviour
    {
        [Header("Portal Inputs")]
        [SerializeField] private TMP_InputField _portalIdInput;
        
        [Header("Portal Direction")]
        [SerializeField] private Button _upBtn;
        [SerializeField] private Button _rightBtn;
        [SerializeField] private Button _downBtn;
        [SerializeField] private Button _leftBtn;

        public Action<string> OnPortalIdChanged;
        public Action<Direction4> OnDirectionChanged;

        private readonly Dictionary<Direction4, Button> _dirButtons = new Dictionary<Direction4, Button>();

        public void Initialize()
        {
            _dirButtons[Direction4.Up] = _upBtn;
            _dirButtons[Direction4.Right] = _rightBtn;
            _dirButtons[Direction4.Down] = _downBtn;
            _dirButtons[Direction4.Left] = _leftBtn;

            if (_portalIdInput != null) _portalIdInput.readOnly = true;

            BindEvents();
        }

        private void BindEvents()
        {
            if (_portalIdInput != null)
            {
                _portalIdInput.onEndEdit.RemoveAllListeners();
                _portalIdInput.onEndEdit.AddListener(val => OnPortalIdChanged?.Invoke(NormalizePortalId(val)));
            }

            foreach (var kvp in _dirButtons)
            {
                Direction4 dir = kvp.Key;
                kvp.Value.onClick.RemoveAllListeners();
                kvp.Value.onClick.AddListener(() => OnDirectionChanged?.Invoke(dir));
            }
        }

        public void Refresh(string portalId, Direction4 currentDir)
        {
            string normalized = NormalizePortalId(portalId);
            if (_portalIdInput != null && _portalIdInput.text != normalized)
            {
                _portalIdInput.SetTextWithoutNotify(normalized);
            }

            foreach (var pair in _dirButtons)
            {
                SetButtonVisual(pair.Value, pair.Key == currentDir);
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private static string NormalizePortalId(string value) => string.IsNullOrWhiteSpace(value) ? "A" : value.Trim().ToUpperInvariant();

        private static void SetButtonVisual(Button btn, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = isActive ? new Color(0.22f, 0.66f, 0.95f, 1f) : new Color(0.16f, 0.2f, 0.27f, 1f);
        }
    }
}