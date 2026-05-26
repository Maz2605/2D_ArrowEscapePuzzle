using System.Collections.Generic;
using UnityEngine;
using GameCore.Utils.DesignPattern.ObjectPooling;

namespace ArrowGame.Gameplay.Visual
{
    public class PortalVisualMaster : MonoBehaviour, IPoolable
    {
        [Tooltip("Kéo thả các GameObject con (Đỏ, Xanh, Tím...) vào đây")]
        [SerializeField] private List<GameObject> visualVariants;
        
        public int VariantCount => visualVariants != null ? visualVariants.Count : 0;

        private GameObject _activeVariant;
        private SpriteRenderer[] _activeRenderers;
        private Color[] _activeBaseColors;

        // Hàm này được View gọi để chọn màu
        public void ShowVariant(int index)
        {
            // Tắt cái cũ
            if (_activeVariant != null) _activeVariant.SetActive(false);

            if (index < 0 || index >= visualVariants.Count) return;

            // Bật cái mới
            _activeVariant = visualVariants[index];
            _activeVariant.SetActive(true);

            // Cache lại renderer của cái đang bật để lát nữa đổi Alpha không bị lag
            _activeRenderers = _activeVariant.GetComponentsInChildren<SpriteRenderer>(true);
            _activeBaseColors = new Color[_activeRenderers.Length];
            for (int i = 0; i < _activeRenderers.Length; i++)
            {
                _activeBaseColors[i] = _activeRenderers[i].color;
            }
        }

        // View gọi hàm này để làm hiệu ứng mờ nhịp nhàng (Pulse)
        public void ApplyAlpha(float alphaMultiplier)
        {
            if (_activeRenderers == null) return;

            for (int i = 0; i < _activeRenderers.Length; i++)
            {
                if (_activeRenderers[i] == null) continue;
                Color baseColor = _activeBaseColors[i];
                _activeRenderers[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alphaMultiplier);
            }
        }

        public void OnSpawn()
        {
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        public void OnDespawn()
        {
            // Tắt hết đi để trả về Pool ở trạng thái sạch sẽ nhất
            if (_activeVariant != null)
            {
                _activeVariant.SetActive(false);
                _activeVariant = null;
            }
            _activeRenderers = null;
        }
    }
}