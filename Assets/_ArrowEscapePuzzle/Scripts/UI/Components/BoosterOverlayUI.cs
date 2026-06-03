using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Components
{
    public class BoosterOverlayUI : MonoBehaviour
    {
        public static BoosterOverlayUI Instance { get; private set; }

        [Header("--- References ---")]
        [SerializeField] private Image dimImage;

        [Header("--- Settings ---")]
        [SerializeField] private float targetAlpha = 0.7f;
        [SerializeField] private float fadeDuration = 0.3f;

        private void Awake()
        {
            Instance = this;

            if (dimImage != null)
            {
                Color c = dimImage.color;
                c.a = 0f;
                dimImage.color = c;
                dimImage.raycastTarget = false;
            }
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public void SetTargetMode(bool isSelecting)
        {
            SetVisible(isSelecting);
        }

        public void SetBoardDarken(bool isDarkened)
        {
            SetVisible(isDarkened);
        }

        private void SetVisible(bool isVisible)
        {
            if (dimImage == null) return;

            dimImage.DOKill();
            dimImage.DOFade(isVisible ? targetAlpha : 0f, fadeDuration).SetUpdate(true);
        }
    }
}
