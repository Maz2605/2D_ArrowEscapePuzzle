using ArrowGame.UI.Manager;
using DG.Tweening;

using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.Utils
{
    public class UITest : MonoBehaviour
    {
        [SerializeField] private Button btnTestToast;
        [SerializeField] private Button btnConfirmPopup;
        [SerializeField] private Button btnLoading;

        private void Start()
        {
            btnConfirmPopup.onClick.AddListener(HandleTestConfirmPopup);
            btnTestToast.onClick.AddListener(HandleTestToast);
            btnLoading.onClick.AddListener(HandleTestLoading);
        }

        private void HandleTestLoading()
        {
            UIManager.Instance.ShowToast("Loading...", 2f);
        }

        private void HandleTestConfirmPopup()
        {
            // UIManager.Instance.ShowConfirmation("Test",
            //     "Đây chỉ là test",
            //     () => UIManager.Instance.ShowToast("Đấm đúng rồi đó", 2f),
            //     () => UIManager.Instance.ShowToast("Đấm sai rồi đó"),
            //     "Đấm", "Không");
        }

        private void HandleTestToast()
        {
            UIManager.Instance.ShowToast("Test Toasttttttttttttttttttttttttttt tttttttttttttttttttttttttt ttttttttttttttttttttttttttttttttt", 3f);
        }
    }
 }
