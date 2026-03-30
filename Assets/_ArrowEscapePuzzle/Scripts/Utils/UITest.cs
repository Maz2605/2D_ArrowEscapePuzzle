using UnityEngine;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Base;
using ArrowGame.UI.Popups;
using ArrowGame.UI.Screens;

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
            // ==========================================
            //         TEST LAYER 1 (SCREENS)
            // ==========================================

            // Bấm Phím G để vào Gameplay
            if (Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("[UITester] Chuyển sang GameplayScreen");
                UIManager.Instance.ShowScreen<BaseScreen>(ScreenID.GameplayScreen);
            }

            // Bấm Phím M để quay lại Menu
            if (Input.GetKeyDown(KeyCode.M))
            {
                Debug.Log("[UITester] Quay lại GameMenuScreen");
                UIManager.Instance.ShowScreen<BaseScreen>(ScreenID.GameMenuScreen);
            }

            // ==========================================
            //         TEST LAYER 2 (POPUPS)
            // ==========================================
            
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[UITester] Bật Setting Popup");
                UIManager.Instance.ShowPopup<BasePopup>(PopupID.SettingPopup);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[UITester] Đóng Top Popup");
                UIManager.Instance.CloseTopPopup();
            }

            // ==========================================
            //         TEST LAYER 3 (TOP UI)
            // ==========================================

            if (Input.GetKeyDown(KeyCode.T))
            {
                Debug.Log("[UITester] Hiện Toast Notification");
                UIManager.Instance.ShowToast("Thử nghiệm UI thành công!", 2f);
            }

            if (Input.GetKeyDown(KeyCode.L))
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