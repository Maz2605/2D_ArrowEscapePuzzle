using System;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using ArrowGame.UI.Screens;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.UI;

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
                    UIManager.Instance.ClearAllPopups(true);
                    MainMenuScreen menuScreen = UIManager.Instance.ShowScreen<MainMenuScreen>(ScreenID.GameMenuScreen);
                    UIManager.Instance.HideLoading(() =>
                    {
                        menuScreen?.PlayCurrentSubScreenRevealAnimations();
                    });
                    break;

                case GameState.InGame:
                    UIManager.Instance.HideCurrentScreen();
                    _currentGameplayScreen = null;
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
                    _currentGameplayScreen = null;
                    break;

                case InGameState.Playing:
                    bool isReturningFromInternalState =
                        _previousInGameState == InGameState.Paused ||
                        _previousInGameState == InGameState.BoosterInstruction ||
                        _previousInGameState == InGameState.WaitingBoosterTarget ||
                        _previousInGameState == InGameState.BoosterExecuting ||
                        _previousInGameState == InGameState.Lose;

                    if (!isReturningFromInternalState || !IsGameplayScreenVisible())
                    {
                        UIManager.Instance.ClearAllPopups();
                        EnsureGameplayScreenVisible();
                    }
                    else
                    {
                        // Khi quay về từ luồng Booster (Cancel hoặc xài xong) hoặc luồng mua tim
                        if (_previousInGameState == InGameState.BoosterInstruction ||
                            _previousInGameState == InGameState.WaitingBoosterTarget ||
                            _previousInGameState == InGameState.BoosterExecuting ||
                            _previousInGameState == InGameState.Lose)
                        {
                            bool shouldClosePopup = true;
                            if (_previousInGameState == InGameState.BoosterExecuting && 
                                ArrowGame.Gameplay.Managers.TutorialManager.Instance != null && 
                                ArrowGame.Gameplay.Managers.TutorialManager.Instance.IsTutorialActive)
                            {
                                shouldClosePopup = false;
                            }

                            if (shouldClosePopup)
                            {
                                UIManager.Instance.CloseTopPopup(); // Đóng Popup hướng dẫn hoặc Popup mua tim
                            }

                            if (_currentGameplayScreen != null)
                            {
                                _currentGameplayScreen.SetGameplayHUDVisible(true);
                            }
                        }
                    }

                    // Kích hoạt BoosterIntroduction nếu có pending, VÀ nếu không phải quay về từ các state nội bộ (nghĩa là mới bắt đầu màn)
                    if (!isReturningFromInternalState)
                    {
                        if (ArrowGame.Gameplay.Managers.BoosterManager.Instance != null && ArrowGame.Gameplay.Managers.BoosterManager.Instance.HasPendingUnlockIntroductions())
                        {
                            if (_currentGameplayScreen != null)
                            {
                                _currentGameplayScreen.SetGameplayHUDVisible(false);
                            }
                            ShowBoosterIntroductionPopup();
                        }
                        else
                        {
                            // Đợi animation của màn hình Playing trượt lên xong (0.5s) rồi mới báo event
                            DG.Tweening.DOVirtual.DelayedCall(0.6f, () => 
                            {
                                GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.Post(ArrowGame.Data.Events.VisualEventID.AllBoosterIntroductionsCompleted);
                            });
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
                    if (GameManager.Instance.CanBuyHeart())
                    {
                        var buyHeartPopup = UIManager.Instance.ShowPopup<RequestBuyHeartPopup>(PopupID.RequestBuyHeartPopup);
                        if (buyHeartPopup != null)
                        {
                            buyHeartPopup.Setup(
                                price: 100,
                                onBuySuccess: () =>
                                {
                                    GameManager.Instance.HandleBuyHeartSuccess();
                                },
                                onTryAgain: () =>
                                {
                                    GameManager.Instance.FinalizeLevelFailed();
                                    UIManager.Instance.CloseTopPopup();
                                    UIManager.Instance.ShowPopup<LosePopup>(PopupID.LosePopup);
                                    EventManager<VisualEventID>.Post(VisualEventID.LosePopupShown);
                                }
                            );
                        }
                    }
                    else
                    {
                        GameManager.Instance.FinalizeLevelFailed();
                        UIManager.Instance.ShowPopup<LosePopup>(PopupID.LosePopup);
                        EventManager<VisualEventID>.Post(VisualEventID.LosePopupShown);
                    }
                    break;

                case InGameState.BoosterInstruction:
                    if (_currentGameplayScreen != null)
                    {
                        _currentGameplayScreen.SetGameplayHUDVisible(false);
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
                        _currentGameplayScreen.SetGameplayHUDVisible(false);
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

            screen.OnReplayClicked = () => GameManager.Instance.RequestReloadLevelWithEnergyWarning();
                
            screen.OnBackHomeClicked = () => GameManager.Instance.RequestBackHomeWithEnergyWarning();
        }

        private void EnsureGameplayScreenVisible()
        {
            if (IsGameplayScreenVisible()) return;

            _currentGameplayScreen = UIManager.Instance.ShowScreen<GameplayScreen>(ScreenID.GameplayScreen);
            WireUpGameplayScreen(_currentGameplayScreen);
        }

        private bool IsGameplayScreenVisible()
        {
            return _currentGameplayScreen != null && _currentGameplayScreen.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Hiển thị popup giới thiệu booster đầu tiên trong queue.
        ///
        /// Flow mới (đúng thứ tự):
        ///   1. User tap xác nhận popup
        ///   2. Popup capture screen pos của icon → spawn clone ngay tại đó → clone đứng yên (bobbing)
        ///   3. ConfirmCurrentUnlockIntroduction() (unlock data ngay)
        ///   4. EnterPlayingState() → BottomHUD slide vào
        ///   5. Khi BottomHUD slide vào xong → clone bay theo Bezier về đúng slot
        ///   6. Khi clone đến nơi → UpdateSlotData (hiển thị unlock)
        /// </summary>
        private void ShowBoosterIntroductionPopup()
        {
            var config = BoosterManager.Instance != null
                ? BoosterManager.Instance.GetCurrentUnlockIntroductionConfig()
                : null;

            if (config == null)
            {
                GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.Post(ArrowGame.Data.Events.VisualEventID.AllBoosterIntroductionsCompleted);
                return;
            }

            var popup = UIManager.Instance.ShowPopup<BoosterInstructionPopup>(PopupID.BoosterInstructionPopup);
            if (popup == null)
            {
                // Fallback: không có popup thì confirm ngay
                bool hasMore = ArrowGame.Gameplay.Managers.BoosterManager.Instance != null &&
                               ArrowGame.Gameplay.Managers.BoosterManager.Instance.ConfirmCurrentUnlockIntroduction();
                if (hasMore) ShowBoosterIntroductionPopup();
                else GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.Post(ArrowGame.Data.Events.VisualEventID.AllBoosterIntroductionsCompleted);
                return;
            }

            popup.SetupIntroduction(config, () =>
            {
                // ── BƯỚC 1: Spawn clone ngay tại vị trí icon của popup (popup chưa đóng hẳn)
                // Popup sẽ capture screen pos rồi trả về GameObject clone
                GameObject iconClone = popup.SpawnIconClone(UIManager.Instance.TopRoot);

                // Đăng ký trạng thái đang bay mở khóa trước khi confirm data (để giữ hiển thị Locked visual)
                ArrowGame.UI.HUD.BottomHUD.RegisterAnimatingUnlock(config.type);

                // ── BƯỚC 2: Unlock data ngay lập tức
                bool hasMoreIntroductions = BoosterManager.Instance != null &&
                                            BoosterManager.Instance.ConfirmCurrentUnlockIntroduction();

                // ── BƯỚC 3: Trở về Playing → BottomHUD slide vào
                // Khi BottomHUD slide xong → trigger fly animation cho clone
                if (_currentGameplayScreen != null)
                {
                    _currentGameplayScreen.SetGameplayHUDVisible(true,
                        onBottomHUDSlideInComplete: () =>
                        {
                            // ── BƯỚC 4: Clone bay từ vị trí hiện tại về slot BottomHUD
                            var flyPayload = new BoosterUnlockAnimPayload
                            {
                                BoosterType = config.type,
                                Icon = config.boosterIcon,
                                // Truyền clone đã tạo sẵn thay vì tạo mới
                                ExistingClone = iconClone,
                                OnAnimComplete = () =>
                                {
                                    // ── BƯỚC 5: Sau khi fly xong, tiếp tục queue (nếu có)
                                    if (hasMoreIntroductions)
                                    {
                                        ShowBoosterIntroductionPopup();
                                    }
                                    else
                                    {
                                        // Hoàn tất toàn bộ chuỗi Introduction
                                        GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.Post(ArrowGame.Data.Events.VisualEventID.AllBoosterIntroductionsCompleted);
                                    }
                                }
                            };
                            EventManager<VisualEventID>.Post(VisualEventID.PlayBoosterUnlockAnimation, flyPayload);
                        });
                }
                else
                {
                    // Fallback: không có GameplayScreen
                    if (iconClone != null) UnityEngine.Object.Destroy(iconClone);
                    GameManager.Instance.EnterPlayingState();
                }

                // ── BƯỚC 3b: Xóa EnterPlayingState ở đây để chờ fly animation xong
                popup.OnClosed = () =>
                {
                    // Không gọi EnterPlayingState ngay, chờ fly anim
                };
            });
        }


        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
    }
}
