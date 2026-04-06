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
    
            if (topHUD != null)
            {
                topHUD.DOKill();
                topHUD.anchoredPosition = _topHUDOriginPos + new Vector2(0, slideOffset);
        
                topHUD.DOAnchorPos(_topHUDOriginPos, transitionDuration)
                    .SetEase(showEffect)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }

            if (bottomHUD != null)
            {
                bottomHUD.DOKill();
                bottomHUD.anchoredPosition = _bottomHUDOriginPos - new Vector2(0, slideOffset);
        
                bottomHUD.DOAnchorPos(_bottomHUDOriginPos, transitionDuration)
                    .SetEase(showEffect)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
        }

        protected override void OnBeforeHide()
        {
            base.OnBeforeHide();
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, OnInGameStateChanged);
            if (topHUD != null)
            {
                topHUD.DOKill();
                topHUD.DOAnchorPos(_topHUDOriginPos + new Vector2(0, slideOffset), transitionDuration)
                    .SetEase(hideEffect) 
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }

            
            if (bottomHUD != null)
            {
                bottomHUD.DOKill();
                bottomHUD.DOAnchorPos(_bottomHUDOriginPos - new Vector2(0, slideOffset), transitionDuration)
                    .SetEase(hideEffect)
                    .SetUpdate(true)
                    .SetLink(gameObject);
            }
        }
        
        private void OnInGameStateChanged(InGameState newState)
        {
            if (newState == InGameState.Playing && _isIconSpinned)
            {
                // SpinIconBackward();
            }
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

        private void OnDestroy()
        {
            DOTween.Kill(this);
            transform.DOKill();
        }
    }
}