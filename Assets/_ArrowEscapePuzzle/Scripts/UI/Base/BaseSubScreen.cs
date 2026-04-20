using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Base
{
    public enum MainTabID
    {
        Shop = 0,
        Home = 1,
        Settings = 2
    }

    public abstract class BaseSubScreen : MonoBehaviour
    {
        protected bool isInitialized = false;

        protected void BindButton(Button btn, Action onClickAction)
        {
            if (btn == null) return;
            
            btn.onClick?.RemoveAllListeners();
            btn.onClick?.AddListener(() =>
            {
                btn.transform.DOKill();
                btn.transform.localScale = Vector3.one;
                btn.transform
                    .DOPunchScale(Vector3.one * -0.1f, 0.15f, 5) 
                    .SetUpdate(true)
                    .SetLink(btn.gameObject, LinkBehaviour.KillOnDisable)
                    .OnComplete(() => onClickAction?.Invoke());
            });
        }
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