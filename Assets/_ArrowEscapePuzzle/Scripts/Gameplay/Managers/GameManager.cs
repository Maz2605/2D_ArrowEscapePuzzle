using ArrowGame.Data;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Controllers;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Visual;
using ArrowGame.Interface;
using ArrowGame.UI.Manager;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine.InputSystem;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("System References")]
        [SerializeField] private GridView gridView;
        [SerializeField] private InputController inputController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private DifficultyIntroVFXController difficultyIntroVFXController;
        [Header("Managers")]
        [SerializeField] private LevelManager levelManager; 
        
        [Header("Game Settings")]
        [SerializeField] private int maxHeartsPerLevel = 3;
        [SerializeField] private float damageCooldown = 1.0f;
        
        [Header("Reward Settings")]
        [SerializeField] private int baseCoinPerStar = 20;
        [SerializeField] private float endStateCameraResetDuration = 0.35f;
        
        
        public GameState CurrentState { get; private set; }
        public InGameState CurrentInGameState { get; private set; }
        public LevelResultData CurrentLevelResult { get; private set; }
        private GridSystem _gridLogic;
        private HeartSystem _heartSystem;
        


        private void Start()
        {
            UIManager.Instance.Init();
            BoosterManager.Instance.Init();
            
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelComplete, HandleLevelComplete);
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelFailed, HandleLevelFailed);
            EventManager<LogicGameEventID>.AddListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
           
            inputController.OnGridCellClicked += HandleGridCellClicked;
            inputController.OnCameraPanStart += cameraController.StartPan;
            inputController.OnCameraPanProcess += cameraController.ProcessPan;
            inputController.OnCameraPanEnd += cameraController.EndPan;
            inputController.OnCameraResetZoom += cameraController.ResetView;

            EventManager<VisualEventID>.AddListener(VisualEventID.WinAnimationComplete, OnWinAnimationComplete);
            EventManager<VisualEventID>.AddListener(VisualEventID.IntroAnimationComplete, OnIntroAnimationComplete);
            EventManager<VisualEventID>.AddListener(VisualEventID.LoseAnimationComplete, OnLoseAnimationComplete);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.ShowHintVisual, HandleShowHintCameraFocus);
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.RequestLoadLevel, OnLoadLevel);
            
            ChangeState(GameState.Loading);
            
            UIManager.Instance.ShowLoading( () => {
                ChangeState(GameState.MainMenu);
            });
            // OnLoadLevel();
        }
        

        private void OnDestroy()
        {
            EventManager<LogicGameEventID>.RemoveListener(LogicGameEventID.LevelComplete, HandleLevelComplete);
            EventManager<LogicGameEventID>.RemoveListener(LogicGameEventID.LevelFailed, HandleLevelFailed);
            EventManager<LogicGameEventID>.RemoveListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);

            if (inputController != null)
            {
                inputController.OnGridCellClicked -= HandleGridCellClicked;
                inputController.OnCameraPanStart -= cameraController.StartPan;
                inputController.OnCameraPanProcess -= cameraController.ProcessPan;
                inputController.OnCameraPanEnd -= cameraController.EndPan;
                inputController.OnCameraResetZoom -= cameraController.ResetView;
            }

            EventManager<VisualEventID>.RemoveListener(VisualEventID.WinAnimationComplete, OnWinAnimationComplete);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.IntroAnimationComplete, OnIntroAnimationComplete);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.LoseAnimationComplete, OnLoseAnimationComplete);
            EventManager<VisualEventID>.RemoveListener<string>(VisualEventID.ShowHintVisual, HandleShowHintCameraFocus);
            EventManager<LogicGameEventID>.RemoveListener(LogicGameEventID.RequestLoadLevel, OnLoadLevel);
            
            DOTween.Kill(this); 
        }
        
        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            
            GameState oldState = CurrentState;
            CurrentState = newState;
            
            if (oldState == GameState.InGame && newState != GameState.InGame)
            {
                CurrentInGameState = InGameState.None;
            }
            
            Debug.Log($"[GameController] GameState: {oldState} -> {newState}");
            EventManager<LogicGameEventID>.Post(LogicGameEventID.GameStateChanged, newState);
        }
        
        private void ChangeInGameState(InGameState newState)
        {
            if (CurrentState != GameState.InGame) return;
            if (CurrentInGameState == newState) return;
            
            InGameState oldState = CurrentInGameState;
            CurrentInGameState = newState;
            
            Debug.Log($"[GameController] InGameState: {oldState} -> {newState}");
            
            bool shouldLockInput = (newState != InGameState.Playing && newState != InGameState.WaitingBoosterTarget);
            if (inputController != null) inputController.IsLocked = shouldLockInput;

            EventManager<LogicGameEventID>.Post(LogicGameEventID.InGameStateChanged, newState);
        }

        public void RequestChangeInGameState(InGameState requestedState)
        {
            if (requestedState == InGameState.Paused && CurrentInGameState != InGameState.Playing)
            {
                Debug.LogWarning($"[GameController] Từ chối Pause vì game đang ở state: {CurrentInGameState}");
                return;
            }

            if (requestedState == InGameState.BoosterInstruction && CurrentInGameState != InGameState.Playing)
            {
                Debug.LogWarning($"[GameController] Từ chối mở hướng dẫn Booster vì game đang ở state: {CurrentInGameState}");
                return;
            }

            if (requestedState == InGameState.WaitingBoosterTarget &&
                CurrentInGameState != InGameState.Playing &&
                CurrentInGameState != InGameState.BoosterInstruction)
            {
                Debug.LogWarning($"[GameController] Từ chối nhắm Booster vì game đang ở state: {CurrentInGameState}");
                return;
            }

            ChangeInGameState(requestedState);
        }

        public void RequestBackHome()
        {
            // Ch\u1ec9 cho ph\u00e9p tho\u00e1t v\u1ec1 Home khi \u0111ang \u1edf state an to\u00e0n, tr\u00e1nh cancel animation quan tr\u1ecdng
            if (CurrentState == GameState.InGame)
            {
                bool isInSafeState = CurrentInGameState == InGameState.Playing ||
                                     CurrentInGameState == InGameState.Paused ||
                                     CurrentInGameState == InGameState.Win ||
                                     CurrentInGameState == InGameState.Lose;
                if (!isInSafeState)
                {
                    Debug.LogWarning($"[GameManager] T\u1eeb ch\u1ed1i BackHome v\u00ec InGameState hi\u1ec7n t\u1ea1i l\u00e0: {CurrentInGameState}");
                    return;
                }
            }
            
            UIManager.Instance.ShowLoading(() =>
            {
                ChangeState(GameState.MainMenu);
            });
        }


        public void OnLoadLevel()
        {
            // Chỉ cho phép load level khi không đang chạy animation quan trọng
            // Tránh việc người chơi Replay ngay giữa lúc Win/Lose animation đang chạy
            if (CurrentState == GameState.InGame)
            {
                bool isInBlockedState = CurrentInGameState == InGameState.Intro ||
                                        CurrentInGameState == InGameState.WinPending ||
                                        CurrentInGameState == InGameState.LosePending ||
                                        CurrentInGameState == InGameState.WinAnimating ||
                                        CurrentInGameState == InGameState.LoseAnimating;
                if (isInBlockedState)
                {
                    Debug.LogWarning($"[GameManager] Từ chối LoadLevel vì InGameState hiện tại là: {CurrentInGameState}");
                    return;
                }
            }

            DOTween.Kill("BoosterExecution");
            BoosterManager.Instance.ClearOnRestart();
            LevelSaveData currentLevelData = levelManager.LoadCurrentLevelMap();
            difficultyIntroVFXController?.SetCurrentDifficulty(currentLevelData.Difficulty);

            _gridLogic = new GridSystem(currentLevelData);
            BoosterManager.Instance.Initialize(_gridLogic);
            _heartSystem = new HeartSystem(maxHeartsPerLevel, damageCooldown);
            
            gridView.Initialize(_gridLogic, currentLevelData);
            cameraController.InitializeCamera(_gridLogic.Width, _gridLogic.Height, 1.1f);
            
            ChangeState(GameState.InGame);
            ChangeInGameState(InGameState.Intro);

            Debug.Log($"[GameController] Khởi tạo Level {DataManager.Instance.GetActiveLevel()} // Tim: {maxHeartsPerLevel}");
        }

        public void OnLoseAnimationComplete()
        {
            ChangeInGameState(InGameState.Lose);
        }

        private void HandleGridCellClicked(Vector2Int gridPos)
        {
            if (CurrentInGameState == InGameState.WaitingBoosterTarget)
            {
                BoosterManager.Instance.TryHandlePendingBoosterClick(gridPos);
                return;
            }
            
            if (CurrentInGameState == InGameState.Playing)
            {
                if (_gridLogic != null && _gridLogic.IsValidPosition(gridPos.x, gridPos.y))
                {
                    _gridLogic.TryMoveArrow(gridPos.x, gridPos.y);
                }
            }
        }

        private void HandleLevelComplete()
        {
            int playLevelIndex = DataManager.Instance.GetActiveLevel();
            LevelSaveData currentLevelData = levelManager.LoadCurrentLevelMap(); 
            
            bool isReplay = DataManager.Instance.HasPlayedLevel(playLevelIndex);
            int oldStars = DataManager.Instance.GetLevelStars(playLevelIndex);
            
            int earnedStars = _heartSystem != null ? _heartSystem.CurrentHeart : 3;
            int earnedCoins = 0;

            if (earnedStars > oldStars)
            {
                float multiplier = isReplay ? 1.0f : currentLevelData.Difficulty.GetMultiplier();
                earnedCoins = Mathf.RoundToInt(earnedStars * baseCoinPerStar * multiplier);
            }

            CurrentLevelResult = new LevelResultData
            {
                LevelIndex = playLevelIndex,
                Stars = earnedStars,
                Coins = earnedCoins
            };

            if (earnedCoins > 0) DataManager.Instance.AddCoin(earnedCoins);
            if (earnedStars > oldStars) DataManager.Instance.SaveLevelStars(playLevelIndex, earnedStars);
            DataManager.Instance.CompleteCurrentLevel();

            // Chuyển sang WinPending ngay lập tức để khóa cả Gameplay và UI button, 
            // không có khoảng trống "Playing" trong khi đang chờ delay camera
            ChangeInGameState(InGameState.WinPending);
            DOVirtual.DelayedCall(1f, () =>
            {
                ResetCameraThenChangeState(InGameState.WinAnimating);
            });
            
            Debug.Log($"[GameController] THẮNG Level {playLevelIndex}! Cũ: {oldStars} -> Mới: {earnedStars}. Tiền: {earnedCoins}");
        }

        private void HandleLevelFailed()
        {
            // Chuyển sang LosePending ngay lập tức để khóa cả Gameplay và UI button
            ChangeInGameState(InGameState.LosePending);
            DOVirtual.DelayedCall(0.5f, () =>
            {
                ResetCameraThenChangeState(InGameState.LoseAnimating);
            });
        }

        private void HandleArrowBlocked(ArrowData arrowData)
        {
            if (CurrentInGameState == InGameState.Playing && _heartSystem != null)
            {
                _heartSystem.RemoveHeart();
            }
        }

        private void HandleShowHintCameraFocus(string arrowId)
        {
            if (CurrentState != GameState.InGame || cameraController == null || gridView == null) return;

            ArrowLineView arrowView = gridView.GetArrowViewById(arrowId);
            if (arrowView == null || !arrowView.gameObject.activeInHierarchy) return;

            cameraController.FocusOn(arrowView.HeadPosition, 0.6f);
        }

        private void ResetCameraThenChangeState(InGameState targetState)
        {
            if (cameraController == null)
            {
                ChangeInGameState(targetState);
                return;
            }

            cameraController.ResetView(endStateCameraResetDuration, () => { ChangeInGameState(targetState); });
        }

        private void OnIntroAnimationComplete()
        {
            ChangeInGameState(InGameState.Playing);
            EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowCountChanged, _gridLogic.RemainingArrows);
        }

        private void OnWinAnimationComplete()
        {
            ChangeInGameState(InGameState.Win);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // if (Keyboard.current.aKey.wasPressedThisFrame)
            // {
            //     DataManager.Instance.DeleteAllProgress();
            //     OnLoadLevel(); 
            // }

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                DataManager.Instance.InitTestBoosters();
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                DataManager.Instance.AddCoin(1000);
                Debug.Log("Add 1000 Coin");
            }
        }
    }
}
