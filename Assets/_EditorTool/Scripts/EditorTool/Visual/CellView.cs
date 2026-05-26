using EditorTool.Scripts.Data;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _bgRenderer;

        private static readonly Color DefaultCellColor = new Color(0.22f, 0.25f, 0.32f, 1f);
        private TextMesh _label;

        private void Awake()
        {
            if (_bgRenderer == null) _bgRenderer = GetComponent<SpriteRenderer>();

            GameObject labelObject = new GameObject("CellLabel");
            labelObject.transform.SetParent(transform, false);
            _label = labelObject.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 48;
            _label.characterSize = 0.065f;
            _label.color = Color.white;
        }

        public void InitPosition(int x, int y)
        {
            gameObject.name = $"Cell_{x}_{y}";
        }

        public void UpdateVisual(CellData data, SpecialCellSaveData specialCell)
        {
            if (data == null) return;

            if (!string.IsNullOrEmpty(data.arrowID))
            {
                _bgRenderer.color = Color.Lerp(DefaultCellColor, EditorConstants.GetArrowColor(data.arrowID), 0.16f);
                _label.text = string.Empty;
                return;
            }

            if (specialCell != null)
            {
                if (specialCell.Type == BoardSpecialType.Redirect)
                {
                    _bgRenderer.color = new Color(0.95f, 0.73f, 0.16f, 1f);
                    _label.text = $"R\n{specialCell.ExitDirection.ToGlyph()}";
                }
                else if (specialCell.Type == BoardSpecialType.CounterBlock)
                {
                    _bgRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                    _label.text = $"C\n[{specialCell.Counter}]";
                }
                else
                {
                    string portalId = PortalVisualUtility.NormalizePortalId(specialCell.PortalId);
                    _bgRenderer.color = PortalVisualUtility.GetPortalColor(portalId);
                    _label.text = $"P{portalId}\n{specialCell.PortalDirection.ToGlyph()}";
                }
                return;
            }

            _bgRenderer.color = DefaultCellColor;
            _label.text = string.Empty;
        }
    }
}
