using UnityEngine;

namespace ArrowGame.UI.Base
{
    public enum MainTabID
    {
        Shop = 0,
        Home = 1,
        Setting = 2
    }

    public abstract class BaseSubScreen : MonoBehaviour
    {
        protected bool isInitialized = false;

        public virtual void Init() 
        {
            isInitialized = true;
        }

        public virtual void Show() 
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide() 
        {
            gameObject.SetActive(false);
        }
    }
}