using DG.Tweening;
using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public class CounterBlockView : SpecialCellViewBase
    {
        private const float LabelZOffset = -0.05f;
        private const float MeshCellSize = 1f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Material s_sharedMeshMaterial;
        
        private static readonly AnimationCurve ImpactAxisCurve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.2f, 0.8f), new Keyframe(0.6f, 1.15f), new Keyframe(1f, 1f)
        );
        private static readonly AnimationCurve PerpendicularAxisCurve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.2f, 1.2f), new Keyframe(0.6f, 0.9f), new Keyframe(1f, 1f)
        );

        [Header("--- MESH TUNING ---")]
        [SerializeField] [Range(0.02f, 0.25f)] private float cornerRadiusScale = 0.085f;
        [SerializeField] [Range(0f, 0.2f)] private float edgePadding = 0.05f; 
        [SerializeField] private float hitAnimDuration = 0.32f;

        [Header("--- LABEL SETTINGS ---")]
        [SerializeField] private bool useManualText = false; 
        [SerializeField] [TextArea] private string manualTextValue = "10";
        [Space]
        [SerializeField] private float labelFontSizeMin = 1f;
        [SerializeField] private float labelFontSizeMax = 5f;
        [SerializeField] private float labelMarginInsideBlock = 0.2f; 
        [SerializeField] private Color labelColorOverride = Color.clear; 

        private Vector2Int _gridPos;
        private Color _baseColor;
        private Color _displayColor;
        
        private Mesh _meshInstance;
        private Transform _meshVisualRoot;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        
        private Tween _colorTween;
        private Vector3 _meshBoundsCenter;
        private bool _isDestroying;

        protected override void Awake()
        {
            base.Awake();
            EnsureMeshVisuals();
        }

        private void OnValidate()
        {
            if (Label != null && useManualText) Label.text = manualTextValue;
        }

        private void OnDestroy()
        {
            _isDestroying = true;
            _colorTween?.Kill();
            transform.DOKill();
            if (_meshInstance != null) Destroy(_meshInstance);
        }

        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            _gridPos = specialCell.Position;
            _baseColor = color;
            _displayColor = color;

            EnsureMeshVisuals();
            
            if (!useManualText) UpdateText(specialCell.Counter);
            else Label.text = manualTextValue;

            RebuildMesh(specialCell);
            UpdatePivotToFootprintCenter(specialCell);
            ApplyMeshColors(color);
            ApplyLabelStyle(color);
        }

        public void UpdateCounter(int newCounter)
        {
            if (_isDestroying || useManualText) return;
            UpdateText(newCounter);
        }

        // --- HIỆU ỨNG BỊ ĐÂM (HIT) ĐÃ ĐƯỢC PHỤC HỒI MÀU SẮC ---
        public void PlayHitAnimation(Vector2Int dir, Color? blockedColor = null, bool playColorPulse = true)
        {
            if (_isDestroying) return;
            transform.DOKill();
            transform.localScale = TargetScale;
            
            if (playColorPulse)
            {
                // Nháy màu
                if (blockedColor.HasValue) PlayColorPulse(blockedColor.Value, 0.1f, 0.32f);
                else PlayHighlight();
            }

            // Méo hình (Squash & Stretch)
            DOTween.To(() => 0f, t => {
                float impact = ImpactAxisCurve.Evaluate(t);
                float perp = PerpendicularAxisCurve.Evaluate(t);
                transform.localScale = (dir.x != 0) 
                    ? new Vector3(TargetScale.x * impact, TargetScale.y * perp, 1f)
                    : new Vector3(TargetScale.x * perp, TargetScale.y * impact, 1f);
            }, 1f, hitAnimDuration).SetEase(Ease.Linear).SetTarget(transform).SetLink(gameObject);
        }

        // --- HIỆU ỨNG BIẾN MẤT (DESTROY) MỚI: JUICY POP & FADE ---
        public void PlayDestroyAnimation()
        {
            if (_isDestroying) return;
            _isDestroying = true;

            transform.DOKill();
            _colorTween?.Kill();

            Sequence seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);

            // Co tại chỗ theo nhiều nhịp giảm dần để cảm giác "rụt lại" vẫn có độ nảy,
            // nhưng không lóe, không lắc và không phình to lên.
            seq.Append(transform.DOScale(TargetScale * 0.92f, 0.08f).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(TargetScale * 0.8f, 0.09f).SetEase(Ease.OutCubic));
            seq.Append(transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack));

            Color transparentBase = new Color(_displayColor.r, _displayColor.g, _displayColor.b, 0f);
            seq.Join(DOVirtual.Color(_displayColor, transparentBase, 0.22f, ApplyAnimatedColor)
                .SetEase(Ease.InQuad));
            if (Label != null)
            {
                seq.Join(Label.DOFade(0f, 0.2f).SetEase(Ease.InQuad));
            }
            
            seq.OnComplete(() => Destroy(gameObject));
        }

        // --- LOGIC NHÁY MÀU ĐƯỢC VIẾT LẠI ---
        public override void PlayHighlight()
        {
            if (_isDestroying) return;
            Color highlightColor = Color.Lerp(_baseColor, Color.white, 0.4f);
            PlayColorPulse(highlightColor, 0.08f, 0.18f);
        }

        private void PlayColorPulse(Color flashColor, float flashUpDuration, float flashDownDuration)
        {
            if (_isDestroying) return;
            _colorTween?.Kill();
            Sequence seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);

            seq.Append(DOVirtual.Color(_displayColor, flashColor, flashUpDuration, ApplyAnimatedColor).SetEase(Ease.OutQuad));
            seq.Append(DOVirtual.Color(flashColor, _baseColor, flashDownDuration, ApplyAnimatedColor).SetEase(Ease.InQuad));
            seq.OnComplete(() => ApplyAnimatedColor(_baseColor));
            
            _colorTween = seq;
        }

        private void ApplyAnimatedColor(Color color)
        {
            _displayColor = color;
            ApplyMeshColors(color);
            // Label nháy theo màu tương phản luôn cho đẹp
            if (Label != null && labelColorOverride == Color.clear) 
            {
                Label.color = GetReadableLabelColor(color);
            }
        }

        private void ApplyLabelStyle(Color color)
        {
            if (Label == null) return;

            RectTransform rectTransform = Label.rectTransform;
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);

            if (_meshInstance != null)
            {
                Bounds b = _meshInstance.bounds;
                rectTransform.sizeDelta = new Vector2(b.size.x - labelMarginInsideBlock, b.size.y - labelMarginInsideBlock);
            }

            Label.enableAutoSizing = true;
            Label.fontSizeMin = labelFontSizeMin;
            Label.fontSizeMax = labelFontSizeMax;
            Label.alignment = TextAlignmentOptions.Center;
            
            Label.transform.localPosition = new Vector3(0f, 0f, LabelZOffset);
            Label.color = (labelColorOverride != Color.clear) ? labelColorOverride : GetReadableLabelColor(color);
        }

        private void RebuildMesh(SpecialCellSaveData specialCell)
        {
            if (_meshInstance != null) Destroy(_meshInstance);
            float radius = MeshCellSize * cornerRadiusScale;
            float padding = MeshCellSize * edgePadding;
            _meshInstance = CounterBlockMeshBuilder.Build(specialCell, MeshCellSize, radius, padding);
            _meshBoundsCenter = _meshInstance != null ? _meshInstance.bounds.center : Vector3.zero;
            _meshFilter.sharedMesh = _meshInstance;
        }

        private void UpdatePivotToFootprintCenter(SpecialCellSaveData specialCell)
        {
            if (specialCell == null) return;
            Vector3 rootPos = new Vector3(specialCell.Position.x * CellSize, specialCell.Position.y * CellSize, 0f);
            Vector3 centerOffset = new Vector3(_meshBoundsCenter.x * CellSize, _meshBoundsCenter.y * CellSize, 0f);
            transform.localPosition = rootPos + centerOffset;
            if (_meshVisualRoot != null) _meshVisualRoot.localPosition = -_meshBoundsCenter;
        }

        private void EnsureMeshVisuals()
        {
            if (_meshVisualRoot != null) return;
            _meshVisualRoot = new GameObject("MeshVisualRoot").transform;
            _meshVisualRoot.SetParent(transform, false);
            (_meshFilter, _meshRenderer) = CreateMeshNode(_meshVisualRoot.gameObject);
            ApplySorting();
        }

        private void ApplySorting()
        {
            if (BackgroundRenderer == null) return;
            _meshRenderer.sortingLayerName = BackgroundRenderer.sortingLayerName;
            _meshRenderer.sortingOrder = BackgroundRenderer.sortingOrder + 1;
            BackgroundRenderer.enabled = false;
        }

        private void ApplyMeshColors(Color fillColor)
        {
            if (_meshRenderer == null) return;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _meshRenderer.GetPropertyBlock(block);
            block.SetColor(ColorId, fillColor);
            block.SetColor(BaseColorId, fillColor);
            _meshRenderer.SetPropertyBlock(block);
        }

        private void UpdateText(int counter) => Label.text = counter.ToString();

        private static (MeshFilter, MeshRenderer) CreateMeshNode(GameObject node)
        {
            if (!node.TryGetComponent(out MeshFilter f)) f = node.AddComponent<MeshFilter>();
            if (!node.TryGetComponent(out MeshRenderer r)) r = node.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sharedMaterial = ResolveMeshMaterial();
            return (f, r);
        }

        private static Material ResolveMeshMaterial()
        {
            if (s_sharedMeshMaterial != null) return s_sharedMeshMaterial;
            s_sharedMeshMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
            return s_sharedMeshMaterial;
        }

        private static Color GetReadableLabelColor(Color c)
        {
            float lum = (0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b);
            return lum > 0.6f ? new Color(0.1f, 0.1f, 0.15f) : Color.white;
        }

        protected override float DefaultScaleMultiplier => 1f;
        protected override void OnVisualColorChanged(Color c) { _baseColor = c; ApplyMeshColors(c); ApplyLabelStyle(c); }
    }
}
