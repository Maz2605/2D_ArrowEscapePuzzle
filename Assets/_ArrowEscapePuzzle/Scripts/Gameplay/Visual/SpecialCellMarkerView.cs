using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public abstract class SpecialCellViewBase : MonoBehaviour
    {
        private static Sprite _sharedSprite;

        [SerializeField] private SpriteRenderer _backgroundRenderer;
        [SerializeField] private TextMesh _label;

        protected virtual void Awake()
        {
            EnsureReferences();
        }

        protected virtual void EnsureReferences()
        {
            if (_backgroundRenderer == null) _backgroundRenderer = GetComponent<SpriteRenderer>();
            if (_backgroundRenderer == null) _backgroundRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (_backgroundRenderer != null)
            {
                if (_backgroundRenderer.sprite == null) _backgroundRenderer.sprite = GetSharedSprite();
                _backgroundRenderer.sortingOrder = DefaultSortingOrder;
            }

            if (_label != null) return;

            Transform labelTransform = transform.Find("Label");
            if (labelTransform != null) _label = labelTransform.GetComponent<TextMesh>();

            if (_label == null)
            {
                GameObject labelObject = new GameObject("Label");
                labelObject.transform.SetParent(transform, false);
                _label = labelObject.AddComponent<TextMesh>();
            }

            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 48;
            _label.characterSize = 0.075f;
            _label.color = Color.white;
        }

        public void Setup(SpecialCellSaveData specialCell, float cellSize, Color color)
        {
            if (specialCell == null) return;
            EnsureReferences();

            transform.localPosition = new Vector3(specialCell.Position.x * cellSize, specialCell.Position.y * cellSize, 0f);
            transform.localScale = Vector3.one * (cellSize * DefaultScaleMultiplier);
            ApplyVisual(specialCell, color);
        }

        protected SpriteRenderer BackgroundRenderer => _backgroundRenderer;
        protected TextMesh Label => _label;
        protected virtual int DefaultSortingOrder => -1;
        protected virtual float DefaultScaleMultiplier => 0.65f;
        protected abstract void ApplyVisual(SpecialCellSaveData specialCell, Color color);

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
