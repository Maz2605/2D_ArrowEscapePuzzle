using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class EditorCounterBlockView : EditorSpecialCellViewBase
    {
        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            if (BackgroundRenderer != null) BackgroundRenderer.color = color;
            if (Label == null) return;

            Label.text = $"COUNT\n[{specialCell.Counter}]";
        }
    }
}
