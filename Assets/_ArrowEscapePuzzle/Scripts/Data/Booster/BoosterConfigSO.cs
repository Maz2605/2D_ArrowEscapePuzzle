using System;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Logic;
using DG.Tweening;
using UnityEngine;

namespace ArrowGame.Data.Booster
{
    public abstract class BoosterConfigSO : ScriptableObject
    {
        public BoosterType type;
        public string boosterName;
        public bool isTargeted; 
        public bool isConsumable = true; 
        public int price = 0; 
        public Sprite boosterIcon;
        [TextArea] public string description;

        [Header("--- VFX Settings ---")]
        public VFXConfig vfxConfig; 

        public virtual bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty();
        }

        public abstract void Execute(GridSystem gridLogic, int targetX, int targetY, Sequence seq, Action onComplete);
    }
}