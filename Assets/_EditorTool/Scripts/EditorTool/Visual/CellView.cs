using ShareCore.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _bgRenderer;

        public void InitPosition(int x, int y)
        {
            gameObject.name = $"Cell_{x}_{y}"; 
        }

        public void UpdateVisual(CellData data)
        {
            if (data == null) return;

            if (data.type == CellType.None)
            {
                _bgRenderer.color = new Color(0.1f, 0.1f, 0.1f); // Hố sâu đen xám
            }
            else
            {
                // Tất cả các ô còn lại (EmptyDot hoặc có Mũi tên đi qua) đều vẽ màu nền xám
                _bgRenderer.color = new Color(0.8f, 0.8f, 0.8f);
            }
        }
    }
}