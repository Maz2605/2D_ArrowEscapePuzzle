using System;
using System.Collections.Generic;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    [RequireComponent(typeof(LineRenderer))]
    public class ArrowLinkGroupView : MonoBehaviour, IPoolable
    {
        [Header("--- COMPONENTS ---")]
        [SerializeField] private LineRenderer lineRenderer;

        [Header("--- ENERGY FLOW SETTINGS ---")]
        [SerializeField] private float tileLength = 1.2f;
        [SerializeField] private float lineWidth = 0.3f;

        private readonly List<ArrowLineView> _linkedArrows = new List<ArrowLineView>();

        private Material _instancedMat;
        private Vector3[] _positionCache = Array.Empty<Vector3>();
        private Vector3[] _lastHeadPositions = Array.Empty<Vector3>();
        private bool _hasInitializedPositions;

        // TỐI ƯU HIỆU NĂNG: Cache lại ID của property trong Shader để tránh lookup bằng string mỗi lần update
        // Lưu ý: Đảm bảo biến Tiling trong Blackboard của Shader Graph có Reference name là "_Tiling"
        private static readonly int TilingPropertyId = Shader.PropertyToID("_Tiling");

        public int ArrowCount => _linkedArrows.Count;

        private void Awake()
        {
            if (lineRenderer == null)
            {
                Debug.LogError("[ArrowLinkGroupView] Missing LineRenderer reference. Please assign it in the prefab.");
                return;
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.startWidth = lineRenderer.endWidth = lineWidth;

            // Khởi tạo Instanced Material để tránh ảnh hưởng đến các object khác dùng chung material gốc
            if (lineRenderer.sharedMaterial != null && _instancedMat == null)
            {
                _instancedMat = Instantiate(lineRenderer.sharedMaterial);
                _instancedMat.name = $"{lineRenderer.sharedMaterial.name}_Instance";
                lineRenderer.sharedMaterial = _instancedMat;
            }
        }

        public void Setup(IReadOnlyList<ArrowLineView> arrows)
        {
            if (arrows == null || arrows.Count < 2) return;

            _linkedArrows.Clear();
            for (int i = 0; i < arrows.Count; i++)
            {
                if (arrows[i] != null) _linkedArrows.Add(arrows[i]);
            }

            EnsurePositionCacheCapacity(_linkedArrows.Count);
            lineRenderer.positionCount = _linkedArrows.Count;
            _hasInitializedPositions = false;

            RefreshColors();
            RefreshPositionsIfNeeded(true);
        }

        public void RefreshColors()
        {
            if (_linkedArrows.Count < 2) return;

            int keyCount = Mathf.Min(_linkedArrows.Count, 8);
            GradientColorKey[] colorKeys = new GradientColorKey[keyCount];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[keyCount];

            for (int i = 0; i < keyCount; i++)
            {
                float time = i / (float)(keyCount - 1);
                int arrowIndex = Mathf.RoundToInt(time * (_linkedArrows.Count - 1));
                
                // Nhận màu sắc random/dynamic từ từng mũi tên
                Color color = _linkedArrows[arrowIndex].BaseColor;
                colorKeys[i] = new GradientColorKey(color, time);
                alphaKeys[i] = new GradientAlphaKey(1f, time);
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            lineRenderer.colorGradient = gradient;
        }

        public bool RefreshPositionsIfNeeded(bool forceRefresh = false)
        {
            if (_linkedArrows == null || _linkedArrows.Count < 2) return false;

            float totalDist = 0f;
            Vector3 lastPos = Vector3.zero;
            bool hasChanges = forceRefresh || !_hasInitializedPositions;

            for (int i = 0; i < _linkedArrows.Count; i++)
            {
                ArrowLineView arrowView = _linkedArrows[i];
                if (arrowView == null) continue;

                Vector3 currentPos = arrowView.HeadPosition;
                _positionCache[i] = currentPos;

                if (!_hasInitializedPositions || _lastHeadPositions[i] != currentPos)
                {
                    hasChanges = true;
                    _lastHeadPositions[i] = currentPos;
                }

                if (i > 0)
                {
                    totalDist += Vector3.Distance(lastPos, currentPos);
                }

                lastPos = currentPos;
            }

            if (!hasChanges) return false;

            lineRenderer.positionCount = _linkedArrows.Count;
            lineRenderer.SetPositions(_positionCache);
            _hasInitializedPositions = true;

            if (_instancedMat != null)
            {
                // CẬP NHẬT: Giao tiếp với Shader Graph qua Property ID đã được cache
                Vector2 newTiling = new Vector2(totalDist / tileLength, 1f);
                _instancedMat.SetVector(TilingPropertyId, newTiling);
            }

            return true;
        }

        public void RemoveArrow(ArrowLineView arrow)
        {
            if (!_linkedArrows.Contains(arrow)) return;

            _linkedArrows.Remove(arrow);
            if (_linkedArrows.Count < 2)
            {
                PoolingManager.Instance.Despawn(gameObject);
                return;
            }

            EnsurePositionCacheCapacity(_linkedArrows.Count);
            lineRenderer.positionCount = _linkedArrows.Count;
            _hasInitializedPositions = false;
            RefreshColors();
            RefreshPositionsIfNeeded(true);
        }

        public void OnSpawn()
        {
            lineRenderer.enabled = true;
            _hasInitializedPositions = false;
        }

        public void OnDespawn()
        {
            _linkedArrows.Clear();
            lineRenderer.positionCount = 0;
            lineRenderer.enabled = false;
            _hasInitializedPositions = false;
        }

        private void OnDestroy()
        {
            // Quan trọng: Phải dọn dẹp Instanced Material để tránh rỉ bộ nhớ (Memory Leak)
            if (_instancedMat != null)
            {
                Destroy(_instancedMat);
                _instancedMat = null;
            }
        }

        private void EnsurePositionCacheCapacity(int requiredSize)
        {
            if (_positionCache.Length < requiredSize)
            {
                _positionCache = new Vector3[requiredSize];
            }

            if (_lastHeadPositions.Length < requiredSize)
            {
                _lastHeadPositions = new Vector3[requiredSize];
            }
        }
    }
}