using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    /// <summary>
    /// Visual tạm thời cho ô Mystery Box (hộp bí ẩn).
    /// Hiển thị một SpriteRenderer đơn giản để test gameplay.
    /// </summary>
    public class MysteryBoxSpecialCellView : SpecialCellViewBase
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer iconRenderer;

        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            if (iconRenderer != null)
            {
                iconRenderer.color = color;
            }
        }

        protected override void OnVisualColorChanged(Color color)
        {
            if (iconRenderer != null)
            {
                iconRenderer.color = color;
            }
        }

        protected override void OnDespawnedToPool()
        {
            if (iconRenderer != null)
            {
                iconRenderer.color = Color.white;
            }
        }
    }
}
