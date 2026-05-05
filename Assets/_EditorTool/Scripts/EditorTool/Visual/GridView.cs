using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.Logic;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class GridView : MonoBehaviour
    {
        [Header("Prefabs & References")]
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private EditorArrowLine _arrowLinePrefab;
        [SerializeField] private List<SpecialCellVisualPrefabSlot> _specialCellPrefabs = new List<SpecialCellVisualPrefabSlot>();

        [Header("Hierarchy Parents")]
        [SerializeField] private Transform _gridParent;
        [SerializeField] private Transform _linesParent;
        [SerializeField] private Transform _specialMarkerParent;

        private GridSystem _gridLogic;
        private CellView[,] _cellViews;
        
        // Caches & Collections
        private readonly Dictionary<string, EditorArrowLine> _linesByID = new Dictionary<string, EditorArrowLine>();
        private readonly HashSet<string> _dirtyArrowIDs = new HashSet<string>();
        private readonly Dictionary<Vector2Int, GameObject> _specialMarkers = new Dictionary<Vector2Int, GameObject>();
        private bool _needCleanupStaleLines;

        public void Initialize(GridSystem logic)
        {
            _gridLogic = logic;
            _gridLogic.OnCellChanged += HandleCellDataChanged;
            _gridLogic.OnGridRebuilt += HandleGridRebuilt; // Lắng nghe sự kiện nạp hàng loạt xong
            
            EnsureSpecialMarkerParent();
            GenerateGridVisual();
            MarkAllDirty();
        }

        private void OnDestroy()
        {
            if (_gridLogic != null)
            {
                _gridLogic.OnCellChanged -= HandleCellDataChanged;
                _gridLogic.OnGridRebuilt -= HandleGridRebuilt;
            }
        }

        // Gọi lại khi toàn bộ dữ liệu hàng loạt đã nạp xong — chỉ vẽ lại 1 lần duy nhất
        private void HandleGridRebuilt()
        {
            RebuildGrid();
        }

        private void GenerateGridVisual()
        {
            int width = _gridLogic.Width;
            int height = _gridLogic.Height;
            _cellViews = new CellView[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CellView cellView = PoolingManager.Instance.Spawn(_cellPrefab, new Vector3(x, y, 0f), Quaternion.identity, _gridParent);
                    cellView.InitPosition(x, y);
                    
                    SpecialCellSaveData specialCell = _gridLogic.GetSpecialCellAt(x, y);
                    cellView.UpdateVisual(_gridLogic.GetCell(x, y), ShouldUseCellFallbackVisual(specialCell) ? specialCell : null);
                    
                    RefreshSpecialMarker(new Vector2Int(x, y), specialCell);
                    _cellViews[x, y] = cellView;
                }
            }
        }

        private void HandleCellDataChanged(int x, int y, CellData updatedData)
        {
            SpecialCellSaveData specialCell = _gridLogic.GetSpecialCellAt(x, y);
            
            if (_cellViews != null && _cellViews[x, y] != null)
            {
                _cellViews[x, y].UpdateVisual(updatedData, ShouldUseCellFallbackVisual(specialCell) ? specialCell : null);
            }
            
            RefreshSpecialMarker(new Vector2Int(x, y), specialCell);

            if (!string.IsNullOrEmpty(updatedData.arrowID))
            {
                _dirtyArrowIDs.Add(updatedData.arrowID);
            }
            else
            {
                _needCleanupStaleLines = true;
            }
        }

        private void LateUpdate()
        {
            if (_dirtyArrowIDs.Count == 0 && !_needCleanupStaleLines) return;

            if (_needCleanupStaleLines)
            {
                CleanupStaleLines();
                _needCleanupStaleLines = false;
            }

            foreach (string id in _dirtyArrowIDs)
            {
                RefreshOneLine(id);
            }

            _dirtyArrowIDs.Clear();
        }

        private void RefreshOneLine(string id)
        {
            List<Vector2Int> path = _gridLogic.GetArrowPath(id);

            if (path == null || path.Count == 0)
            {
                if (_linesByID.TryGetValue(id, out EditorArrowLine stale) && stale != null)
                {
                    PoolingManager.Instance.Despawn(stale.gameObject);
                }
                _linesByID.Remove(id);
                return;
            }

            if (!_linesByID.TryGetValue(id, out EditorArrowLine line) || line == null)
            {
                line = PoolingManager.Instance.Spawn(_arrowLinePrefab, Vector3.zero, Quaternion.identity, _linesParent);
                _linesByID[id] = line;
            }

            Color arrowColor = EditorConstants.GetArrowColor(id);
            bool isHeadFirst = _gridLogic.IsHeadFirst(id);
            line.Setup(id, path, arrowColor, isHeadFirst);
        }

        private void CleanupStaleLines()
        {
            HashSet<string> activeIDs = new HashSet<string>(_gridLogic.GetAllArrowIDs());
            List<string> toRemove = new List<string>();

            foreach (var kvp in _linesByID)
            {
                if (!activeIDs.Contains(kvp.Key))
                {
                    if (kvp.Value != null) PoolingManager.Instance.Despawn(kvp.Value.gameObject);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string key in toRemove) _linesByID.Remove(key);
        }

        // ==========================================
        // SPECIAL MARKERS HANDLING (Refactored)
        // ==========================================

        private void RefreshSpecialMarker(Vector2Int position, SpecialCellSaveData specialCell)
        {
            // Trạng thái 1: Bị xóa
            if (specialCell == null)
            {
                if (_specialMarkers.TryGetValue(position, out GameObject staleMarker) && staleMarker != null)
                {
                    // Đổi Destroy thành Despawn để ăn khớp với Object Pooling
                    PoolingManager.Instance.Despawn(staleMarker);
                }
                _specialMarkers.Remove(position);
                return;
            }

            // Trạng thái 2: Tạo mới hoặc Update
            GameObject markerObject;
            if (_specialMarkers.TryGetValue(position, out GameObject existingMarker) && existingMarker != null)
            {
                markerObject = existingMarker;
            }
            else
            {
                GameObject markerPrefab = GetSpecialMarkerPrefab(specialCell.Type);
                if (markerPrefab == null)
                {
                    Debug.LogWarning($"[GridView] Missing Visual Prefab for SpecialType: {specialCell.Type}");
                    return;
                }

                // Chuyển từ Instantiate sang Pooling
                markerObject = PoolingManager.Instance.Spawn(markerPrefab, Vector3.zero, Quaternion.identity, _specialMarkerParent);
                _specialMarkers[position] = markerObject;
            }

            markerObject.name = $"Special_{specialCell.Type}_{position.x}_{position.y}";
            
            // Ép buộc Prefab phải có sẵn Script, không dùng AddComponent lúc runtime nữa
            if (markerObject.TryGetComponent(out EditorSpecialCellViewBase view))
            {
                view.Setup(specialCell, GetSpecialCellColor(specialCell));
            }
            else
            {
                Debug.LogError($"[GridView] Prefab '{markerObject.name}' đang thiếu component kế thừa từ EditorSpecialCellViewBase!");
            }
        }

        private void ClearSpecialMarkers()
        {
            foreach (var pair in _specialMarkers)
            {
                if (pair.Value != null) PoolingManager.Instance.Despawn(pair.Value);
            }
            _specialMarkers.Clear();
        }

        private void EnsureSpecialMarkerParent()
        {
            if (_specialMarkerParent != null) return;

            GameObject root = new GameObject("SpecialMarkers");
            root.transform.SetParent(_gridParent, false);
            root.transform.localPosition = Vector3.zero;
            _specialMarkerParent = root.transform;
        }

        private GameObject GetSpecialMarkerPrefab(BoardSpecialType type)
        {
            for (int i = 0; i < _specialCellPrefabs.Count; i++)
            {
                var slot = _specialCellPrefabs[i];
                if (slot != null && slot.Type == type && slot.Prefab != null) return slot.Prefab;
            }
            return null;
        }

        private bool ShouldUseCellFallbackVisual(SpecialCellSaveData specialCell)
        {
            return specialCell != null && GetSpecialMarkerPrefab(specialCell.Type) == null;
        }

        private static Color GetSpecialCellColor(SpecialCellSaveData specialCell)
        {
            if (specialCell.Type == BoardSpecialType.Redirect)
                return new Color(0.95f, 0.73f, 0.16f, 0.95f);

            int seed = Mathf.Abs((specialCell.PortalId ?? string.Empty).GetHashCode());
            return Color.HSVToRGB((seed % 100) / 100f, 0.65f, 0.95f);
        }

        // ==========================================
        // LIFECYCLE & CLEANUP
        // ==========================================

        private void MarkAllDirty()
        {
            foreach (string id in _gridLogic.GetAllArrowIDs()) _dirtyArrowIDs.Add(id);
            _needCleanupStaleLines = true;

            for (int x = 0; x < _gridLogic.Width; x++)
            {
                for (int y = 0; y < _gridLogic.Height; y++)
                {
                    SpecialCellSaveData specialCell = _gridLogic.GetSpecialCellAt(x, y);
                    if (_cellViews[x, y] != null)
                    {
                        _cellViews[x, y].UpdateVisual(_gridLogic.GetCell(x, y), ShouldUseCellFallbackVisual(specialCell) ? specialCell : null);
                    }
                    RefreshSpecialMarker(new Vector2Int(x, y), specialCell);
                }
            }
        }

        public void ClearVisuals()
        {
            if (_cellViews != null)
            {
                foreach (CellView cell in _cellViews)
                {
                    if (cell != null) PoolingManager.Instance.Despawn(cell.gameObject);
                }
            }
            _cellViews = null;
            
            ClearSpecialMarkers();

            foreach (var kvp in _linesByID)
            {
                if (kvp.Value != null) PoolingManager.Instance.Despawn(kvp.Value.gameObject);
            }
            _linesByID.Clear();
            
            _dirtyArrowIDs.Clear();
            _needCleanupStaleLines = false;
        }

        public void RebuildGrid()
        {
            ClearVisuals();
            GenerateGridVisual();
            MarkAllDirty();
        }
    }
}