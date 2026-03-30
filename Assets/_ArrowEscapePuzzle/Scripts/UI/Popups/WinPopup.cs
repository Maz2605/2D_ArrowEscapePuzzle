using System;
using ArrowGame.UI.Base;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Managers;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Popups
{
    public class WinPopup : BasePopup
    {
        [Header("--- Buttons ---")]
        [SerializeField] private Button btnNext;
        [SerializeField] private Button btnHome;

        protected override void Awake()
        {
            base.Awake();
            if (btnNext != null) btnNext.onClick.AddListener(OnNextClicked);
            if (btnHome != null) btnHome.onClick.AddListener(OnHomeClicked);
        }

        private void OnNextClicked()
        {
            Hide();
            EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestRestartLevel);
        }

        private void OnHomeClicked()
        {
            Hide();
            GameStateManager.Instance.ChangeState(GameState.MainMenu);
        }

        protected override void PlayShowAnimation()
        {
            // TODO: Mặc định bật ngay, có thể thêm DoTween tuỳ bạn
        }

        protected override void PlayHideAnimation(Action onComplete)
        {
            // TODO: Mặc định tắt ngay
            onComplete?.Invoke();
        }

        protected void OnDestroy()
        {
            if (btnNext != null) btnNext.onClick.RemoveAllListeners();
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();
        }
    }
}