using UnityEngine;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Base;
using ArrowGame.UI.Popups;
using ArrowGame.UI.Screens;
using UnityEngine.InputSystem;

namespace ArrowGame.Test
{
    public class UITester : MonoBehaviour
    {
        private void Start()
        {
            // GIẢ LẬP FLOW VÀO GAME
            Debug.Log("[UITester] Khởi chạy game -> Bật GameMenuScreen");
            UIManager.Instance.ShowScreen<BaseScreen>(ScreenID.GameMenuScreen);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            // ==========================================
            //         TEST LAYER 1 (SCREENS)
            // ==========================================

            // Bấm Phím G để vào Gameplay
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                Debug.Log("[UITester] Chuyển sang GameplayScreen");
                UIManager.Instance.ShowScreen<BaseScreen>(ScreenID.GameplayScreen);
            }

            // Bấm Phím M để quay lại Menu
            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                Debug.Log("[UITester] Quay lại GameMenuScreen");
                UIManager.Instance.ShowScreen<BaseScreen>(ScreenID.GameMenuScreen);
            }

            // ==========================================
            //         TEST LAYER 2 (POPUPS)
            // ==========================================
            
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("[UITester] Bật Setting Popup");
                UIManager.Instance.ShowPopup<BasePopup>(PopupID.SettingPopup);
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Debug.Log("[UITester] Đóng Top Popup");
                UIManager.Instance.CloseTopPopup();
            }

            // ==========================================
            //         TEST LAYER 3 (TOP UI)
            // ==========================================

            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                Debug.Log("[UITester] Hiện Toast Notification");
                UIManager.Instance.ShowToast("Thử nghiệm UI thành công!", 2f);
            }

            if (Keyboard.current.lKey.wasPressedThisFrame)
            {
                Debug.Log("[UITester] Bật Loading Screen");
                UIManager.Instance.ShowLoading();
                Invoke(nameof(HideLoadingTest), 2f);
            }
        }

        private void HideLoadingTest()
        {
            Debug.Log("[UITester] Tắt Loading Screen");
            UIManager.Instance.HideLoading();
        }
    }
}