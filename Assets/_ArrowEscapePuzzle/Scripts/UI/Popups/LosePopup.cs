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
    public class LosePopup : BasePopup
    {
        [Header("--- Buttons ---")]
        [SerializeField] private Button btnRetry;
        [SerializeField] private Button btnHome;

        protected override void Awake()
        {
            base.Awake();
            if (btnRetry != null) btnRetry.onClick.AddListener(OnRetryClicked);
            if (btnHome != null) btnHome.onClick.AddListener(OnHomeClicked);
        }

        private void OnRetryClicked()
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
            if (btnRetry != null) btnRetry.onClick.RemoveAllListeners();
            if (btnHome != null) btnHome.onClick.RemoveAllListeners();
        }
    }
}