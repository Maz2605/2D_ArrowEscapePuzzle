using System;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.UI.Base;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;

namespace ArrowGame.UI.Screens
{
    public class GameplayScreen : BaseScreen
    {
        [Header("--- Screen Controls ---")]
        [SerializeField] private Button btnSetting;
        [SerializeField] private Button btnReplay;
        [SerializeField] private Button btnHome;

        [Header("--- HUD Animation ---")]
        [SerializeField] private RectTransform topHUD;
        [SerializeField] private RectTransform bottomHUD; 
        [SerializeField] private float slideOffset = 300f; 
        [SerializeField] private Ease hideEffect =  Ease.InBack;
        [SerializeField] private Ease showEffect =  Ease.OutBack;
        
        [Header("--- Icon Animation ---")]
        [SerializeField] private RectTransform settingIcon;
        [SerializeField] private float spinDuration = 0.5f;
        
        private Vector2 _topHUDOriginPos;
        private Vector2 _bottomHUDOriginPos; 
        private bool _isIconSpinned = false;

        public Action OnSettingClicked;
        public Action OnReplayClicked;
        public Action OnBackHomeClicked;
        
        protected override void Awake()
        {
            base.Awake();
            
            BindButton(btnSetting, () =>
            {
                if (!_isIconSpinned)
                {
                    SpinIconForward();
                }

                OnSettingClicked?.Invoke();
            });
            BindButton(btnReplay, () => OnReplayClicked?.Invoke());
            BindButton(btnHome, () => OnBackHomeClicked?.Invoke());

            if (topHUD != null) _topHUDOriginPos = topHUD.anchoredPosition;
            if (bottomHUD != null) _bottomHUDOriginPos = bottomHUD.anchoredPosition;
        }
        
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, OnInGameStateChanged);
    
            // Đảm bảo UI luôn interactable khi Screen được hiện ra
            SetUIInteractable(true);
            
            if (topHUD != null)
            {
                topHUD.DOKill();
                topHUD.anchoredPosition = _topHUDOriginPos + new Vector2(0, slideOffset);
                SetTopHUDVisible(true);
            }

            SetBottomHUDVisible(true);
        }

        protected override void OnBeforeHide()
        {
            base.OnBeforeHide();
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, OnInGameStateChanged);
            
            // Reset interactable về mặc định khi screen ẩn
            SetUIInteractable(true);
            
            if (topHUD != null)
            {
                SetTopHUDVisible(false);
            }

            
            SetBottomHUDVisible(false);
        }
        
        private void OnInGameStateChanged(InGameState newState)
        {
            if (newState == InGameState.Playing && _isIconSpinned)
            {
                // SpinIconBackward();
            }
            
            // Chỉ cho phép tương tác UI khi đang Playing hoặc Paused (popup sẽ tự quản lý)
            // Các trạng thái animation phải khóa hoàn toàn để tránh bấm nhầm
            bool isInteractable = newState == InGameState.Playing ||
                                   newState == InGameState.Paused ||
                                   newState == InGameState.BoosterInstruction ||
                                   newState == InGameState.WaitingBoosterTarget ||
                                   newState == InGameState.BoosterExecuting;
            SetUIInteractable(isInteractable);
        }
        
        /// <summary>
        /// Khóa/mở tương tác với toàn bộ UI của GameplayScreen.
        /// Dùng để ngăn người chơi bấm Pause/Replay/Home trong lúc Intro hoặc Win/Lose Animation đang chạy.
        /// </summary>
        private void SetUIInteractable(bool isInteractable)
        {
            if (canvasGroup == null) return;
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = isInteractable;
        }
        private void SpinIconForward()
        {
            if (settingIcon == null) return;

            _isIconSpinned = true;
            settingIcon.DOKill();
            settingIcon.DORotate(new Vector3(0, 0, -180f), spinDuration, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.OutBack) 
                .SetUpdate(true)
                .SetLink(settingIcon.gameObject); 
        }

        private void SpinIconBackward()
        {
            if (settingIcon == null) return;

            _isIconSpinned = false;
            settingIcon.DOKill();
            settingIcon.DORotate(new Vector3(0, 0, 180f), spinDuration, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(settingIcon.gameObject); 
        }

        public void SetBottomHUDVisible(bool isVisible, System.Action onSlideInComplete = null)
        {
            if (bottomHUD == null) return;

            bottomHUD.DOKill();
    
            Vector2 targetPos = isVisible ? _bottomHUDOriginPos : _bottomHUDOriginPos - new Vector2(0, slideOffset);
            Ease easeType = isVisible ? showEffect : hideEffect;

            var tween = bottomHUD.DOAnchorPos(targetPos, transitionDuration)
                .SetEase(easeType)
                .SetUpdate(true)
                .SetLink(gameObject);

            if (isVisible && onSlideInComplete != null)
            {
                tween.OnComplete(() => onSlideInComplete.Invoke());
            }
        }

        public void SetTopHUDVisible(bool isVisible)
        {
            if (topHUD == null) return;

            topHUD.DOKill();

            Vector2 targetPos = isVisible ? _topHUDOriginPos : _topHUDOriginPos + new Vector2(0, slideOffset);
            Ease easeType = isVisible ? showEffect : hideEffect;

            topHUD.DOAnchorPos(targetPos, transitionDuration)
                .SetEase(easeType)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void SetGameplayHUDVisible(bool isVisible, System.Action onBottomHUDSlideInComplete = null)
        {
            SetTopHUDVisible(isVisible);
            SetBottomHUDVisible(isVisible, onBottomHUDSlideInComplete);
        }
        
        private void OnDestroy()
        {
            DOTween.Kill(this);
            transform.DOKill();
        }
    }
}
