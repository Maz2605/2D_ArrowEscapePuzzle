using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public class RedirectSpecialCellView : SpecialCellViewBase
    {
        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            if (BackgroundRenderer != null) BackgroundRenderer.color = color;
            if (Label == null) return;
            Label.text = $"R{specialCell.ExitDirection.ToGlyph()}";
        }
    }
}
