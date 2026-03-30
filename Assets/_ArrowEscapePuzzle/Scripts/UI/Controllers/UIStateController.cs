using System;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using ArrowGame.UI.Screens;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.UI.Controllers
{
    public class UIStateController : MonoBehaviour
    {
        // THÊM BIẾN NÀY ĐỂ TRACK STATE TRƯỚC ĐÓ
        private GameState _previousState = GameState.Loading; 

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<GameState>(LogicGameEventID.GameStateChanged, OnStateChanged);
            EventManager<VisualEventID>.AddListener(VisualEventID.WinAnimationComplete, OnWinAnimationComplete);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<GameState>(LogicGameEventID.GameStateChanged, OnStateChanged);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.WinAnimationComplete, OnWinAnimationComplete);
        }

        private void OnStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Loading:
                    UIManager.Instance.ShowLoading();
                    break;

                case GameState.MainMenu:
                    UIManager.Instance.HideLoading();
                    UIManager.Instance.ClearAllPopups();
                    UIManager.Instance.ShowScreen<MainMenuScreen>(ScreenID.GameMenuScreen);
                    break;

                case GameState.IntroLevel:
                    UIManager.Instance.HideCurrentScreen();
                    UIManager.Instance.HideLoading();
                    break;

                case GameState.Playing:
                    // CHỐT LOGIC Ở ĐÂY: Chỉ setup lại nếu KHÔNG PHẢI từ Paused đi ra
                    if (_previousState != GameState.Paused)
                    {
                        UIManager.Instance.ClearAllPopups();
                        var gameplayScreen = UIManager.Instance.ShowScreen<GameplayScreen>(ScreenID.GameplayScreen);
                        WireUpGameplayScreen(gameplayScreen);
                    }
                    // Nếu _previousState == Paused, hệ thống bỏ qua, GameplayScreen nằm im không suy suyển.
                    break;

                case GameState.Paused:
                    var settingPopup = UIManager.Instance.ShowPopup<GameplaySettingUI>(PopupID.SettingPopup);
    
                    if (settingPopup != null)
                    {
                        settingPopup.OnClosed = () => 
                        {
                            if (GameStateManager.Instance.CurrentState == GameState.Paused)
                            {
                                GameStateManager.Instance.ChangeState(GameState.Playing);
                            }
                        };
                    }
                    break;

                case GameState.Win:
                    break;

                case GameState.Lose:
                    DOVirtual.DelayedCall(0.5f, () => {
                        UIManager.Instance.ShowPopup<LosePopup>(PopupID.LosePopup);
                    }).SetId(this);
                    break;

                case GameState.Shop:
                    break;
            }

            // SAU KHI XỬ LÝ XONG, LƯU STATE NÀY THÀNH PREVIOUS CHO LẦN SAU
            _previousState = newState;
        }
        
        private void WireUpGameplayScreen(GameplayScreen screen)
        {
            if (screen == null) return;

            screen.OnSettingClicked = () =>
                GameStateManager.Instance.ChangeState(GameState.Paused);

            screen.OnReplayClicked = () =>
                UIManager.Instance.ShowLoading(onCovered: () =>
                    EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestRestartLevel));

            screen.OnBackHomeClicked = () =>
                UIManager.Instance.ShowLoading(onCovered: () =>
                    GameStateManager.Instance.ChangeState(GameState.MainMenu));
        }

        private void OnWinAnimationComplete()
        {
            if (GameStateManager.Instance.CurrentState == GameState.Win)
            {
                UIManager.Instance.ShowPopup<WinPopup>(PopupID.WinPopup);
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}