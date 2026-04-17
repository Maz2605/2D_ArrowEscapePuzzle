using System;
using System.Collections.Generic;
using EditorTool.Scripts.EditorTool.System;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI.Panels
{
    /// <summary>
    /// Quản lý cài đặt bản đồ: Width, Height, Difficulty.
    /// Tích hợp thêm các điều khiển cho ảnh nền mẫu (Reference Image).
    /// </summary>
    public class MapSettingsPanel : MonoBehaviour
    {
        [Header("Map Settings UI")]
        [SerializeField] private TMP_Dropdown difficultyDropdown;
        [SerializeField] private TMP_InputField widthInput;
        [SerializeField] private TMP_InputField heightInput;

        [Header("Reference Image UI")]
        [SerializeField] private Button btnLoadRefImage;
        [SerializeField] private Slider opacitySlider;
        [SerializeField] private Slider scaleSlider;
        [SerializeField] private Slider posXSlider;
        [SerializeField] private Slider posYSlider;

        // === Callbacks cho Map Settings — được EditorController gán ===
        public Action OnMapResized;

        // === Callbacks cho Reference Image — được EditorController gán ===
        public Action OnLoadReferenceImage;
        public Action<float> OnReferenceOpacityChanged;
        public Action<float> OnReferenceScaleChanged;
        public Action<float> OnReferencePosXChanged;
        public Action<float> OnReferencePosYChanged;

        // Getters có clamp sẵn cho Map Size
        public int Width  => Mathf.Clamp(int.TryParse(widthInput.text,  out int w) ? w : 10, 3, 50);
        public int Height => Mathf.Clamp(int.TryParse(heightInput.text, out int h) ? h : 10, 3, 50);

        public void Initialize()
        {
            // --- Khởi tạo Map Settings ---
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(LevelDifficulty))));
            difficultyDropdown.value = (int)LevelMakerManager.Instance.currentDifficulty;
            difficultyDropdown.onValueChanged.AddListener(val =>
                LevelMakerManager.Instance.currentDifficulty = (LevelDifficulty)val);

            widthInput.onEndEdit.AddListener(_ => OnSizeInputChanged());
            heightInput.onEndEdit.AddListener(_ => OnSizeInputChanged());

            // --- Khởi tạo Reference Image Controls ---
            if (btnLoadRefImage != null)
                btnLoadRefImage.onClick.AddListener(() => OnLoadReferenceImage?.Invoke());

            if (opacitySlider != null)
            {
                opacitySlider.minValue = 0f;
                opacitySlider.maxValue = 1f;
                opacitySlider.value = 0.5f;
                opacitySlider.onValueChanged.AddListener(val => OnReferenceOpacityChanged?.Invoke(val));
            }

            if (scaleSlider != null)
            {
                scaleSlider.minValue = 0.1f;
                scaleSlider.maxValue = 10f;
                scaleSlider.value = 1f;
                scaleSlider.onValueChanged.AddListener(val => OnReferenceScaleChanged?.Invoke(val));
            }

            if (posXSlider != null)
            {
                posXSlider.minValue = -50f;
                posXSlider.maxValue = 50f;
                posXSlider.value = 0f;
                posXSlider.onValueChanged.AddListener(val => OnReferencePosXChanged?.Invoke(val));
            }

            if (posYSlider != null)
            {
                posYSlider.minValue = -50f;
                posYSlider.maxValue = 50f;
                posYSlider.value = 0f;
                posYSlider.onValueChanged.AddListener(val => OnReferencePosYChanged?.Invoke(val));
            }
        }

        private void OnSizeInputChanged()
        {
            widthInput.text  = Width.ToString();
            heightInput.text = Height.ToString();
            OnMapResized?.Invoke();
        }

        /// <summary>Refresh UI từ state thực tế của LevelMakerManager.</summary>
        public void Refresh()
        {
            widthInput.text  = LevelMakerManager.Instance.GridSystem.Width.ToString();
            heightInput.text = LevelMakerManager.Instance.GridSystem.Height.ToString();
            difficultyDropdown.SetValueWithoutNotify((int)LevelMakerManager.Instance.currentDifficulty);
            difficultyDropdown.RefreshShownValue();
        }

        /// <summary>Preview data từ file đã chọn (chưa Load).</summary>
        public void RefreshFromPreview(LevelSaveData data)
        {
            if (data == null)
            {
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