using System;
using ArrowGame.UI.Base;
using UnityEngine;
using UnityEngine.UI;


namespace ArrowGame.UI.Screens
{
    public class GamePlayScreen : BaseScreen
    {
        [Header("--- Screen Controls ---")]
        [SerializeField] private Button btnSetting;
        [SerializeField] private Button btnReplay;
        [SerializeField] private Button btnHome;

        public Action OnSettingClicked;
        public Action OnReplayClicked;
        public Action OnBackHomeClicked;
        
        protected override void Awake()
        {
            base.Awake();
            
            BindButton(btnSetting, () => OnSettingClicked?.Invoke());
            BindButton(btnReplay, () => OnReplayClicked?.Invoke());
            BindButton(btnHome, () => OnBackHomeClicked?.Invoke());
        }

        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
        }
    }
}