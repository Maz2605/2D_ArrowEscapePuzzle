using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI
{
    public class UIMechanicListItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _colorImage;
        [SerializeField] private TextMeshProUGUI _infoText;
        [SerializeField] private Button _selectButton;

        private string _rawID;
        private Action<string> _onSelected;

        public void Setup(string displayName, string rawId, Color color, Action<string> onSelected)
        {
            _rawID = rawId;
            _onSelected = onSelected;

            if (_colorImage != null) _colorImage.color = color;
            if (_infoText != null) _infoText.text = displayName;

            if (_selectButton != null)
            {
                _selectButton.onClick.RemoveAllListeners();
                _selectButton.onClick.AddListener(OnItemClicked);
            }
        }

        private void OnItemClicked()
        {
            if (_selectButton != null)
            {
                _selectButton.transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0f), 0.15f)
                    .OnComplete(() => _onSelected?.Invoke(_rawID));
            }
        }

        private void OnDisable()
        {
            if (_selectButton != null)
            {
                _selectButton.transform.DOKill();
            }
        }
    }
}