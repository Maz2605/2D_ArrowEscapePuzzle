using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class EditorRedirectSpecialCellView : EditorSpecialCellViewBase
    {
        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            if (BackgroundRenderer != null) BackgroundRenderer.color = color;
            float rotation = specialCell.ExitDirection switch
            {
                Direction4.Up => 0f,
                Direction4.Right => -90f,
                Direction4.Down => 180f,
                Direction4.Left => 90f,
                _ => 0f
            };
            transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            if (Label != null)
            {
                Label.gameObject.SetActive(false);
            }
        }
        
    }
}