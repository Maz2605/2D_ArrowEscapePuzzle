using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    public class GameStateManager : Singleton<GameStateManager>
    {
        public GameState CurrentState { get; private set; }

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;
            
            GameState oldState = CurrentState;
            CurrentState = newState;
            
            Debug.Log($"[GameStateManager] State Changed: {oldState} -> {newState}");
            EventManager<LogicGameEventID>.Post(LogicGameEventID.GameStateChanged, newState);
        }
    }
}
