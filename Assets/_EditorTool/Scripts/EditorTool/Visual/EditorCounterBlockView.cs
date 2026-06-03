using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;
using TMPro;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class EditorCounterBlockView : EditorSpecialCellViewBase
    {
        private const float LabelZOffset = -0.05f;
        private const float ColorLuminanceThreshold = 0.58f;
        private const float MeshCellSizeWorld = 1f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static Material s_sharedMeshMaterial;

        [Header("Mesh Shape")]
        [SerializeField] [Range(0.02f, 0.25f)] private float cornerRadiusScale = 0.18f;
        [SerializeField] [Range(0f, 0.2f)] private float edgePadding = 0.04f;

        [Header("Label")]
        [SerializeField] private float labelFontSizeMin = 1.25f;
        [SerializeField] private float labelFontSizeMax = 6.5f;
        [SerializeField] private float labelMarginInsideBlock = 0.18f;
        [SerializeField] private Color labelColorOverride = Color.clear;

        private SpecialCellSaveData _currentData;
        private Mesh _meshInstance;
        private Transform _meshVisualRoot;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Vector3 _meshBoundsCenter;

        protected override void Awake()
        {
            base.Awake();
            EnsureMeshVisuals();
        }

        private void OnDestroy()
        {
            ReleaseMeshInstance();
        }

        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            _currentData = specialCell;
            EnsureMeshVisuals();
            ApplyLabelText(specialCell.Counter);

            if (TryBuildMeshVisual(specialCell))
            {
                ApplyMeshColors(color);
                ApplyCenteredTransform(specialCell.Position);
                ApplyLabelStyle(color, useMeshBounds: true);
                if (BackgroundRenderer != null) BackgroundRenderer.enabled = false;
                return;
            }

            ApplyFallbackTransform(specialCell.Position);
            ApplyLabelStyle(color, useMeshBounds: false);
            if (BackgroundRenderer != null)
            {
                BackgroundRenderer.color = color;
                BackgroundRenderer.enabled = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (_currentData == null || _currentData.OccupiedOffsets == null) return;
            
            Gizmos.color = Color.yellow;
            Vector3 pivotPos = new Vector3(_currentData.Position.x, _currentData.Position.y, transform.position.z);
            
            List<Vector2Int> allPoints = new List<Vector2Int>(_currentData.OccupiedOffsets);
            if (!allPoints.Contains(Vector2Int.zero)) allPoints.Add(Vector2Int.zero);
            
            foreach (Vector2Int p in allPoints)
            {
                Vector3 worldPos = pivotPos + new Vector3(p.x, p.y, 0);
                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
            }
            
            for (int i = 0; i < allPoints.Count; i++)
            {
                for (int j = i + 1; j < allPoints.Count; j++)
                {
                    if (Vector2Int.Distance(allPoints[i], allPoints[j]) == 1f)
                    {
                        Vector3 posA = pivotPos + new Vector3(allPoints[i].x, allPoints[i].y, 0);
                        Vector3 posB = pivotPos + new Vector3(allPoints[j].x, allPoints[j].y, 0);
                        Gizmos.DrawLine(posA, posB);
                    }
                }
            }
        }

        private bool TryBuildMeshVisual(SpecialCellSaveData specialCell)
        {
            ReleaseMeshInstance();

            float meshCellSize = MeshCellSizeWorld / DefaultScaleMultiplier;
            float radius = meshCellSize * cornerRadiusScale;
            float padding = meshCellSize * edgePadding;

            _meshInstance = CounterBlockMeshBuilder.Build(specialCell, meshCellSize, radius, padding);
            if (_meshInstance == null || _meshInstance.vertexCount == 0)
            {
                _meshBoundsCenter = Vector3.zero;
                if (_meshFilter != null) _meshFilter.sharedMesh = null;
                return false;
            }

            _meshBoundsCenter = _meshInstance.bounds.center;
            _meshFilter.sharedMesh = _meshInstance;
            return true;
        }

        private void EnsureMeshVisuals()
        {
            if (_meshVisualRoot != null) return;

            _meshVisualRoot = transform.Find("MeshVisualRoot");
            if (_meshVisualRoot == null)
            {
                _meshVisualRoot = new GameObject("MeshVisualRoot").transform;
                _meshVisualRoot.SetParent(transform, false);
            }

            (_meshFilter, _meshRenderer) = CreateMeshNode(_meshVisualRoot.gameObject);
            ApplySorting();
        }

        private void ApplyCenteredTransform(Vector2Int basePosition)
        {
            transform.localPosition = new Vector3(
                basePosition.x + (_meshBoundsCenter.x * DefaultScaleMultiplier),
                basePosition.y + (_meshBoundsCenter.y * DefaultScaleMultiplier),
                -0.05f);

            if (_meshVisualRoot != null)
            {
                _meshVisualRoot.localPosition = -_meshBoundsCenter;
            }
        }

        private void ApplyFallbackTransform(Vector2Int basePosition)
        {
            transform.localPosition = new Vector3(basePosition.x, basePosition.y, -0.05f);
            if (_meshVisualRoot != null)
            {
                _meshVisualRoot.localPosition = Vector3.zero;
            }
        }

        private void ApplyLabelText(int counter)
        {
            if (Label != null)
            {
                Label.text = counter.ToString();
            }
        }

        private void ApplyLabelStyle(Color color, bool useMeshBounds)
        {
            if (Label == null) return;

            RectTransform rectTransform = Label.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
                if (useMeshBounds && _meshInstance != null)
                {
                    Bounds bounds = _meshInstance.bounds;
                    float margin = labelMarginInsideBlock / DefaultScaleMultiplier;
                    rectTransform.sizeDelta = new Vector2(
                        Mathf.Max(0.45f, bounds.size.x - margin),
                        Mathf.Max(0.45f, bounds.size.y - margin));
                }
                else
                {
                    rectTransform.sizeDelta = new Vector2(0.92f / DefaultScaleMultiplier, 0.92f / DefaultScaleMultiplier);
                }
            }

            Label.enableAutoSizing = true;
            Label.fontSizeMin = labelFontSizeMin;
            Label.fontSizeMax = labelFontSizeMax;
            Label.alignment = TextAlignmentOptions.Center;
            Label.enableWordWrapping = false;
            Label.overflowMode = TextOverflowModes.Truncate;
            Label.margin = Vector4.zero;
            Label.transform.localPosition = new Vector3(0f, 0f, LabelZOffset);
            Label.color = labelColorOverride != Color.clear ? labelColorOverride : GetReadableLabelColor(color);
        }

        private void ApplySorting()
        {
            if (_meshRenderer == null) return;

            if (BackgroundRenderer != null)
            {
                _meshRenderer.sortingLayerName = BackgroundRenderer.sortingLayerName;
                _meshRenderer.sortingOrder = BackgroundRenderer.sortingOrder + 1;
            }
            else
            {
                _meshRenderer.sortingOrder = DefaultSortingOrder + 1;
            }
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

        private void ReleaseMeshInstance()
        {
            if (_meshFilter != null)
            {
                _meshFilter.sharedMesh = null;
            }

            if (_meshInstance == null) return;

            if (Application.isPlaying)
            {
                Destroy(_meshInstance);
            }
            else
            {
                DestroyImmediate(_meshInstance);
            }

            _meshInstance = null;
        }

        private static (MeshFilter, MeshRenderer) CreateMeshNode(GameObject node)
        {
            if (!node.TryGetComponent(out MeshFilter filter)) filter = node.AddComponent<MeshFilter>();
            if (!node.TryGetComponent(out MeshRenderer renderer)) renderer = node.AddComponent<MeshRenderer>();

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = ResolveMeshMaterial();
            return (filter, renderer);
        }

        private static Material ResolveMeshMaterial()
        {
            if (s_sharedMeshMaterial != null) return s_sharedMeshMaterial;

            s_sharedMeshMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return s_sharedMeshMaterial;
        }

        private static Color GetReadableLabelColor(Color baseColor)
        {
            float luminance = (0.299f * baseColor.r) + (0.587f * baseColor.g) + (0.114f * baseColor.b);
            return luminance > ColorLuminanceThreshold
                ? new Color(0.07f, 0.11f, 0.15f, 0.95f)
                : new Color(0.96f, 0.99f, 1f, 0.98f);
        }
    }
}
