using ArrowGame.Data.LevelProvider;
using ArrowGame.UI.Popups;
using UnityEngine;

namespace ArrowGame.Gameplay.Tutorials
{
    public abstract class BaseTutorialHandler
    {
        protected TutorialConfigSO _config;
        protected TutorialOverlayUI _overlay;

        public virtual void Init(TutorialConfigSO config, TutorialOverlayUI overlay)
        {
            _config = config;
            _overlay = overlay;
        }

        public virtual void OnStepStarted(int stepIndex, TutorialStepConfig step)
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual bool IsGridActionAllowed(Vector2Int gridPos)
        {
            return true;
        }

        public virtual bool TryGetCustomWorldPosition(int stepIndex, TutorialStepConfig step, out Vector3 worldPos, out float highlightSize, out Vector3 secondWorldPos, out float secondHighlightSize)
        {
            worldPos = Vector3.zero;
            highlightSize = 120f;
            secondWorldPos = Vector3.zero;
            secondHighlightSize = 120f;
            return false;
        }

        public virtual bool TryGetCustomUITarget(int stepIndex, TutorialStepConfig step, out Transform uiTarget, out float highlightSize, out Transform secondUiTarget, out float secondHighlightSize)
        {
            uiTarget = null;
            highlightSize = 120f;
            secondUiTarget = null;
            secondHighlightSize = 120f;
            return false;
        }

        public virtual void CleanUp()
        {
        }
    }
}
