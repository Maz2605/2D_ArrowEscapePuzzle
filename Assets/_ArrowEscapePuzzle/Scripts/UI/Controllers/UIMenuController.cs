using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.UI.Controllers
{
    public class UIMenuController : MonoBehaviour
    {
        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<GameState>(LogicGameEventID.GameStateChanged, HandleStateChanged);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<GameState>(LogicGameEventID.GameStateChanged, HandleStateChanged);
        }

        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.MainMenu || newState == GameState.Shop)
            {
                gameObject.SetActive(true); 
            }
            else
            {
                gameObject.SetActive(false); 
            }
        }
    }
}