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
using DG.Tweening;
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

        public bool IsCameraStep()
        {
            if (!_isTutorialActive || _currentTutorialConfig == null || _currentStepIndex < 0 || _currentStepIndex >= _currentTutorialConfig.steps.Count)
                return false;
            var step = _currentTutorialConfig.steps[_currentStepIndex];
            return step.targetGridPos.x < 0 || step.targetGridPos.y < 0;
        }

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<LevelSaveData>(LogicGameEventID.LevelLoaded, OnLevelLoaded);
            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.AddListener(ArrowGame.Data.Events.VisualEventID.AllBoosterIntroductionsCompleted, OnAllBoosterIntroductionsCompleted);
        }

        private void OnDisable()
        {
            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.LogicGameEventID>.RemoveListener<LevelSaveData>(ArrowGame.Data.Events.LogicGameEventID.LevelLoaded, OnLevelLoaded);
            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.RemoveListener(ArrowGame.Data.Events.VisualEventID.AllBoosterIntroductionsCompleted, OnAllBoosterIntroductionsCompleted);
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


        private void OnAllBoosterIntroductionsCompleted()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentInGameState == InGameState.Playing && _isTutorialActive && _currentStepIndex == 0)
            {
                TryShowFirstStep();
            }
        }

        private void TryShowFirstStep()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentInGameState != InGameState.Playing || !_isTutorialActive) return;

            var cam = GameManager.Instance.CurrentCameraController;
            if (cam != null && cam.IsIntroZooming)
            {
                DG.Tweening.DOVirtual.DelayedCall(0.1f, TryShowFirstStep).SetLink(gameObject);
                return;
            }

            ShowCurrentStep();
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
                    // Ưu tiên handlerTypeName từ SO nếu có, fallback về convention LevelID_TutorialHandler
                    string handlerTypeName = !string.IsNullOrEmpty(_currentTutorialConfig?.handlerTypeName)
                        ? $"ArrowGame.Gameplay.Tutorials.{_currentTutorialConfig.handlerTypeName}"
                        : $"ArrowGame.Gameplay.Tutorials.{_currentLevelData.LevelID}_TutorialHandler";

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
                        _currentHandler = new DefaultTutorialHandler();
                        _currentHandler.Init(_currentTutorialConfig, _overlayUI);
                    }
                }

                // Kích hoạt bước hiện tại trong Handler
                if (_currentHandler != null)
                {
                    _currentHandler.OnStepStarted(_currentStepIndex, step);
                }

                if (_overlayUI != null && !_overlayUI.gameObject.activeSelf)
                {
                    _overlayUI.gameObject.SetActive(true);
                }

                Vector3 worldPos = Vector3.zero;
                float highlightSize = 120f;
                Vector3 secondWorldPos = Vector3.zero;
                float secondHighlightSize = 120f;

                bool isCustomUI = false;
                Transform uiTarget = null;
                Transform secondUiTarget = null;

                if (_currentHandler != null)
                {
                    isCustomUI = _currentHandler.TryGetCustomUITarget(_currentStepIndex, step, out uiTarget, out highlightSize, out secondUiTarget, out secondHighlightSize);
                }

                bool isCustomPos = false;
                if (!isCustomUI && _currentHandler != null)
                {
                    isCustomPos = _currentHandler.TryGetCustomWorldPosition(_currentStepIndex, step, out worldPos, out highlightSize, out secondWorldPos, out secondHighlightSize);
                }

                if (!isCustomUI && !isCustomPos && GameManager.Instance != null && GameManager.Instance.CurrentGridView != null)
                {
                    var gridLogic = GameManager.Instance.GridLogic;
                    if (gridLogic != null)
                    {
                        var specialCell = gridLogic.GetSpecialCellAt(step.targetGridPos.x, step.targetGridPos.y);
                        if (specialCell != null && specialCell.Type == ShareCore.Data.BoardSpecialType.CounterBlock)
                        {
                            var occupied = ShareCore.Scripts.Data.CounterBlockUtility.GetOccupiedPositions(specialCell);
                            Vector2 sumPos = Vector2.zero;
                            int count = 0;
                            foreach (var p in occupied)
                            {
                                sumPos += new Vector2(p.x, p.y);
                                count++;
                            }
                            Vector2 centerGridPos = count > 0 ? sumPos / count : new Vector2(step.targetGridPos.x, step.targetGridPos.y);
                            worldPos = GameManager.Instance.CurrentGridView.GetCellWorldPosition(centerGridPos);
                            highlightSize = 220f;
                        }
                        else
                        {
                            worldPos = GameManager.Instance.CurrentGridView.GetCellWorldPosition(step.targetGridPos);
                        }

                        if (step.hasSecondTarget)
                        {
                            var specialCell2 = gridLogic.GetSpecialCellAt(step.secondTargetGridPos.x, step.secondTargetGridPos.y);
                            if (specialCell2 != null && specialCell2.Type == ShareCore.Data.BoardSpecialType.CounterBlock)
                            {
                                var occupied2 = ShareCore.Scripts.Data.CounterBlockUtility.GetOccupiedPositions(specialCell2);
                                Vector2 sumPos2 = Vector2.zero;
                                int count2 = 0;
                                foreach (var p in occupied2)
                                {
                                    sumPos2 += new Vector2(p.x, p.y);
                                    count2++;
                                }
                                Vector2 centerGridPos2 = count2 > 0 ? sumPos2 / count2 : new Vector2(step.secondTargetGridPos.x, step.secondTargetGridPos.y);
                                secondWorldPos = GameManager.Instance.CurrentGridView.GetCellWorldPosition(centerGridPos2);
                                secondHighlightSize = 220f;
                            }
                            else
                            {
                                secondWorldPos = GameManager.Instance.CurrentGridView.GetCellWorldPosition(step.secondTargetGridPos);
                            }
                        }
                    }
                    else
                    {
                        worldPos = GameManager.Instance.CurrentGridView.GetCellWorldPosition(step.targetGridPos);
                        if (step.hasSecondTarget)
                        {
                            secondWorldPos = GameManager.Instance.CurrentGridView.GetCellWorldPosition(step.secondTargetGridPos);
                        }
                    }
                }
                
                if (isCustomUI)
                {
                    _overlayUI.ShowStep(Vector3.zero, step.tooltipText, step.showHandPointer, highlightSize, uiTarget != null && secondUiTarget != null, Vector3.zero, secondHighlightSize, true, uiTarget, secondUiTarget);
                }
                else
                {
                    _overlayUI.ShowStep(worldPos, step.tooltipText, step.showHandPointer, highlightSize, step.hasSecondTarget, secondWorldPos, secondHighlightSize, false, null, null);
                }
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
                _overlayUI.Hide();
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
