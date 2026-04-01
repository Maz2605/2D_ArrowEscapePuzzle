using System;
using System.Collections.Generic;
using EditorTool.Scripts.EditorTool.System;
using ShareCore.Data;
using TMPro;
using UnityEngine;

namespace EditorTool.Scripts.UI.Panels
{
    /// <summary>
    /// Quản lý cài đặt bản đồ: Width, Height, Difficulty.
    /// Expose getters để EditorUIManager đọc khi cần (Facade pattern).
    /// </summary>
    public class MapSettingsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private TMP_InputField widthInput;
        [SerializeField] private TMP_InputField heightInput;

        // === Callback — được EditorController gán trước Initialize() ===
        public Action OnMapResized;

        // Getters có clamp sẵn để EditorUIManager dùng trực tiếp
        public int Width  => Mathf.Clamp(int.TryParse(widthInput.text,  out int w) ? w : 10, 3, 50);
        public int Height => Mathf.Clamp(int.TryParse(heightInput.text, out int h) ? h : 10, 3, 50);

        public void Initialize()
        {

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(LevelDifficulty))));
            difficultyDropdown.value = (int)LevelMakerManager.Instance.currentDifficulty;
            difficultyDropdown.onValueChanged.AddListener(val =>
                LevelMakerManager.Instance.currentDifficulty = (LevelDifficulty)val);

            widthInput.onEndEdit.AddListener(_ => OnSizeInputChanged());
            heightInput.onEndEdit.AddListener(_ => OnSizeInputChanged());
        }

        private void OnSizeInputChanged()
        {
            // Sync giá trị clamped về input trước khi báo resize
            widthInput.text  = Width.ToString();
            heightInput.text = Height.ToString();
            OnMapResized?.Invoke();
        }

        /// <summary>Refresh UI từ state thực tế của LevelMakerManager (sau Load).</summary>
        public void Refresh()
        {
            widthInput.text  = LevelMakerManager.Instance.GridSystem.Width.ToString();
            heightInput.text = LevelMakerManager.Instance.GridSystem.Height.ToString();
            difficultyDropdown.SetValueWithoutNotify((int)LevelMakerManager.Instance.currentDifficulty);
            difficultyDropdown.RefreshShownValue();
        }

        /// <summary>Preview data từ file đã chọn (chưa Load), dùng khi dropdown thay đổi.</summary>
        public void RefreshFromPreview(LevelSaveData data)
        {
            if (data == null)
            {
                // Tạo mới → reset về default
                widthInput.text  = "10";
                heightInput.text = "10";
                difficultyDropdown.value = 0;
                LevelMakerManager.Instance.currentDifficulty = (LevelDifficulty)0;
                return;
            }
            widthInput.text  = data.Width.ToString();
            heightInput.text = data.Height.ToString();
            difficultyDropdown.value = (int)data.Difficulty;
            LevelMakerManager.Instance.currentDifficulty = data.Difficulty;
        }
    }
}
