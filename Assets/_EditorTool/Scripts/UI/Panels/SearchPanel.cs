using System;
using System.Collections.Generic;
using System.Linq;
using EditorTool.Scripts.EditorTool.System;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI.Panels
{
    /// <summary>
    /// Quản lý toàn bộ UI Search, Dropdown và Load level.
    /// Giao tiếp với EditorUIManager qua callbacks, không phụ thuộc panel khác.
    /// </summary>
    public class SearchPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField searchLevelInput;
        [SerializeField] private TMP_Dropdown levelDropdown;
        [SerializeField] private Button btnLoadLevel;
        [SerializeField] private Button btnClearSearch;

        private List<string> _allSavedLevelFiles = new List<string>();
        private bool _isSelectingFromDropdown;
        private int _previousDropdownIndex;

        // === Callbacks — được EditorController gán trước Initialize() ===
        public Func<bool> OnCheckUnsavedChanges;
        public Action<LevelSaveData> OnDataPreviewLoaded;
        public Action OnLoadButtonClicked;

        // Private backing fields (copy từ public sau khi Initialize)
        private Func<bool> _checkUnsavedChanges;
        private Action<LevelSaveData> _onDataPreviewLoaded;
        private Action _onLoadButtonClicked;

        public string CurrentSearchText => searchLevelInput != null ? searchLevelInput.text : string.Empty;

        public void Initialize()
        {
            _checkUnsavedChanges = OnCheckUnsavedChanges;
            _onDataPreviewLoaded  = OnDataPreviewLoaded;
            _onLoadButtonClicked  = OnLoadButtonClicked;

            ScanSavedLevels();
            searchLevelInput.text = LevelMakerManager.Instance.currentLevelID;
            searchLevelInput.onValueChanged.AddListener(OnSearchInputChanged);
            levelDropdown.onValueChanged.AddListener(OnDropdownSelected);
            btnLoadLevel.onClick.AddListener(() => _onLoadButtonClicked?.Invoke());

            if (btnClearSearch != null)
                btnClearSearch.onClick.AddListener(() =>
                {
                    searchLevelInput.text = string.Empty;
                    searchLevelInput.Select();
                });
        }

        public void ScanSavedLevels()
        {
            _allSavedLevelFiles = SaveLoadService.GetAllSavedLevels();
            UpdateDropdownOptions(searchLevelInput != null ? searchLevelInput.text : string.Empty);
        }

        public void SetSearchText(string text)
        {
            if (searchLevelInput != null) searchLevelInput.text = text;
        }

        private void OnSearchInputChanged(string keyword)
        {
            if (_isSelectingFromDropdown) return;
            LevelMakerManager.Instance.currentLevelID = keyword;
            UpdateDropdownOptions(keyword);
        }

        private void UpdateDropdownOptions(string keyword)
        {
            levelDropdown.onValueChanged.RemoveListener(OnDropdownSelected);
            levelDropdown.ClearOptions();

            var options = new List<string> { "--- Tạo mới ---" };
            var filtered = string.IsNullOrEmpty(keyword)
                ? _allSavedLevelFiles
                : _allSavedLevelFiles.Where(x => x.ToLower().Contains(keyword.ToLower())).ToList();

            options.AddRange(filtered);
            levelDropdown.AddOptions(options);

            int currentIndex = options.IndexOf(LevelMakerManager.Instance.currentLevelID);
            levelDropdown.SetValueWithoutNotify(currentIndex >= 0 ? currentIndex : 0);
            levelDropdown.RefreshShownValue();

            levelDropdown.onValueChanged.AddListener(OnDropdownSelected);
        }

        private void OnDropdownSelected(int index)
        {
            if (_isSelectingFromDropdown) return;

            if (_checkUnsavedChanges != null && !_checkUnsavedChanges())
            {
                _isSelectingFromDropdown = true;
                levelDropdown.SetValueWithoutNotify(_previousDropdownIndex);
                levelDropdown.RefreshShownValue();
                _isSelectingFromDropdown = false;
                return;
            }

            _previousDropdownIndex = index;
            string selectedText = levelDropdown.options[index].text;
            _isSelectingFromDropdown = true;

            if (selectedText == "--- Tạo mới ---")
            {
                searchLevelInput.text = "Level_";
                LevelMakerManager.Instance.currentLevelID = "Level_";
                searchLevelInput.Select();
                searchLevelInput.caretPosition = searchLevelInput.text.Length;
                _onDataPreviewLoaded?.Invoke(null); // null = tạo mới, reset UI về default
            }
            else
            {
                searchLevelInput.text = selectedText;
                LevelMakerManager.Instance.currentLevelID = selectedText;
                var saveData = SaveLoadService.LoadLevelEditor(selectedText);
                _onDataPreviewLoaded?.Invoke(saveData);
            }

            levelDropdown.SetValueWithoutNotify(index);
            levelDropdown.RefreshShownValue();
            _isSelectingFromDropdown = false;
        }
    }
}
