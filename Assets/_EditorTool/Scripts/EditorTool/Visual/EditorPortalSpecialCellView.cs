using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class EditorPortalSpecialCellView : EditorSpecialCellViewBase
    {
        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            if (BackgroundRenderer != null) BackgroundRenderer.color = color;
            if (Label == null) return;

            string portalId = string.IsNullOrEmpty(specialCell.PortalId) ? "?" : specialCell.PortalId;
            Label.text = $"PORTAL\n[{portalId}]\n{specialCell.ExitDirection.ToGlyph()}";
        }
    }
}