using System;
using ArrowGame.Data.Events;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.Haptic
{
    public class HapticController : MonoBehaviour
    {
        private void OnEnable()
        {
            EventManager<VisualEventID>.AddListener(VisualEventID.ArrowWrongImpact, HandleWrongArrowImpact);
        }

        private void OnDisable()
        {
            EventManager<VisualEventID>.RemoveListener(VisualEventID.ArrowWrongImpact, HandleWrongArrowImpact);
        }
        private void HandleWrongArrowImpact()
        {
            HapticManager.Instance.LightVibrateImpact();
        }

    }
}