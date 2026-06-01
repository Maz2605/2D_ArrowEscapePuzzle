using ArrowGame.Data.LevelProvider;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Popups;
using UnityEngine;

namespace ArrowGame.Gameplay.Tutorials
{
    public class Level_5_TutorialHandler : BaseTutorialHandler
    {
        private int _currentStepIndex = -1;
        private bool _isSubscribed;

        public override void Init(TutorialConfigSO config, TutorialOverlayUI overlay)
        {
            base.Init(config, overlay);
            SubscribeEvents();
        }

        public override void OnStepStarted(int stepIndex, TutorialStepConfig step)
        {
            _currentStepIndex = stepIndex;
            SubscribeEvents(); // Đảm bảo đã subscribe nếu lúc Init chưa thành công
        }

        private void SubscribeEvents()
        {
            if (_isSubscribed) return;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnZoomInput += HandleZoomInput;
            }

            if (GameManager.Instance != null && GameManager.Instance.InputController != null)
            {
                GameManager.Instance.InputController.OnCameraPanProcess += HandleCameraPanProcess;
                GameManager.Instance.InputController.OnCameraResetZoom += HandleCameraResetZoom;
                _isSubscribed = true;
            }
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed) return;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnZoomInput -= HandleZoomInput;
            }

            if (GameManager.Instance != null && GameManager.Instance.InputController != null)
            {
                GameManager.Instance.InputController.OnCameraPanProcess -= HandleCameraPanProcess;
                GameManager.Instance.InputController.OnCameraResetZoom -= HandleCameraResetZoom;
            }

            _isSubscribed = false;
        }

        private void HandleZoomInput(ZoomInputData data)
        {
            if (_currentStepIndex == 0) // Zoom step
            {
                TutorialManager.Instance.AdvanceToNextStep();
            }
        }

        private void HandleCameraPanProcess(Vector2 screenPos)
        {
            if (_currentStepIndex == 1) // Pan step
            {
                TutorialManager.Instance.AdvanceToNextStep();
            }
        }

        private void HandleCameraResetZoom()
        {
            if (_currentStepIndex == 2) // Reset Zoom step
            {
                TutorialManager.Instance.AdvanceToNextStep();
            }
        }

        public override bool IsGridActionAllowed(Vector2Int gridPos)
        {
            // Chặn hoàn toàn mọi bấm vào grid trong các bước camera
            return false;
        }

        public override void CleanUp()
        {
            base.CleanUp();
            UnsubscribeEvents();
        }
    }
}
