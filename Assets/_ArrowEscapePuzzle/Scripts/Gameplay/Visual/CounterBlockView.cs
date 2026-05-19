using System;
using System.Collections.Generic;
using DG.Tweening;
using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArrowGame.Gameplay.Visual
{
    public class CounterBlockView : SpecialCellViewBase
    {
        private const float LabelZOffset = 0f;
        private const float MeshCellSize = 1f;

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

        [Header("--- MESH REFERENCES ---")]
        [SerializeField] private Transform meshVisualRoot;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private ParticleSystem coldMistParticles;
        [SerializeField] private ParticleSystem snowBurstParticles;
        [SerializeField] private ParticleSystem iceShakeBurstLeftParticles;
        [SerializeField] private ParticleSystem iceShakeBurstRightParticles;

        [Header("--- ARROW EXIT FEEDBACK ---")]
        [SerializeField] private float arrowExitTensionDuration = 0.18f;
        [SerializeField] [Range(0.02f, 0.3f)] private float arrowExitTensionStretch = 0.14f;
        [SerializeField] [Range(0.02f, 0.2f)] private float arrowExitTensionSquash = 0.08f;
        [SerializeField] private float snowBurstOffset = 0.22f;
        [SerializeField] private float iceShakeBurstEdgeInset = 0.04f;
        [SerializeField] [Range(0.05f, 0.5f)] private float iceShakeBurstStripWidth = 0.08f;
        [SerializeField] [Range(0.2f, 1f)] private float iceShakeBurstHeightScale = 0.72f;
        [SerializeField] private float destroyPreBreakDuration = 0.08f;
        [SerializeField] [Range(0.05f, 0.35f)] private float destroyPreBreakStretch = 0.2f;
        [SerializeField] [Range(0.02f, 0.2f)] private float destroyPreBreakSquash = 0.1f;

        private Vector2Int _gridPos;
        private Vector2Int _lastArrowExitDir = Vector2Int.up;
        
        private Mesh _meshInstance;
        private Vector3 _meshBoundsCenter;
        private bool _isDestroying;
        
        private List<Vector2Int> _occupiedOffsets;

        protected override void Awake()
        {
            base.Awake();
            EnsureMeshRendererVisualState();
            EnsureColdMistRendererVisualState();
            EnsureSnowBurstRendererVisualState();
            EnsureIceShakeBurstRendererVisualState();
        }

        private void OnValidate()
        {
            colorMode = SpecialCellColorMode.UseTheme;
            if (Label != null && useManualText) Label.text = manualTextValue;
        }

        private void OnDestroy()
        {
            _isDestroying = true;
            transform.DOKill();
            StopColdMist(true);
            ResetSnowBurst();
            ResetIceShakeBursts();
            if (_meshInstance != null) Destroy(_meshInstance);
        }

        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            colorMode = SpecialCellColorMode.UseTheme;

            if (!HasMeshReferences())
            {
                Debug.LogError("[CounterBlockView] Missing mesh references. Please assign MeshVisualRoot, MeshFilter, and MeshRenderer manually.");
                return;
            }

            _gridPos = specialCell.Position;
            _occupiedOffsets = CounterBlockUtility.GetAllOffsets(specialCell);
             
            if (!useManualText) UpdateText(specialCell.Counter);
            else Label.text = manualTextValue;

            EnsureMeshRendererVisualState();
            RebuildMesh(specialCell);
            SyncColdMistToMeshBounds();
            SyncIceShakeBurstsToMeshBounds();
            UpdatePivotToFootprintCenter(specialCell);
            ApplySorting();
            
            // Chỉ áp dụng style label (giữ margin để canh giữa); không thay đổi màu
            ApplyLabelStyle(); 
        }

        public void UpdateCounter(int newCounter)
        {
            if (_isDestroying || useManualText) return;
            UpdateText(newCounter);
        }

        public void PlayArrowExitFeedback(Vector2Int dir)
        {
            if (_isDestroying) return;

            if (dir != Vector2Int.zero)
            {
                _lastArrowExitDir = dir;
            }

            PlayIceShakeBursts();
            PlayArrowExitTension(dir);
        }

        public void PlayHitAnimation(Vector2Int dir, Color? blockedColor = null, bool playColorPulse = true)
        {
            if (_isDestroying) return;
            transform.DOKill();
            transform.localScale = TargetScale;
            
            // Đã bỏ logic nhấp nháy màu, chỉ chạy anim co giãn nảy (Scale Bounce) khi bị chạm vào
            DOTween.To(() => 0f, t => {
                float impact = ImpactAxisCurve.Evaluate(t);
                float perp = PerpendicularAxisCurve.Evaluate(t);
                transform.localScale = (dir.x != 0) 
                    ? new Vector3(TargetScale.x * impact, TargetScale.y * perp, 1f)
                    : new Vector3(TargetScale.x * perp, TargetScale.y * impact, 1f);
            }, 1f, hitAnimDuration).SetEase(Ease.Linear).SetTarget(transform).SetLink(gameObject);
        }

        public void PlayDestroyAnimation(Action onComplete = null)
        {
            if (_isDestroying) return;
            _isDestroying = true;

            transform.DOKill();
            PlaySnowBurst(_lastArrowExitDir);
            PlayIceShakeBursts();

            Sequence seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);

            Vector3 preBreakScale = GetDirectionalScale(_lastArrowExitDir, destroyPreBreakStretch, destroyPreBreakSquash);
            seq.Append(transform.DOScale(preBreakScale, destroyPreBreakDuration).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(TargetScale * 0.8f, 0.09f).SetEase(Ease.InQuad));
            seq.Append(transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack));

            // Chỉ fade alpha của text
            if (Label != null)
            {
                seq.Join(Label.DOFade(0f, 0.2f).SetEase(Ease.InQuad));
            }
            
            seq.OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        }

        public override void PlayHighlight()
        {
            // Để trống vì base class có thể yêu cầu Override, nhưng ta không dùng màu nữa
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            _isDestroying = false;
            SyncColdMistToMeshBounds();
            SyncIceShakeBurstsToMeshBounds();
            PlayColdMist();
            ResetSnowBurst();
            ResetIceShakeBursts();
        }

        public override void OnDespawn()
        {
            _isDestroying = false;
            StopColdMist(true);
            ResetSnowBurst();
            ResetIceShakeBursts();
            if (Label != null)
            {
                Label.alpha = 1f;
            }
            base.OnDespawn();
        }

        private void ApplyLabelStyle()
        {
            if (Label == null) return;

            RectTransform rectTransform = Label.rectTransform;
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // Giữ margin để canh chữ ở giữa lưới; không thay đổi kích thước hoặc màu chữ ở đây
            float m = labelMarginInsideBlock;
            Label.margin = new Vector4(m, m, m, m);
            Label.alignment = TextAlignmentOptions.Center;
            Label.enableWordWrapping = false;
            Label.overflowMode = TextOverflowModes.Truncate;

            Vector2 perfectCenter = CalculatePerfectVisualCenterLocal();
            
            Label.transform.localPosition = new Vector3(
                perfectCenter.x - _meshBoundsCenter.x,
                perfectCenter.y - _meshBoundsCenter.y,
                LabelZOffset);
                
            // Không thay đổi `Label.color` ở đây: giữ màu gốc của TextMeshPro
        }

        private Vector2 CalculatePerfectVisualCenterLocal()
        {
            if (_occupiedOffsets == null || _occupiedOffsets.Count == 0) return Vector2.zero;

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            Vector2 sumCenters = Vector2.zero;

            foreach (var offset in _occupiedOffsets)
            {
                if (offset.x < minX) minX = offset.x;
                if (offset.x > maxX) maxX = offset.x;
                if (offset.y < minY) minY = offset.y;
                if (offset.y > maxY) maxY = offset.y;

                sumCenters += new Vector2(offset.x * MeshCellSize, offset.y * MeshCellSize);
            }

            int width = maxX - minX + 1;
            int height = maxY - minY + 1;

            if (_occupiedOffsets.Count == width * height)
            {
                return new Vector2(
                    ((minX + maxX) / 2f) * MeshCellSize,
                    ((minY + maxY) / 2f) * MeshCellSize
                );
            }

            Vector2 averageCenter = sumCenters / _occupiedOffsets.Count;
            Vector2 bestCellCenter = Vector2.zero;
            float minSqrDist = float.MaxValue;

            foreach (var offset in _occupiedOffsets)
            {
                Vector2 cellCenter = new Vector2(offset.x * MeshCellSize, offset.y * MeshCellSize);
                float sqrDist = (cellCenter - averageCenter).sqrMagnitude;
        
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    bestCellCenter = cellCenter;
                }
            }

            return bestCellCenter;
        }

        private void RebuildMesh(SpecialCellSaveData specialCell)
        {
            if (_meshInstance != null) Destroy(_meshInstance);
            float radius = MeshCellSize * cornerRadiusScale;
            float padding = MeshCellSize * edgePadding;
            
            _meshInstance = CounterBlockMeshBuilder.Build(specialCell, MeshCellSize, radius, padding);
            _meshBoundsCenter = _meshInstance != null ? _meshInstance.bounds.center : Vector3.zero;
            
            meshFilter.sharedMesh = _meshInstance;
            if (meshRenderer != null) meshRenderer.enabled = _meshInstance != null && _meshInstance.vertexCount > 0;
        }

        private void SyncColdMistToMeshBounds()
        {
            if (coldMistParticles == null) return;

            var shape = coldMistParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = Vector3.zero;

            Vector3 size = _meshInstance != null ? _meshInstance.bounds.size : Vector3.one;
            shape.scale = new Vector3(
                Mathf.Max(0.35f, size.x),
                Mathf.Max(0.35f, size.y),
                0.1f);
        }

        private void UpdatePivotToFootprintCenter(SpecialCellSaveData specialCell)
        {
            if (specialCell == null) return;
            Vector3 rootPos = new Vector3(specialCell.Position.x * CellSize, specialCell.Position.y * CellSize, 0f);
            Vector3 centerOffset = new Vector3(_meshBoundsCenter.x * CellSize, _meshBoundsCenter.y * CellSize, 0f);
            
            transform.localPosition = rootPos + centerOffset;
            if (meshVisualRoot != null) meshVisualRoot.localPosition = -_meshBoundsCenter;
        }

        private void SyncIceShakeBurstsToMeshBounds()
        {
            Vector3 size = _meshInstance != null ? _meshInstance.bounds.size : Vector3.one;
            float halfWidth = Mathf.Max(0.2f, size.x * 0.5f);
            float sidePositionX = Mathf.Max(0.1f, halfWidth - iceShakeBurstEdgeInset);
            float stripHeight = Mathf.Max(0.3f, size.y * iceShakeBurstHeightScale);

            SyncIceShakeBurst(iceShakeBurstLeftParticles, -sidePositionX, stripHeight);
            SyncIceShakeBurst(iceShakeBurstRightParticles, sidePositionX, stripHeight);
        }

        private void SyncIceShakeBurst(ParticleSystem particles, float localX, float stripHeight)
        {
            if (particles == null) return;

            particles.transform.localPosition = new Vector3(localX, 0f, 0f);
            particles.transform.localRotation = Quaternion.identity;

            // var shape = particles.shape;
            // shape.enabled = true;
            // shape.shapeType = ParticleSystemShapeType.Box;
            // shape.position = Vector3.zero;
            // shape.scale = new Vector3(iceShakeBurstStripWidth, stripHeight, 0.05f);
        }

        private void ApplySorting()
        {
            if (meshRenderer == null || BackgroundRenderer == null) return;
    
            // 1. Set lớp cho cái lưới (Mesh)
            meshRenderer.sortingLayerName = BackgroundRenderer.sortingLayerName;
            meshRenderer.sortingOrder = BackgroundRenderer.sortingOrder + 1;
            BackgroundRenderer.enabled = !meshRenderer.enabled;

            ParticleSystemRenderer coldMistRenderer = GetColdMistRenderer();
            if (coldMistRenderer != null)
            {
                coldMistRenderer.sortingLayerName = meshRenderer.sortingLayerName;
                coldMistRenderer.sortingOrder = meshRenderer.sortingOrder + 1;
            }

            ParticleSystemRenderer snowBurstRenderer = GetSnowBurstRenderer();
            if (snowBurstRenderer != null)
            {
                snowBurstRenderer.sortingLayerName = meshRenderer.sortingLayerName;
                snowBurstRenderer.sortingOrder = meshRenderer.sortingOrder + 1;
            }

            ApplyParticleSorting(GetIceShakeBurstLeftRenderer(), meshRenderer.sortingLayerName, meshRenderer.sortingOrder + 1);
            ApplyParticleSorting(GetIceShakeBurstRightRenderer(), meshRenderer.sortingLayerName, meshRenderer.sortingOrder + 1);

            // 2. Ép lớp cho cái Text (Label) phải NẰM TRÊN particle + Mesh
            if (Label != null)
            {
                // TextMeshPro (World Space) thực chất cũng dùng Renderer bên dưới
                Renderer textRenderer = Label.GetComponent<Renderer>();
                if (textRenderer != null)
                {
                    textRenderer.sortingLayerName = meshRenderer.sortingLayerName;
                    textRenderer.sortingOrder = meshRenderer.sortingOrder + 2;
                }
            }
        }

        private void UpdateText(int counter) => Label.text = counter.ToString();

        private static Color GetReadableLabelColor(Color c)
        {
            float lum = (0.299f * c.r) + (0.587f * c.g) + (0.114f * c.b);
            return lum > 0.6f ? new Color(0.1f, 0.1f, 0.15f) : Color.white;
        }

        protected override float DefaultScaleMultiplier => 1f;
        
        protected override void OnVisualColorChanged(Color c)
        {
            // Không ép màu mới cho Label; chỉ tái-áp dụng style (vị trí/margin)
            ApplyLabelStyle();
        }

        private bool HasMeshReferences()
        {
            return meshVisualRoot != null && meshFilter != null && meshRenderer != null;
        }

        private void EnsureMeshRendererVisualState()
        {
            if (meshRenderer == null) return;

            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private ParticleSystemRenderer GetColdMistRenderer()
        {
            return coldMistParticles != null ? coldMistParticles.GetComponent<ParticleSystemRenderer>() : null;
        }

        private void EnsureColdMistRendererVisualState()
        {
            ParticleSystemRenderer coldMistRenderer = GetColdMistRenderer();
            if (coldMistRenderer == null) return;

            coldMistRenderer.shadowCastingMode = ShadowCastingMode.Off;
            coldMistRenderer.receiveShadows = false;
        }

        private void EnsureSnowBurstRendererVisualState()
        {
            ParticleSystemRenderer snowBurstRenderer = GetSnowBurstRenderer();
            if (snowBurstRenderer == null) return;

            snowBurstRenderer.shadowCastingMode = ShadowCastingMode.Off;
            snowBurstRenderer.receiveShadows = false;
        }

        private void EnsureIceShakeBurstRendererVisualState()
        {
            EnsureParticleRendererVisualState(GetIceShakeBurstLeftRenderer());
            EnsureParticleRendererVisualState(GetIceShakeBurstRightRenderer());
        }

        private void EnsureParticleRendererVisualState(ParticleSystemRenderer renderer)
        {
            if (renderer == null) return;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void ApplyParticleSorting(ParticleSystemRenderer renderer, string sortingLayerName, int sortingOrder)
        {
            if (renderer == null) return;

            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrder;
        }

        private void PlayColdMist()
        {
            if (coldMistParticles == null) return;

            coldMistParticles.Clear(true);
            coldMistParticles.Play(true);
        }

        private void PlaySnowBurst(Vector2Int dir)
        {
            if (snowBurstParticles == null) return;

            Vector3 burstOffset = dir == Vector2Int.zero
                ? Vector3.zero
                : new Vector3(dir.x, dir.y, 0f).normalized * snowBurstOffset;
            snowBurstParticles.transform.localPosition = burstOffset;
            snowBurstParticles.transform.localRotation = Quaternion.identity;

            snowBurstParticles.Clear(true);
            snowBurstParticles.Play(true);
        }

        private void PlayIceShakeBursts()
        {
            PlayIceShakeBurst(iceShakeBurstLeftParticles);
            PlayIceShakeBurst(iceShakeBurstRightParticles);
        }

        private void PlayIceShakeBurst(ParticleSystem particles)
        {
            if (particles == null) return;

            particles.Clear(true);
            particles.Play(true);
        }

        private void PlayArrowExitTension(Vector2Int dir)
        {
            transform.DOKill();
            transform.localScale = TargetScale;

            Vector3 tensionScale = GetDirectionalScale(dir, arrowExitTensionStretch, arrowExitTensionSquash);
            Sequence seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);
            seq.Append(transform.DOScale(tensionScale, arrowExitTensionDuration * 0.45f).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(TargetScale, arrowExitTensionDuration * 0.55f).SetEase(Ease.OutBack));
        }

        private void StopColdMist(bool clearParticles)
        {
            if (coldMistParticles == null) return;

            ParticleSystemStopBehavior stopBehavior = clearParticles
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;
            coldMistParticles.Stop(true, stopBehavior);
        }

        private void ResetSnowBurst()
        {
            if (snowBurstParticles == null) return;

            snowBurstParticles.transform.localPosition = Vector3.zero;
            snowBurstParticles.transform.localRotation = Quaternion.identity;
            snowBurstParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void ResetIceShakeBursts()
        {
            ResetIceShakeBurst(iceShakeBurstLeftParticles);
            ResetIceShakeBurst(iceShakeBurstRightParticles);
        }

        private void ResetIceShakeBurst(ParticleSystem particles)
        {
            if (particles == null) return;

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private ParticleSystemRenderer GetSnowBurstRenderer()
        {
            return snowBurstParticles != null ? snowBurstParticles.GetComponent<ParticleSystemRenderer>() : null;
        }

        private Vector3 GetDirectionalScale(Vector2Int dir, float stretchAmount, float squashAmount)
        {
            Vector2Int resolvedDir = dir == Vector2Int.zero ? _lastArrowExitDir : dir;
            if (resolvedDir == Vector2Int.zero)
            {
                resolvedDir = Vector2Int.up;
            }

            float stretchX = resolvedDir.x != 0 ? 1f + stretchAmount : 1f - squashAmount;
            float stretchY = resolvedDir.y != 0 ? 1f + stretchAmount : 1f - squashAmount;

            if (resolvedDir.x != 0)
            {
                stretchY = 1f - squashAmount;
            }

            if (resolvedDir.y != 0)
            {
                stretchX = 1f - squashAmount;
            }

            return new Vector3(TargetScale.x * stretchX, TargetScale.y * stretchY, 1f);
        }

        private ParticleSystemRenderer GetIceShakeBurstLeftRenderer()
        {
            return iceShakeBurstLeftParticles != null ? iceShakeBurstLeftParticles.GetComponent<ParticleSystemRenderer>() : null;
        }

        private ParticleSystemRenderer GetIceShakeBurstRightRenderer()
        {
            return iceShakeBurstRightParticles != null ? iceShakeBurstRightParticles.GetComponent<ParticleSystemRenderer>() : null;
        }
    }
}
