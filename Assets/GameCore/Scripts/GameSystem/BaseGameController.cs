using GameCore.SceneFlow;
using GameCore.UI.Manager;
using UnityEngine;

namespace GameCore.GameSystem
{
    public abstract class BaseGameController : MonoBehaviour
    {
        [SerializeField] private string mainSceneName = "MainMenu";

        protected abstract void OnResetGameplay();

        protected virtual void OnResumeGameplay()
        {
            Debug.Log("Resume Gameplay");
        }

        protected void RequestReplay()
        {
            UIManager.Instance.ShowConfirmation("Replay", "Replay game?", 
                OnResetGameplay, null, "Yes", "No");
        }

        protected void RequestPause() => UIManager.Instance.OpenSettings(OnResumeGameplay);

        protected void RequestBackHome()
        {
            UIManager.Instance.ShowConfirmation("Quit", "Back to Home?", 
                () => SceneLoader.Instance.LoadScene(mainSceneName), null);
        }

        protected void RequestBackHomeWithoutConfirmation()
        {
            SceneLoader.Instance.LoadScene(mainSceneName);
        }

        protected void RequestReplayWithoutConfirmation()
        {
            OnResetGameplay();
        }
    }
}