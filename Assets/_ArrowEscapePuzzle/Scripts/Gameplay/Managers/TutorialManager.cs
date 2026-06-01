using System;
using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using ShareCore.Scripts.Data;
using ArrowGame.Data.LevelProvider;
using ArrowGame.Gameplay.Tutorials;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    public class TutorialManager : Singleton<TutorialManager>
    {
        [Header("Tutorial Settings")]
        [SerializeField] private TutorialInformation tutorialInformation;

        private LevelSaveData _currentLevelData;
        private TutorialConfigSO _currentTutorialConfig;
        private BaseTutorialHandler _currentHandler;
        private int _currentStepIndex = -1;
        private TutorialOverlayUI _overlayUI;
        private bool _isTutorialActive;

        public bool IsTutorialActive => _isTutorialActive;
        public TutorialConfigSO CurrentTutorialConfig => _currentTutorialConfig;
        public int CurrentStepIndex => _currentStepIndex;

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<LevelSaveData>(LogicGameEventID.LevelLoaded, OnLevelLoaded);
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, OnInGameStateChanged);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<LevelSaveData>(LogicGameEventID.LevelLoaded, OnLevelLoaded);
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, OnInGameStateChanged);
        }

        private void Update()
        {
            if (_isTutorialActive && _currentHandler != null)
            {
                _currentHandler.OnUpdate();
            }
        }

        private void OnLevelLoaded(LevelSaveData levelData)
        {
            _currentLevelData = levelData;
            _currentStepIndex = -1;
            _isTutorialActive = false;
            _overlayUI = null;

            if (_currentHandler != null)
            {
                _currentHandler.CleanUp();
                _currentHandler = null;
            }

            if (levelData != null)
            {
                // Lấy cấu hình hướng dẫn từ TutorialInformation gán trên Manager
                if (tutorialInformation != null)
                {
                    _currentTutorialConfig = tutorialInformation.GetTutorialForLevel(levelData.LevelID);
                }
                else
                {
                    // Fallback tải động từ Resources nếu chưa gán database
                    _currentTutorialConfig = Resources.Load<TutorialConfigSO>($"Tutorials/{levelData.LevelID}");
                }
                
                if (_currentTutorialConfig != null && _currentTutorialConfig.steps != null && _currentTutorialConfig.steps.Count > 0)
                {
                    _isTutorialActive = true;
                    _currentStepIndex = 0;
                    Debug.Log($"[TutorialManager] Khởi động Tutorial cho {levelData.LevelID} bằng ScriptableObject.");
                }
            }
        }

        private void OnInGameStateChanged(InGameState newState)
        {
            // Chỉ hiển thị UI hướng dẫn khi màn chơi đã kết thúc hiệu ứng mở màn (InGameState.Playing)
            if (newState == InGameState.Playing && _isTutorialActive && _currentStepIndex == 0)
            {
                ShowCurrentStep();
            }
        }

        private void ShowCurrentStep()
        {
            if (_currentTutorialConfig == null || _currentTutorialConfig.steps == null || _currentStepIndex >= _currentTutorialConfig.steps.Count)
            {
                CompleteTutorial();
                return;
            }

            TutorialStepConfig step = _currentTutorialConfig.steps[_currentStepIndex];

            // Hiển thị Popup overlay trên cùng
            if (_overlayUI == null)
            {
                _overlayUI = UIManager.Instance.ShowPopup<TutorialOverlayUI>(PopupID.TutorialOverlay);
            }

            if (_overlayUI != null)
            {
                // Khởi tạo Handler nếu chưa được tạo
                if (_currentHandler == null && _currentLevelData != null)
                {
                    string handlerTypeName = $"ArrowGame.Gameplay.Tutorials.{_currentLevelData.LevelID}_TutorialHandler";
                    Type handlerType = Type.GetType(handlerTypeName);
                    if (handlerType != null)
                    {
                        _currentHandler = (BaseTutorialHandler)Activator.CreateInstance(handlerType);
                        _currentHandler.Init(_currentTutorialConfig, _overlayUI);
                        Debug.Log($"[TutorialManager] Đã khởi tạo Handler: {handlerTypeName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[TutorialManager] Không tìm thấy Handler: {handlerTypeName}, dùng Handler mặc định.");
                    }
                }

                // Kích hoạt bước hiện tại trong Handler
                if (_currentHandler != null)
                {
                    _currentHandler.OnStepStarted(_currentStepIndex, step);
                }

                Vector3 worldPos = Vector3.zero;
                if (GameManager.Instance != null && GameManager.Instance.CurrentGridView != null)
                {
                    worldPos = GameManager.Instance.CurrentGridView.GetCellWorldPosition(step.targetGridPos);
                }
                
                _overlayUI.ShowStep(worldPos, step.tooltipText, step.showHandPointer);
            }
        }

        public bool IsGridActionAllowed(Vector2Int gridPos)
        {
            if (!_isTutorialActive) return true;
            if (_currentHandler != null)
            {
                return _currentHandler.IsGridActionAllowed(gridPos);
            }
            return true;
        }

        public void AdvanceToNextStep()
        {
            if (!_isTutorialActive) return;

            _currentStepIndex++;
            if (_currentTutorialConfig != null && _currentTutorialConfig.steps != null && _currentStepIndex < _currentTutorialConfig.steps.Count)
            {
                ShowCurrentStep();
            }
            else
            {
                CompleteTutorial();
            }
        }

        public void PlayErrorFeedback()
        {
            if (_overlayUI != null)
            {
                _overlayUI.ShowErrorFeedback();
            }
        }

        private void CompleteTutorial()
        {
            _isTutorialActive = false;
            
            if (_overlayUI != null)
            {
                UIManager.Instance.CloseTopPopup();
                _overlayUI = null;
            }

            if (_currentHandler != null)
            {
                _currentHandler.CleanUp();
                _currentHandler = null;
            }

            if (_currentLevelData != null)
            {
                DataManager.Instance.MarkTutorialCompleted(_currentLevelData.LevelID);
            }
            
            Debug.Log("[TutorialManager] Hoàn thành Tutorial.");
        }
    }
}
