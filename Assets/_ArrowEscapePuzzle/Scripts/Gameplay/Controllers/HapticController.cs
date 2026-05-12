using System;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Logic;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.Haptic
{
    public class HapticController : MonoBehaviour
    {
        private void OnEnable()
        {
            EventManager<VisualEventID>.AddListener<Vector3>(VisualEventID.ArrowWrongImpact, HandleWrongArrowImpact);
            
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelComplete, HandleLevelComplete);
            EventManager<LogicGameEventID>.AddListener(LogicGameEventID.LevelFailed, HandleLevelFailed);
            EventManager<LogicGameEventID>.AddListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
        }

        private void OnDisable()
        {
            EventManager<VisualEventID>.RemoveListener<Vector3>(VisualEventID.ArrowWrongImpact, HandleWrongArrowImpact);
            
            EventManager<LogicGameEventID>.RemoveListener(LogicGameEventID.LevelComplete, HandleLevelComplete);
            EventManager<LogicGameEventID>.RemoveListener(LogicGameEventID.LevelFailed, HandleLevelFailed);
            EventManager<LogicGameEventID>.RemoveListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
        }

        private void HandleWrongArrowImpact(Vector3 impactPosition)
        {
            HapticManager.Instance.LightVibrateImpact();
        }

        private void HandleLevelComplete()
        {
            HapticManager.Instance.Success();
        }

        private void HandleLevelFailed()
        {
            HapticManager.Instance.Failure();
        }

        private void HandleArrowBlocked(ArrowData data)
        {
            HapticManager.Instance.Failure();
        }
    }
}
