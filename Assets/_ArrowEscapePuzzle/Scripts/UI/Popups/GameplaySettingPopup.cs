using System;
using ArrowGame.UI.Base;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Popups
{
    public class GameplaySettingUI : BaseSetting
    {
        [Header("UI Buttons")]
        [SerializeField] private Button settingButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button sfxButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private Button backgroundButton;

        [Header("Off-State Visuals")]
        [SerializeField] private GameObject musicOffLine;
        [SerializeField] private GameObject sfxOffLine;
        [SerializeField] private GameObject vibrationOffLine;

        [Header("Animation Config (Slide from Right)")]
        [SerializeField] private RectTransform[] slidingButtons; 
        [SerializeField] private float slideOffset = 300f; 
        [SerializeField] private float staggerDelay = 0.08f; 
        
        [Header("--- Icon Animation ---")]
        [SerializeField] private RectTransform settingIcon;
        [SerializeField] private float spinDuration = 0.5f;
        
        private bool _isIconSpinned = false;
        private float[] _originalXPositions;

        protected override void Awake()
        {
            base.Awake(); 
            
            BindButton(settingButton, OnBackgroundClicked);
            BindButton(musicButton, OnMusicButtonClicked);
            BindButton(sfxButton, OnSfxButtonClicked);
            BindButton(vibrationButton, OnVibrationButtonClicked);
        
            if (backgroundButton != null) 
            {
                backgroundButton.onClick.RemoveAllListeners();
                backgroundButton.onClick.AddListener(OnBackgroundClicked);
            }
            

            if (slidingButtons != null && slidingButtons.Length > 0)
            {
                _originalXPositions = new float[slidingButtons.Length];
                for (int i = 0; i < slidingButtons.Length; i++)
                {
                    _originalXPositions[i] = slidingButtons[i].anchoredPosition.x;
                }
            }
        }

        private void OnDestroy()
        {
            if (musicButton != null) musicButton.onClick.RemoveAllListeners();
            if (sfxButton != null) sfxButton.onClick.RemoveAllListeners();
            if (vibrationButton != null) vibrationButton.onClick.RemoveAllListeners();
            if (backgroundButton != null) backgroundButton.onClick.RemoveAllListeners();
            if (settingButton != null) settingButton.onClick.RemoveAllListeners();
        }

        private void OnMusicButtonClicked() => ToggleMusic();
        private void OnSfxButtonClicked() => ToggleSFX();
        private void OnVibrationButtonClicked() => ToggleVibration();
    
        private void OnBackgroundClicked() => Hide(); 

        protected override void UpdateUIVisuals()
        {
            if (musicOffLine != null) musicOffLine.SetActive(!CurrentSettings.isMusicEnabled);
            if (sfxOffLine != null) sfxOffLine.SetActive(!CurrentSettings.isSfxEnabled);
            if (vibrationOffLine != null) vibrationOffLine.SetActive(!CurrentSettings.isVibrationEnabled);
        }

        protected override void PlayShowAnimation()
        {
            SpinIconForward();
            if (slidingButtons == null || slidingButtons.Length == 0) return;

            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true); 

            for (int i = 0; i < slidingButtons.Length; i++)
            {
                RectTransform btnRect = slidingButtons[i];
                float targetX = _originalXPositions[i]; 

                btnRect.anchoredPosition = new Vector2(targetX + slideOffset, btnRect.anchoredPosition.y);
                seq.Insert(i * staggerDelay, btnRect.DOAnchorPosX(targetX, animDuration).SetEase(Ease.OutBack));
            }
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            SpinIconBackward();

            if (slidingButtons == null || slidingButtons.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);

            for (int i = 0; i < slidingButtons.Length; i++)
            {
                RectTransform btnRect = slidingButtons[i];
                float originalX = _originalXPositions[i];

                seq.Insert(i * staggerDelay, btnRect.DOAnchorPosX(originalX + slideOffset, animDuration).SetEase(Ease.InBack));
            }

            seq.OnComplete(() => onComplete?.Invoke());
        }
        
        private void SpinIconForward()
        {
            if (settingIcon == null || _isIconSpinned) return;

            _isIconSpinned = true;
            settingIcon.DOKill();
            
            settingIcon.localEulerAngles = Vector3.zero; 
            settingIcon.DORotate(new Vector3(0, 0, -180f), spinDuration, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.OutBack) 
                .SetUpdate(true); 
        }

        private void SpinIconBackward()
        {
            if (settingIcon == null || !_isIconSpinned) return;

            _isIconSpinned = false;
            settingIcon.DOKill();
            
            settingIcon.DORotate(new Vector3(0, 0, 180f), spinDuration, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.OutBack)
                .SetUpdate(true); 
        }
    }
}