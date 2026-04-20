using UnityEngine;
using DG.Tweening;

namespace ArrowGame.VFX
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TapAuraVFX : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        [Header("Animation Settings")]
        [SerializeField] private float _targetScale = 2.5f; // Độ to tối đa
        [SerializeField] private float _duration = 0.4f;    // Thời gian hiệu ứng

        private void Awake()
        {
            if (_spriteRenderer == null) 
                _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            // 1. Reset lại trạng thái ban đầu (Rất quan trọng khi dùng Object Pooling)
            transform.localScale = Vector3.zero; // Bắt đầu từ mức 0
            
            Color resetColor = _spriteRenderer.color;
            resetColor.a = 0.6f; // Alpha ban đầu (hơi trong suốt một chút)
            _spriteRenderer.color = resetColor;

            // 2. Chạy Animation với DOTween
            // Phóng to ra
            transform.DOScale(Vector3.one * _targetScale, _duration)
                .SetEase(Ease.OutQuad); // OutQuad giúp nó phình ra nhanh lúc đầu rồi chậm lại

            // Mờ dần đi
            _spriteRenderer.DOFade(0f, _duration)
                .SetEase(Ease.InQuad);
        }
        
        // Lưu ý: Chúng ta không cần viết hàm Destroy/Despawn ở đây 
        // vì GlobalVFXManager của bạn đã tự động thu hồi nó sau thời gian duration rồi.
    }
}