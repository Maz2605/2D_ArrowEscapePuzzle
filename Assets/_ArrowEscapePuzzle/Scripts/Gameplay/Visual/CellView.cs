using ArrowGame.Gameplay.Logic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CellView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Assets")]
        [SerializeField] private Sprite sprHead;
        [SerializeField] private Sprite sprBodyStraight;
        [SerializeField] private Sprite sprBodyCurve;
        [SerializeField] private Sprite sprTail;
        [SerializeField] private Sprite sprEmpty;

        public string ArrowID { get; private set; }
        private CellType _currentType;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void SetupVisual(ArrowData data)
        {
            ArrowID = data.ID;
            _currentType = data.Type;
            
            spriteRenderer.enabled = true; 
            spriteRenderer.color = Color.white; 
            transform.localScale = Vector3.one; 

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var (targetSprite, angleZ) = GetVisualProps(_currentType);

            if (targetSprite == null && _currentType != CellType.None)
            {
                spriteRenderer.enabled = (_currentType == CellType.EmptyDot);
                spriteRenderer.sprite = sprEmpty;
                return;
            }

            spriteRenderer.sprite = targetSprite;
            transform.rotation = Quaternion.Euler(0, 0, angleZ);
        }

        private (Sprite sprite, float rotation) GetVisualProps(CellType type)
        {
            return type switch
            {
                CellType.ArrowHeadUp    => (sprHead, 0f),
                CellType.ArrowHeadRight => (sprHead, -90f),
                CellType.ArrowHeadDown  => (sprHead, 180f),
                CellType.ArrowHeadLeft  => (sprHead, 90f),
                
                CellType.ArrowBodyVertical   => (sprBodyStraight, 0f),
                CellType.ArrowBodyHorizontal => (sprBodyStraight, 90f),
                
                CellType.ArrowCurveTopRight    => (sprBodyCurve, 0f),
                CellType.ArrowCurveBottomRight => (sprBodyCurve, -90f),
                CellType.ArrowCurveBottomLeft  => (sprBodyCurve, 180f),
                CellType.ArrowCurveTopLeft     => (sprBodyCurve, 90f),
                
                CellType.ArrowTailUp    => (sprTail, 0f),
                CellType.ArrowTailRight => (sprTail, -90f),
                CellType.ArrowTailDown  => (sprTail, 180f),
                CellType.ArrowTailLeft  => (sprTail, 90f),
                
                CellType.EmptyDot => (sprEmpty, 0f),
                _ => (null, 0f)
            };
        }

        public void PlayEscapeAnimation()
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
            spriteRenderer.sprite = sprEmpty;
        }

        public void PlayBlockedAnimation()
        {
            spriteRenderer.color = Color.red;
            Invoke(nameof(ResetVisualState), 0.15f);
        }

        private void ResetVisualState()
        {
            spriteRenderer.color = Color.white;
            UpdateDisplay(); // Gọi lại hàm này cho chắc ăn thay vì chỉ đổi màu
        }
    }
}