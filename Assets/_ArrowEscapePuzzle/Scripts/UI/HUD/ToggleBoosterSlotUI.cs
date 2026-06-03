using System;
using DG.Tweening;
using UnityEngine;

namespace ArrowGame.UI.HUD
{
    [Serializable]
    public class ToggleBoosterSlotUI : BoosterSlotUI
    {
        [Header("--- Toggle Visuals ---")]
        public RectTransform fillRect;      
        public CanvasGroup fillCanvasGroup; 
        
        [NonSerialized] public Tween pulseTween;
    }
}
