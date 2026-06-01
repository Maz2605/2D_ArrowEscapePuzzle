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

        public virtual void CleanUp()
        {
        }
    }
}
