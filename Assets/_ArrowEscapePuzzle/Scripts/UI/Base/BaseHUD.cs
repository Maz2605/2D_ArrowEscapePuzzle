using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Base
{
    public class BaseHUD : MonoBehaviour
    {
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
                    .SetLink(btn.gameObject)
                    .OnComplete(() => onClickAction?.Invoke());
            });
        }
    }
}