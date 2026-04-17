using System;
using ArrowGame.UI.Base;
using ArrowGame.Gameplay.Managers; 
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Popups
{
    public class GameplaySettingUI : BasePopup 
    {
        [Header("UI Buttons")]
        [SerializeField] private Button settingButton;
        [SerializeField] private Button musicButton;
        [SerializeField] private Button sfxButton;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private Button themeButton;
        [SerializeField] private Button backgroundButton;

        [Header("Off-State Visuals")]
        [SerializeField] private GameObject musicOffLine;
        [SerializeField] private GameObject sfxOffLine;
        [SerializeField] private GameObject vibrationOffLine;
        [SerializeField] private GameObject themeOffLine;

        [Header("Animation Config (Slide from Right)")]
        [SerializeField] private RectTransform[] slidingButtons; 
        [SerializeField] private float slideOffset = 300f; 
        [SerializeField] private float staggerDelay = 0.08f; 
        
        [Header("--- Icon Animation ---")]
        [SerializeField] private RectTransform settingIcon;
        [SerializeField] private float spinDuration = 0.5f;
        
        private bool _isIconSpined = false;
        private float[] _originalXPositions;

        protected override void Awake()
        {
            base.Awake(); 
            
            // Link các nút bấm tới SettingManager
            BindButton(musicButton, () => {
                SettingManager.Instance.ToggleMusic();
                UpdateUIVisuals();
            });

            BindButton(sfxButton, () => {
                SettingManager.Instance.ToggleSFX();
                UpdateUIVisuals();
            });

            BindButton(vibrationButton, () => {
                SettingManager.Instance.ToggleVibration();
                UpdateUIVisuals();
            });

            BindButton(themeButton, () => {
                SettingManager.Instance.ToggleTheme();
                UpdateUIVisuals();
            });

            BindButton(settingButton, Hide);
        
            if (backgroundButton != null) 
            {
                backgroundButton.onClick.RemoveAllListeners();
                backgroundButton.onClick.AddListener(Hide);
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

        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            UpdateUIVisuals(); // Cập nhật trạng thái On/Off của icon trước khi hiện
        }

        private void UpdateUIVisuals()
        {
            var settings = SettingManager.Instance.CurrentSettings;
            if (musicOffLine != null) musicOffLine.SetActive(!settings.isMusicEnabled);
           
            if (sfxOffLine != null) sfxOffLine.SetActive(!settings.isSfxEnabled);
           
            if (vibrationOffLine != null) vibrationOffLine.SetActive(!settings.isVibrationEnabled);
           
        }

        #region --- Animation Logic (Giữ nguyên từ bản gốc) ---

        protected override void PlayShowAnimation()
        {
            SpinIconForward();
            if (slidingButtons == null || slidingButtons.Length == 0) return;

            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true).SetLink(gameObject); 

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
            seq.SetUpdate(true).SetLink(gameObject);

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
            if (settingIcon == null || _isIconSpined) return;
            _isIconSpined = true;
            settingIcon.DOKill();
            settingIcon.localEulerAngles = Vector3.zero; 
            settingIcon.DORotate(new Vector3(0, 0, -180f), spinDuration, RotateMode.FastBeyond360)
                .SetRelative(true).SetEase(Ease.OutBack).SetUpdate(true); 
        }

        private void SpinIconBackward()
        {
            if (settingIcon == null || !_isIconSpined) return;
            _isIconSpined = false;
            settingIcon.DOKill();
            settingIcon.DORotate(new Vector3(0, 0, 180f), spinDuration, RotateMode.FastBeyond360)
                .SetRelative(true).SetEase(Ease.OutBack).SetUpdate(true); 
        }
        #endregion
    }
}