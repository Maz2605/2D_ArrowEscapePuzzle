using ArrowGame.UI.Base;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using UnityEngine;

namespace ArrowGame.Utils.Developer
{
    public class DevPopupTest : MonoBehaviour
    {
        public void PlayLosePopup()
        {
            UIManager.Instance.ShowPopup<BasePopup>(PopupID.LosePopup);            
        }
    }
}