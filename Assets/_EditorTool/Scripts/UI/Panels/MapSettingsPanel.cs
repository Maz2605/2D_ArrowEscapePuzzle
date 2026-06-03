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
    public class MapSettingsPanel : MonoBehaviour
    {
        [Header("UI References - Map Settings")]
        [SerializeField] private TMP_Dropdown _difficultyDropdown;
        [SerializeField] private TMP_InputField _widthInput;
        [SerializeField] private TMP_InputField _heightInput;

        // [Header("UI References - Reference Image")]
        // [SerializeField] private Button _btnLoadRefImage;
        // [SerializeField] private Slider _opacitySlider;
        // [SerializeField] private Slider _scaleSlider;
        // [SerializeField] private Slider _posXSlider;
        // [SerializeField] private Slider _posYSlider;

        public Action OnMapResized;
        public Action OnLoadReferenceImage;
        public Action<float> OnReferenceOpacityChanged;
        public Action<float> OnReferenceScaleChanged;
        public Action<float> OnReferencePosXChanged;
        public Action<float> OnReferencePosYChanged;

        public int Width => Mathf.Clamp(int.TryParse(_widthInput.text, out int w) ? w : 10, 3, 50);
        public int Height => Mathf.Clamp(int.TryParse(_heightInput.text, out int h) ? h : 10, 3, 50);

        public void Initialize()
        {
            SetupMapSettingsUI();
            // SetupReferenceImageUI();
        }

        private void SetupMapSettingsUI()
        {
            _difficultyDropdown.ClearOptions();
            _difficultyDropdown.AddOptions(new List<string>(Enum.GetNames(typeof(LevelDifficulty))));
            _difficultyDropdown.value = (int)LevelMakerManager.Instance.currentDifficulty;
            
            _difficultyDropdown.onValueChanged.RemoveAllListeners();
            _difficultyDropdown.onValueChanged.AddListener(val => LevelMakerManager.Instance.currentDifficulty = (LevelDifficulty)val);

            _widthInput.onEndEdit.RemoveAllListeners();
            _heightInput.onEndEdit.RemoveAllListeners();
            
            _widthInput.onEndEdit.AddListener(_ => OnSizeInputChanged());
            _heightInput.onEndEdit.AddListener(_ => OnSizeInputChanged());
        }

        // private void SetupReferenceImageUI()
        // {
        //     if (_btnLoadRefImage != null)
        //     {
        //         _btnLoadRefImage.onClick.RemoveAllListeners();
        //         _btnLoadRefImage.onClick.AddListener(() => OnLoadReferenceImage?.Invoke());
        //     }
        //
        //     SetupSlider(_opacitySlider, 0f, 1f, 0.5f, OnReferenceOpacityChanged);
        //     SetupSlider(_scaleSlider, 0.1f, 10f, 1f, OnReferenceScaleChanged);
        //     SetupSlider(_posXSlider, -50f, 50f, 0f, OnReferencePosXChanged);
        //     SetupSlider(_posYSlider, -50f, 50f, 0f, OnReferencePosYChanged);
        // }

        private void SetupSlider(Slider slider, float min, float max, float defaultVal, Action<float> callback)
        {
            if (slider == null) return;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultVal;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(val => callback?.Invoke(val));
        }

        private void OnSizeInputChanged()
        {
            _widthInput.text = Width.ToString();
            _heightInput.text = Height.ToString();
            OnMapResized?.Invoke();
        }

        public void Refresh()
        {
            _widthInput.text = LevelMakerManager.Instance.GridSystem.Width.ToString();
            _heightInput.text = LevelMakerManager.Instance.GridSystem.Height.ToString();
            _difficultyDropdown.SetValueWithoutNotify((int)LevelMakerManager.Instance.currentDifficulty);
            _difficultyDropdown.RefreshShownValue();
        }

        public void RefreshFromPreview(LevelSaveData data)
        {
            if (data == null)
            {
                _widthInput.text = "10";
                _heightInput.text = "10";
                _difficultyDropdown.value = 0;
                LevelMakerManager.Instance.currentDifficulty = (LevelDifficulty)0;
                return;
            }

            _widthInput.text = data.Width.ToString();
            _heightInput.text = data.Height.ToString();
            _difficultyDropdown.value = (int)data.Difficulty;
            LevelMakerManager.Instance.currentDifficulty = data.Difficulty;
        }
    }
}