using System;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Logic;
using UnityEngine;

namespace ArrowGame.Data.Booster
{
    public abstract class BoosterConfigSO : ScriptableObject
    {
        public BoosterType type;
        public string boosterName;
        public bool isTargeted; 
        public bool isConsumable = true; 
        public bool useBoosterInstructionPopup = true;
        [Min(1)] public int unlockLevel = 1;
        public bool showUnlockIntroduction = true;
        public string unlockTitle;
        [TextArea] public string unlockDescription;
        public int price = 0; 
        public Sprite boosterIcon;
        [TextArea] public string description;

        [Header("--- VFX Settings ---")]
        public VFXConfig vfxConfig; 

        public virtual bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty();
        }

        public virtual bool RequiresValidArrowTarget => isTargeted;

        public string GetUnlockTitle()
        {
            return string.IsNullOrWhiteSpace(unlockTitle) ? boosterName : unlockTitle;
        }

        public string GetUnlockDescription()
        {
            return string.IsNullOrWhiteSpace(unlockDescription) ? description : unlockDescription;
        }
    }
}
