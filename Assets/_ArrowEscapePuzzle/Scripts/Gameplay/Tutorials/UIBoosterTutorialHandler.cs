using System.Linq;
using ArrowGame.Data.Booster;
using ArrowGame.Data.LevelProvider;
using ArrowGame.UI.HUD;
using UnityEngine;

namespace ArrowGame.Gameplay.Tutorials
{
    public abstract class UIBoosterTutorialHandler : BaseTutorialHandler
    {
        protected abstract BoosterType TargetBooster { get; }
        private int _currentStepIndex = -1;

        public override void OnStepStarted(int stepIndex, TutorialStepConfig step)
        {
            _currentStepIndex = stepIndex;
            if (stepIndex == 0)
            {
                GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.LogicGameEventID>.AddListener<ArrowGame.Data.States.InGameState>(ArrowGame.Data.Events.LogicGameEventID.InGameStateChanged, OnStateChanged);
            }
        }

        public override void CleanUp()
        {
            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.LogicGameEventID>.RemoveListener<ArrowGame.Data.States.InGameState>(ArrowGame.Data.Events.LogicGameEventID.InGameStateChanged, OnStateChanged);
            base.CleanUp();
        }

        private void OnStateChanged(ArrowGame.Data.States.InGameState newState)
        {
            if (newState == ArrowGame.Data.States.InGameState.WaitingBoosterTarget || 
                newState == ArrowGame.Data.States.InGameState.BoosterInstruction || 
                newState == ArrowGame.Data.States.InGameState.BoosterExecuting)
            {
                if (ArrowGame.Gameplay.Managers.TutorialManager.Instance != null && ArrowGame.Gameplay.Managers.TutorialManager.Instance.IsTutorialActive)
                {
                    ArrowGame.Gameplay.Managers.TutorialManager.Instance.AdvanceToNextStep();
                }
            }
        }

        public override bool TryGetCustomUITarget(int stepIndex, TutorialStepConfig step, out Transform uiTarget, out float highlightSize, out Transform secondUiTarget, out float secondHighlightSize)
        {
            uiTarget = null;
            highlightSize = 130f; // UI is typically smaller, 130 is a good size for a button
            secondUiTarget = null;
            secondHighlightSize = 120f;

            if (TargetBooster == BoosterType.None) return false;

            var slots = Object.FindObjectsByType<BoosterSlotUI>(FindObjectsSortMode.None);
            var targetSlot = slots.FirstOrDefault(s => s.type == TargetBooster);

            if (targetSlot != null)
            {
                uiTarget = targetSlot.iconImage != null ? targetSlot.iconImage.transform : targetSlot.transform;
                return true;
            }

            return false;
        }

        public override bool IsGridActionAllowed(Vector2Int gridPos)
        {
            // Block all grid actions while guiding the user to click the booster button UI
            return false;
        }
    }

    public class Hint_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.Hint;
    }

    public class Gate_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.Gate;
    }

    public class Ufo_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.Ufo;
    }

    public class Lightning_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.Lightning;
    }

    public class LineGuide_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.LineGuide;
    }

    public class ArrowDash_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.ArrowDash;
    }

    // Default fallbacks cho các Level theo yêu cầu (Level 4 = LineGuide)
    public class Level_4_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.LineGuide;
    }

    public class Level_7_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.Hint;
    }

    public class Level_12_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.Gate;
    }

    public class Level_17_TutorialHandler : UIBoosterTutorialHandler
    {
        protected override BoosterType TargetBooster => BoosterType.ArrowDash;
    }
}
