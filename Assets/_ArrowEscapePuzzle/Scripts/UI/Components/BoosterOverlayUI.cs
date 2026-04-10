using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ArrowGame.Data.Events;
using GameCore.Utils.DesignPattern.Events;

namespace ArrowGame.UI.Components
{
    public class BoosterOverlayUI : MonoBehaviour
    {
        [Header("--- References ---")]
        [SerializeField] private Image dimImage; // Kéo ảnh đen vào đây

        [Header("--- Settings ---")]
        [SerializeField] private float targetAlpha = 0.7f;
        [SerializeField] private float fadeDuration = 0.3f;

        private void Awake()
        {
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
            EventManager<VisualEventID>.AddListener<bool>(VisualEventID.BoosterTargetModeChanged, OnTargetModeChanged);
        }

        private void OnDisable()
        {
            EventManager<VisualEventID>.RemoveListener<bool>(VisualEventID.BoosterTargetModeChanged, OnTargetModeChanged);
        }

        private void OnTargetModeChanged(bool isSelecting)
        {
            if (dimImage == null) return;

            dimImage.DOKill();

            if (isSelecting)
            {
                dimImage.DOFade(targetAlpha, fadeDuration).SetUpdate(true);
            }
            else
            {
                dimImage.DOFade(0f, fadeDuration).SetUpdate(true);
            }
        }
    }
}