using GameCore.GameSystem;
using GameCore.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameCore.UI.Base
{
    public abstract class SettingBase : BasePopup 
    {
        [Header("--- Volume Sliders ---")]
        [SerializeField] protected Slider masterSlider;
        [SerializeField] protected Slider musicSlider;
        [SerializeField] protected Slider sfxSlider;

        [Header("--- State Toggles ---")]
        [SerializeField] protected SwitchToggle musicToggle;
        [SerializeField] protected SwitchToggle sfxToggle;
        [SerializeField] protected SwitchToggle vibrationToggle;
        
        [Header("--- Buttons ---")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backgroundButton;

        protected override void Awake()
        {
            base.Awake(); 
            if (closeButton) closeButton.onClick.AddListener(OnCloseClicked);
            if (backgroundButton) backgroundButton.onClick.AddListener(OnCloseClicked);
        }

        public override void Show()
        {
            base.Show(); 
            InitData(); 
        }

        private void InitData()
        {
            var settings = SettingsManager.Instance.CurrentSettings;
            var manager = SettingsManager.Instance;

            BindSlider(masterSlider, settings.masterVolume, manager.SetMasterVolume);
            BindToggleOnly(vibrationToggle, settings.isVibrationEnabled, manager.SetVibrationState);
            SetupSmartGroup(
                musicToggle, 
                musicSlider, 
                settings.isMusicEnabled, 
                manager.SetMusicState,
                settings.musicVolume,
                manager.SetMusicVolume
            );

            // --- SMART LOGIC: SFX ---
            SetupSmartGroup(
                sfxToggle, 
                sfxSlider, 
                settings.isSfxEnabled, 
                manager.SetSfxState,
                settings.sfxVolume,
                manager.SetSfxVolume
            );
        }

        // --- CORE LOGIC: GROUP TOGGLE & SLIDER ---
        private void SetupSmartGroup(
            SwitchToggle toggle, 
            Slider slider, 
            bool isEnabled, 
            UnityAction<bool> onToggleChanged,
            float sliderVal, 
            UnityAction<float> onSliderChanged)
        {
            if (toggle == null) return;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.ForceSetState(isEnabled); 
            
            toggle.onValueChanged.AddListener((isOn) =>
            {
                onToggleChanged?.Invoke(isOn);
                UpdateSliderVisualState(slider, isOn);
            });

            if (slider != null)
            {
                BindSlider(slider, sliderVal, onSliderChanged);
                UpdateSliderVisualState(slider, isEnabled);
            }
        }

        private void UpdateSliderVisualState(Slider slider, bool isActive)
        {
            if (slider == null) return;

            slider.interactable = isActive; 

            var canvasGroup = slider.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = slider.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = isActive ? 1f : 0.5f;
            canvasGroup.blocksRaycasts = isActive;
        }

        // --- Standard Helpers ---
        private void BindSlider(Slider s, float val, UnityAction<float> action)
        {
            if (s == null) return;
            s.SetValueWithoutNotify(val);
            s.onValueChanged.RemoveAllListeners();
            s.onValueChanged.AddListener(action);
        }

        private void BindToggleOnly(SwitchToggle t, bool state, UnityAction<bool> action)
        {
            if (t == null) return;
            t.ForceSetState(state);
            t.onValueChanged.RemoveAllListeners();
            t.onValueChanged.AddListener(action);
        }

        protected virtual void OnCloseClicked()
        {
            Hide();
        }
    }
}