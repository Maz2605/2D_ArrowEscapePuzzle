using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI
{
    /// <summary>
    /// UI item trong danh sách mũi tên.
    /// Khi bấm Select → invoke callback được truyền vào từ DrawingToolPanel.
    /// Không biết gì về EditorController hay EventManager.
    /// </summary>
    public class UIArrowListItem : MonoBehaviour
    {
        [SerializeField] private Image colorImage;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private Button selectButton;

        private string _arrowID;
        private Action<string> _onSelected;

        public void Setup(string id, Color color, int length, Action<string> onSelected)
        {
            _arrowID    = id;
            _onSelected = onSelected;

            colorImage.color = color;
            infoText.text    = $"Mũi tên số {id} (Dài: {length} ô)";

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onSelected?.Invoke(_arrowID));
        }
    }
}