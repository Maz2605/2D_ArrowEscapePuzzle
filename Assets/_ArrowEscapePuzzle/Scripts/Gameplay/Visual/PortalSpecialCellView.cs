using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public class PortalSpecialCellView : SpecialCellViewBase
    {
        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            if (BackgroundRenderer != null) BackgroundRenderer.color = color;
            if (Label == null) return;

            string portalLabel = string.IsNullOrEmpty(specialCell.PortalId) ? "P" : $"P{specialCell.PortalId}";
            Label.text = $"{portalLabel}\n{specialCell.ExitDirection.ToGlyph()}";
        }
    }
}
