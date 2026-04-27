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
        private InGameState _previousInGameState = InGameState.None;

        private GameplayScreen _currentGameplayScreen;

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<GameState>(LogicGameEventID.GameStateChanged,
                OnGameStateChanged);
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged,
                OnInGameStateChanged);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<GameState>(LogicGameEventID.GameStateChanged,
                OnGameStateChanged);
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged,
                OnInGameStateChanged);
        }


        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.Loading:
                    // UIManager.Instance.ShowLoading();
                    break;

                case GameState.MainMenu:
                    DOVirtual.DelayedCall(0.5f, () => UIManager.Instance.HideLoading());
                    UIManager.Instance.ClearAllPopups();
                    UIManager.Instance.ShowScreen<MainMenuScreen>(ScreenID.GameMenuScreen);
                    break;

                case GameState.InGame:
                    UIManager.Instance.HideCurrentScreen();
                    // DOVirtual.DelayedCall(0.5f, () => UIManager.Instance.HideLoading());
                    UIManager.Instance.HideLoading();
                    break;

                case GameState.Shop:
                    break;
            }
        }

        private void OnInGameStateChanged(InGameState newState)
        {
            switch (newState)
            {
                case InGameState.Intro:
                    UIManager.Instance.ClearAllPopups();
                    UIManager.Instance.HideCurrentScreen();
                    break;

                case InGameState.Playing:
                    bool isReturningFromInternalState =
                        _previousInGameState == InGameState.Paused ||
                        _previousInGameState == InGameState.BoosterInstruction ||
                        _previousInGameState == InGameState.WaitingBoosterTarget ||
                        _previousInGameState == InGameState.BoosterExecuting;

                    if (!isReturningFromInternalState)
                    {
                        UIManager.Instance.ClearAllPopups();
                        _currentGameplayScreen = UIManager.Instance.ShowScreen<GameplayScreen>(ScreenID.GameplayScreen);
                        WireUpGameplayScreen(_currentGameplayScreen);
                    }
                    else
                    {
                        // Khi quay về từ luồng Booster (Cancel hoặc xài xong)
                        if (_previousInGameState == InGameState.BoosterInstruction ||
                            _previousInGameState == InGameState.WaitingBoosterTarget ||
                            _previousInGameState == InGameState.BoosterExecuting)
                        {
                            UIManager.Instance.CloseTopPopup(); // Đóng Popup hướng dẫn

                            if (_currentGameplayScreen != null)
                            {
                                _currentGameplayScreen.SetBottomHUDVisible(true); // Trượt BottomHUD lên lại
                            }
                        }
                    }

                    break;

                case InGameState.Paused:
                    var settingPopup = UIManager.Instance.ShowPopup<GameplaySettingUI>(PopupID.SettingPopup);
                    if (settingPopup != null)
                    {
                        settingPopup.OnClosed = () =>
                            GameManager.Instance.RequestChangeInGameState(InGameState.Playing);
                    }

                    break;

                case InGameState.Win:
                    var winPopup = UIManager.Instance.ShowPopup<WinPopup>(PopupID.WinPopup);
                    if (winPopup != null)
                    {
                        var resultData = GameManager.Instance.CurrentLevelResult;
                        winPopup.SetupAndAnimate(resultData.LevelIndex, resultData.Stars, resultData.Coins);
                    }

                    break;

                case InGameState.Lose:
                    UIManager.Instance.ShowPopup<LosePopup>(PopupID.LosePopup);
                    break;

                case InGameState.BoosterInstruction:
                    if (_currentGameplayScreen != null)
                    {
                        _currentGameplayScreen.SetBottomHUDVisible(false);
                    }

                    var boosterPopup = UIManager.Instance.ShowPopup<BoosterInstructionPopup>(PopupID.BoosterInstructionPopup);
                    if (boosterPopup != null)
                    {
                        boosterPopup.Setup(
                            BoosterManager.Instance.GetPendingBoosterConfig(),
                            true,
                            () => BoosterManager.Instance.ConfirmPendingBoosterFromPopup(),
                            () => BoosterManager.Instance.CancelPendingBooster()
                        );
                    }
                    break;

                case InGameState.WaitingBoosterTarget:
                    if (_currentGameplayScreen != null)
                    {
                        _currentGameplayScreen.SetBottomHUDVisible(false);
                    }
                    break;

                case InGameState.BoosterExecuting:
                    UIManager.Instance.CloseTopPopup();
                    break;
            }

            _previousInGameState = newState;
        }

        private void WireUpGameplayScreen(GameplayScreen screen)
        {
            if (screen == null) return;

            screen.OnSettingClicked = () =>
                GameManager.Instance.RequestChangeInGameState(InGameState.Paused);

            screen.OnReplayClicked = () => EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel);
                
            screen.OnBackHomeClicked = () => GameManager.Instance.RequestBackHome();
        }


        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
