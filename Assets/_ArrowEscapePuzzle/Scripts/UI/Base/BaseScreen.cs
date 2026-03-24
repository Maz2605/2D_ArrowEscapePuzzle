using UnityEngine;

namespace ArrowGame.UI.Base
{
    public abstract class BaseScreen : MonoBehaviour
    {
        public virtual void Show()
        {
            gameObject.SetActive(true);
            OnShow();
        }
        
        public virtual void Hide()
        {
            gameObject.SetActive(false);
            OnHide();
        }
        
        protected abstract void OnShow();
        protected abstract void OnHide();
    }
}