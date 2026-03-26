using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
using ArrowGame.Gameplay.Visual;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Controllers
{
    public class GameController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private GridView gridView;
        [SerializeField] private InputController inputController;
        [SerializeField] private CameraController cameraController;
        
        [Header("Managers")]
        [SerializeField] private LevelManager levelManager; 
        
        [Header("Game Settings")]
        [SerializeField] private int maxHeartsPerLevel = 3;
        [SerializeField] private float damageCooldown = 1.0f;

        private GridSystem _gridLogic;
        private HeartSystem _heartSystem;

        private void Awake()
        {
            DOTween.SetTweensCapacity(500, 200);
        
            DOTween.defaultAutoKill = true;
            DOTween.defaultRecyclable = true;
        }

        private void Start()
        {
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelComplete, HandleLevelComplete);
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelFailed, HandleLevelFailed);
            EventManager<LogicGameEventID>.AddListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
           
            inputController.OnGridCellClicked += HandleGridCellClicked;
            inputController.OnCameraPanStart += cameraController.StartPan;
            inputController.OnCameraPanProcess += cameraController.ProcessPan;
            inputController.OnCameraResetZoom += cameraController.ResetZoom;

            EventManager<VisualEventID>.AddListener(VisualEventID.WinAnimationComplete, OnWinAnimationComplete);
            EventManager<VisualEventID>.AddListener(VisualEventID.IntroAnimationComplete, OnIntroAnimationComplete);

            StartLevel();
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
                inputController.OnCameraResetZoom -= cameraController.ResetZoom;
            }
            
            EventManager<VisualEventID>.RemoveListener(VisualEventID.WinAnimationComplete, OnWinAnimationComplete);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.IntroAnimationComplete, OnIntroAnimationComplete);
            
            DOTween.Kill(this); 
        }
        
        private void StartLevel()
        {
            LevelSaveData currentLevelData = levelManager.LoadCurrentLevelMap();

            _gridLogic = new GridSystem(currentLevelData);
            _heartSystem = new HeartSystem(maxHeartsPerLevel, damageCooldown);
            gridView.Initialize(_gridLogic, currentLevelData);
            
            cameraController.InitializeCamera(_gridLogic.Width, _gridLogic.Height, 1.1f);

            GameStateManager.Instance.ChangeState(GameState.IntroLevel);

            Debug.Log($"[GameController] Bắt đầu Level {DataManager.Instance.GetCurrentLevel()} // Tim: {maxHeartsPerLevel}");
        }

        private void HandleGridCellClicked(Vector2Int gridPos)
        {
            if (GameStateManager.Instance.CurrentState != GameState.Playing) return;

            if (_gridLogic != null && _gridLogic.IsValidPosition(gridPos.x, gridPos.y))
            {
                _gridLogic.TryMoveArrow(gridPos.x, gridPos.y);
            }
        }

        private void HandleLevelComplete()
        {
            GameStateManager.Instance.ChangeState(GameState.Win);
            Debug.Log("[GameController] THẮNG RỒI! Bắt đầu chạy Animation ở GridView...");
            
            DataManager.Instance.IncreaseLevel();

        }

        private void HandleLevelFailed()
        {
            GameStateManager.Instance.ChangeState(GameState.Lose);
            Debug.Log("=== GAME OVER ===");
            
            DOVirtual.DelayedCall(2.0f, StartLevel).SetId(this);
        }

        private void HandleArrowBlocked(ArrowData arrowData)
        {
            if (GameStateManager.Instance.CurrentState == GameState.Playing && _heartSystem != null)
            {
                _heartSystem.RemoveHeart();
            }
        }

        private void OnWinAnimationComplete()
        {
            DOVirtual.DelayedCall(2.0f, StartLevel).SetId(this);
        }

        private void OnIntroAnimationComplete()
        {
            GameStateManager.Instance.ChangeState(GameState.Playing);
        }

        // Test Input tạm thời
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                DataManager.Instance.ResetLevelData();
                StartLevel(); 
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
            }
        }
    }
}