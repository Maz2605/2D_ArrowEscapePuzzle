using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.UI.Base;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.UI.Controllers
{
    public class UIGameplayController : MonoBehaviour
    {
        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<GameState>(LogicGameEventID.GameStateChanged, HandleStateChanged);
            EventManager<VisualEventID>.AddListener(VisualEventID.WinAnimationComplete, HandleWinAnimationComplete);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<GameState>(LogicGameEventID.GameStateChanged, HandleStateChanged);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.WinAnimationComplete, HandleWinAnimationComplete);
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.Playing || newState == GameState.Paused || newState == GameState.IntroLevel)
            {
                gameObject.SetActive(true); 
            }
            else
            {
                gameObject.SetActive(false); 
            }
        }

        private void HandleWinAnimationComplete()
        {
            // UIManager.Instance.ShowPopup<BasePopup>(PopupID.WinPopup);
        }
    }
}