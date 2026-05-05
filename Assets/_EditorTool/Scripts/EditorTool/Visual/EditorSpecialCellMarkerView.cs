using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public abstract class EditorSpecialCellViewBase : MonoBehaviour
    {
        private static Sprite _sharedSprite;

        protected SpriteRenderer _backgroundRenderer;
        protected TextMeshPro _label;

        protected virtual void Awake()
        {
            BuildVisualHierarchy();
        }

        public void Setup(SpecialCellSaveData specialCell, Color color)
        {
            if (specialCell == null) return;

            // Đặt object nổi lên trên lưới một chút (Z = -0.05f) để không bị đè
            transform.localPosition = new Vector3(specialCell.Position.x, specialCell.Position.y, -0.05f);
            transform.localScale = Vector3.one * DefaultScaleMultiplier;
            
            ApplyVisual(specialCell, color);
        }

        /// <summary>
        /// Khởi tạo toàn bộ cấu trúc UI bằng code (Procedural Setup).
        /// Đảm bảo tính nhất quán 100%, không phụ thuộc vào việc kéo thả Inspector.
        /// </summary>
       /// <summary>
        /// Khởi tạo toàn bộ cấu trúc UI bằng code (Procedural Setup).
        /// </summary>
        private void BuildVisualHierarchy()
        {
            // 1. Setup Background (SpriteRenderer) ở Root
            _backgroundRenderer = GetComponent<SpriteRenderer>();
            if (_backgroundRenderer == null)
            {
                _backgroundRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
            
            _backgroundRenderer.sprite = GetSharedSprite();
            _backgroundRenderer.sortingOrder = DefaultSortingOrder;

            // 2. Setup Label (TextMeshPro) ở Object con
            Transform labelTransform = transform.Find("Label");
            if (labelTransform == null)
            {
                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(transform, false);
                _label = labelObject.AddComponent<TextMeshPro>();
            }
            else
            {
                _label = labelTransform.GetComponent<TextMeshPro>();
                if (_label == null) _label = labelTransform.gameObject.AddComponent<TextMeshPro>();
            }

            // 3. Force Configurations (Ép cấu hình chuẩn cho ô Grid 1x1)
            RectTransform rectTransform = _label.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(1f, 1f); // Tận dụng toàn bộ khung 1x1
                rectTransform.localPosition = Vector3.zero;    
                rectTransform.localScale = Vector3.one;
            }

            // ==========================================
            // TỐI ƯU TEXTMESHPRO AUTO-SIZE CHO WORLD SPACE
            // ==========================================
            _label.alignment = TextAlignmentOptions.Center;
            _label.enableAutoSizing = true;
            _label.fontSizeMin = 0.05f; 
            _label.fontSizeMax = 2f;    // Thả trần để TMP tự scale fit với Box 1x1
            
            // Tắt Wrap để chữ PORTAL không bị rớt dòng ép scale nhỏ
            _label.enableWordWrapping = false; 
            _label.overflowMode = TextOverflowModes.Truncate; 
            
            _label.margin = Vector4.zero; // Xóa margin, dùng 100% diện tích
            _label.lineSpacing = -25f;    // Bóp khoảng cách giữa 3 dòng lại để không bị tràn chiều dọc
            
            _label.color = Color.white;
            _label.sortingOrder = DefaultSortingOrder + 1; 
        }

        protected SpriteRenderer BackgroundRenderer => _backgroundRenderer;
        protected TextMeshPro Label => _label;
        protected virtual int DefaultSortingOrder => 6;
        protected virtual float DefaultScaleMultiplier => 0.72f;
        
        protected abstract void ApplyVisual(SpecialCellSaveData specialCell, Color color);

        /// <summary>
        /// Tạo một texture trắng tinh 1x1 pixel lưu vào bộ nhớ chung (Shared).
        /// Giúp tiết kiệm mem và tránh tạo nhiều texture rác.
        /// </summary>
        private static Sprite GetSharedSprite()
        {
            if (_sharedSprite != null) return _sharedSprite;

            Texture2D texture = Texture2D.whiteTexture;
            Rect rect = new Rect(0f, 0f, texture.width, texture.height);
            _sharedSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), texture.width);
            return _sharedSprite;
        }
    }
}