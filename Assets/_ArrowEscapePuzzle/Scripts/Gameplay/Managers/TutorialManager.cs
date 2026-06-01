using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    public class TutorialManager : Singleton<TutorialManager>
    {
        private LevelSaveData _currentLevelData;
        private int _currentStepIndex = -1;
        private TutorialOverlayUI _overlayUI;
        private bool _isTutorialActive;

        public bool IsTutorialActive => _isTutorialActive;

#if UNITY_EDITOR
        [Header("--- Editor Helper (Scanned Level Tutorials) ---")]
        [SerializeField] private List<string> levelsWithTutorials = new();

        private void OnValidate()
        {
            levelsWithTutorials.Clear();
            var levelAssets = Resources.LoadAll<TextAsset>("Levels");
            foreach (var asset in levelAssets)
            {
                if (asset == null) continue;
                try
                {
                    var levelData = Newtonsoft.Json.JsonConvert.DeserializeObject<LevelSaveData>(asset.text);
                    if (levelData != null && levelData.TutorialSteps != null && levelData.TutorialSteps.Count > 0)
                    {
                        levelsWithTutorials.Add($"{asset.name} ({levelData.TutorialSteps.Count} steps)");
                    }
                }
                catch
                {
                    // Ignore
                }
            }
        }
#endif

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

        private void OnLevelLoaded(LevelSaveData levelData)
        {
            _currentLevelData = levelData;
            _currentStepIndex = -1;
            _isTutorialActive = false;
            _overlayUI = null;

            if (levelData != null && levelData.TutorialSteps != null && levelData.TutorialSteps.Count > 0)
            {
                _isTutorialActive = true;
                _currentStepIndex = 0;
                Debug.Log($"[TutorialManager] Khởi động Tutorial cho {levelData.LevelID}.");
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
            if (_currentLevelData == null || _currentLevelData.TutorialSteps == null || _currentStepIndex >= _currentLevelData.TutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }

            TutorialStepData step = _currentLevelData.TutorialSteps[_currentStepIndex];

            // Hiển thị Popup overlay trên cùng
            if (_overlayUI == null)
            {
                _overlayUI = UIManager.Instance.ShowPopup<TutorialOverlayUI>(PopupID.TutorialOverlay);
            }

            if (_overlayUI != null && GameManager.Instance != null && GameManager.Instance.CurrentGridView != null)
            {
                Vector3 worldPos = GameManager.Instance.CurrentGridView.GetCellWorldPosition(step.TargetGridPos);
                _overlayUI.ShowStep(worldPos, step.TooltipText, step.ShowHandPointer);
            }
        }

        public bool IsGridActionAllowed(Vector2Int gridPos)
        {
            if (!_isTutorialActive) return true;
            if (_currentLevelData == null || _currentLevelData.TutorialSteps == null || _currentStepIndex >= _currentLevelData.TutorialSteps.Count)
            {
                return true;
            }

            // Chỉ cho phép thao tác nếu chạm đúng ô được highlight
            return _currentLevelData.TutorialSteps[_currentStepIndex].TargetGridPos == gridPos;
        }

        public void AdvanceToNextStep()
        {
            if (!_isTutorialActive) return;

            _currentStepIndex++;
            if (_currentLevelData != null && _currentLevelData.TutorialSteps != null && _currentStepIndex < _currentLevelData.TutorialSteps.Count)
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

            if (_currentLevelData != null)
            {
                DataManager.Instance.MarkTutorialCompleted(_currentLevelData.LevelID);
            }
            
            Debug.Log("[TutorialManager] Hoàn thành Tutorial.");
        }
    }
}
