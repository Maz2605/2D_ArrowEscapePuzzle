using ArrowGame.Data;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Visual;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Controller
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
        private bool _isPlaying; 

        private void Start()
        {
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelComplete, HandleLevelComplete);
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelFailed, HandleLevelFailed);
            EventManager<LogicGameEventID>.AddListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
           
            inputController.OnGridCellClicked += HandleGridCellClicked;
            inputController.OnCameraPanStart += cameraController.StartPan;
            inputController.OnCameraPanProcess += cameraController.ProcessPan;
            inputController.OnCameraResetZoom += cameraController.ResetZoom;

            StartLevel();
        }

        private void OnDestroy()
        {
            if (inputController != null)
            {
                inputController.OnGridCellClicked -= HandleGridCellClicked;
                inputController.OnCameraPanStart -= cameraController.StartPan;
                inputController.OnCameraPanProcess -= cameraController.ProcessPan;
                inputController.OnCameraResetZoom -= cameraController.ResetZoom;
            }
            EventManager<LogicGameEventID>.RemoveListener(LogicGameEventID.LevelComplete, HandleLevelComplete);
        }
        
        private void StartLevel()
        {
            LevelSaveData currentLevelData = levelManager.LoadCurrentLevel();

            _gridLogic = new GridSystem(currentLevelData);
            _heartSystem = new HeartSystem(maxHeartsPerLevel, damageCooldown);
            gridView.Initialize(_gridLogic, currentLevelData);
            
            cameraController.InitializeCamera(_gridLogic.Width, _gridLogic.Height, 1.1f); // 1.1f là cellSize

            _isPlaying = true;

            Debug.Log($"[GameController] Bắt đầu chơi Level {levelManager.GetCurrentLevelIndex()} // Heart: {maxHeartsPerLevel}!");
        }

        private void HandleGridCellClicked(Vector2Int gridPos)
        {
            if (!_isPlaying) return;

            if (_gridLogic != null && _gridLogic.IsValidPosition(gridPos.x, gridPos.y))
            {
                _gridLogic.TryMoveArrow(gridPos.x, gridPos.y);
            }
        }

        private void HandleLevelComplete()
        {
            Debug.Log("[GameController] THẮNG RỒI! Khóa màn hình, chuẩn bị sang map mới...");
            
            _isPlaying = false;

            Invoke(nameof(StartLevel), 1.5f);
        }

        private void HandleLevelFailed()
        {
            _isPlaying = false;
            Debug.Log("===GameOver===");
            Invoke(nameof(StartLevel), 2.0f);
        }

        private void HandleArrowBlocked(ArrowData arrowData)
        {
            if (_isPlaying && _heartSystem != null)
            {
                _heartSystem.RemoveHeart();
            }
        }

        
        private void Update()
        {
            if (Input.GetKey(KeyCode.A))
                levelManager.ResetLevel();
        }
    }
}