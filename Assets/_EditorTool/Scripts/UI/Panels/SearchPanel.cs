using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using EditorTool.Scripts.EditorTool.System;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI.Panels
{
    public class SearchPanel : MonoBehaviour
    {
        [Header("UI References - Search & Load")]
        [SerializeField] private TMP_InputField _searchLevelInput;
        [SerializeField] private TMP_Dropdown _levelDropdown;
        [SerializeField] private Button _btnLoadLevel;
        [SerializeField] private Button _btnClearSearch;

        // Delegates / Actions
        public Func<bool> OnCheckUnsavedChanges;
        public Action<LevelSaveData> OnDataPreviewLoaded;
        public Action OnLoadButtonClicked;

        private List<string> _allSavedLevelFiles = new List<string>();
        private bool _isSelectingFromDropdown;
        private int _previousDropdownIndex;

        public string CurrentSearchText => _searchLevelInput != null ? _searchLevelInput.text : string.Empty;

        public void Initialize()
        {
            ScanSavedLevels();
            
            _searchLevelInput.text = LevelMakerManager.Instance.currentLevelID;
            
            // Clean up listeners before adding to avoid duplication
            _searchLevelInput.onValueChanged.RemoveAllListeners();
            _levelDropdown.onValueChanged.RemoveAllListeners();
            _btnLoadLevel.onClick.RemoveAllListeners();
            if (_btnClearSearch != null) _btnClearSearch.onClick.RemoveAllListeners();

            _searchLevelInput.onValueChanged.AddListener(OnSearchInputChanged);
            _levelDropdown.onValueChanged.AddListener(OnDropdownSelected);
            
            _btnLoadLevel.onClick.AddListener(() => 
            {
                // UI Feedback với DOTween
                _btnLoadLevel.transform.DOPunchScale(Vector3.one * -0.1f, 0.2f, 10, 1f);
                OnLoadButtonClicked?.Invoke();
            });

            if (_btnClearSearch != null)
            {
                _btnClearSearch.onClick.AddListener(() =>
                {
                    _searchLevelInput.text = string.Empty;
                    _searchLevelInput.Select();
                });
            }
        }

        public void ScanSavedLevels()
        {
            _allSavedLevelFiles = SaveLoadService.GetAllSavedLevels();
            UpdateDropdownOptions(_searchLevelInput != null ? _searchLevelInput.text : string.Empty);
        }

        public void SetSearchText(string text)
        {
            if (_searchLevelInput != null) _searchLevelInput.text = text;
        }

        private void OnSearchInputChanged(string keyword)
        {
            if (_isSelectingFromDropdown) return;
            LevelMakerManager.Instance.currentLevelID = keyword;
            UpdateDropdownOptions(keyword);
        }

        private void UpdateDropdownOptions(string keyword)
        {
            _levelDropdown.onValueChanged.RemoveListener(OnDropdownSelected);
            _levelDropdown.ClearOptions();

            List<string> options = new List<string> { "--- Tạo mới ---" };
            
            // Tối ưu GC: Dùng StringComparison thay vì ToLower()
            IEnumerable<string> filtered = string.IsNullOrEmpty(keyword)
                ? _allSavedLevelFiles
                : _allSavedLevelFiles.Where(x => x.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            options.AddRange(filtered);
            _levelDropdown.AddOptions(options);

            int currentIndex = options.IndexOf(LevelMakerManager.Instance.currentLevelID);
            _levelDropdown.SetValueWithoutNotify(currentIndex >= 0 ? currentIndex : 0);
            _levelDropdown.RefreshShownValue();

            _levelDropdown.onValueChanged.AddListener(OnDropdownSelected);
        }

        private void OnDropdownSelected(int index)
        {
            if (_isSelectingFromDropdown) return;

            if (OnCheckUnsavedChanges != null && !OnCheckUnsavedChanges())
            {
                _isSelectingFromDropdown = true;
                _levelDropdown.SetValueWithoutNotify(_previousDropdownIndex);
                _levelDropdown.RefreshShownValue();
                _isSelectingFromDropdown = false;
                return;
            }

            _previousDropdownIndex = index;
            string selectedText = _levelDropdown.options[index].text;
            _isSelectingFromDropdown = true;

            if (selectedText == "--- Tạo mới ---")
            {
                _searchLevelInput.text = "Level_";
                LevelMakerManager.Instance.currentLevelID = "Level_";
                _searchLevelInput.Select();
                _searchLevelInput.caretPosition = _searchLevelInput.text.Length;
                OnDataPreviewLoaded?.Invoke(null);
            }
            else
            {
                _searchLevelInput.text = selectedText;
                LevelMakerManager.Instance.currentLevelID = selectedText;
                LevelSaveData saveData = SaveLoadService.LoadLevelEditor(selectedText);
                OnDataPreviewLoaded?.Invoke(saveData);
            }

            _levelDropdown.SetValueWithoutNotify(index);
            _levelDropdown.RefreshShownValue();
            _isSelectingFromDropdown = false;
        }

        private void OnDestroy()
        {
            // Kill tween để tránh lỗi rò rỉ nếu object bị hủy giữa chừng
            _btnLoadLevel.transform.DOKill();
        }
    }
}