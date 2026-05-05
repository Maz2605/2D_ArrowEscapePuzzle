using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace ArrowGame.VFX
{
    [RequireComponent(typeof(Image))]
    public class TapAuraVFX : MonoBehaviour, GameCore.Utils.DesignPattern.ObjectPooling.IPoolable
    {
        [SerializeField] private Image _image;
        [SerializeField] private RectTransform _rectTransform;
        
        [Header("Animation Settings")]
        [SerializeField] private float _targetScale = 1.5f;   
        [SerializeField] private float _duration = 0.4f;     
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Aura Layer Settings")]
        [SerializeField] private float _auraScaleMultiplier = 2.0f; 
        [SerializeField] private float _auraAlphaMultiplier = 0.5f;  
        [SerializeField] private float _auraDelay = 0.05f; 

        private void Awake()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            _rectTransform.localScale = Vector3.zero;
            
            Color resetColor = _image.color;
            resetColor.a = 0.75f;
            _image.color = resetColor;

            _rectTransform.DOScale(Vector3.one * _targetScale, _duration)
                .SetEase(_scaleCurve)
                .SetUpdate(true); // Ignore time scale in case game is paused

            _image.DOFade(0f, _duration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true);

            // Chỉ tạo duy nhất 1 lớp Aura tỏa ra to hơn và mờ hơn
            SpawnRippleLayer(_auraDelay, _targetScale * _auraScaleMultiplier, 0.75f * _auraAlphaMultiplier);
        }

        private void OnDisable()
        {
            // Kill tat ca tween tren object nay khi bi thu hoi vao pool
            _rectTransform.DOKill();
            _image.DOKill();
        }

        public void OnSpawn()
        {
            // Reset state neu can thiet
        }

        public void OnDespawn()
        {
            // Dung tat ca effect truoc khi vao pool
            _rectTransform.DOKill();
            _image.DOKill();
        }

        private void SpawnRippleLayer(float delay, float targetScale, float startAlpha)
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                if (this == null || gameObject == null || transform.parent == null) return;

                GameObject ripple = new GameObject("RippleLayer");
                ripple.transform.SetParent(transform.parent, false);
                
                RectTransform rect = ripple.AddComponent<RectTransform>();
                rect.localPosition = _rectTransform.localPosition;
                rect.sizeDelta = _rectTransform.sizeDelta;
                rect.localScale = Vector3.zero;

                Image img = ripple.AddComponent<Image>();
                img.sprite = _image.sprite;
                img.raycastTarget = false;
                
                Color c = _image.color;
                c.a = startAlpha;
                img.color = c;

                Sequence seq = DOTween.Sequence().SetLink(ripple).SetUpdate(true);
                seq.Append(rect.DOScale(Vector3.one * targetScale, _duration).SetEase(_scaleCurve));
                seq.Join(img.DOFade(0f, _duration).SetEase(Ease.InCubic));
                seq.OnComplete(() => { if (ripple != null) Destroy(ripple); });
            }).SetUpdate(true).SetLink(gameObject);
        }
    }
}