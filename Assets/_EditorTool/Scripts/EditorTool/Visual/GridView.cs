using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.Logic;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class GridView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CellView cellPrefab;
        [SerializeField] private EditorArrowLine arrowLinePrefab;
        [SerializeField] private Transform gridParent;
        [SerializeField] private Transform linesParent;

        private CellView[,] _cellViews;
        private GridSystem _gridLogic;

        // === DIRTY-FLAG PATTERN ===
        // Thay _activeLines (List) → Dictionary để lookup O(1), không cần scan toàn bộ
        private readonly Dictionary<string, EditorArrowLine> _linesByID = new Dictionary<string, EditorArrowLine>();
        // Tập hợp các Arrow ID cần re-render trong LateUpdate của frame này
        private readonly HashSet<string> _dirtyArrowIDs = new HashSet<string>();
        // Cờ báo cần dọn line của arrow đã bị xóa hoàn toàn
        private bool _needCleanupStaleLines;

        // =====================================================================

        public void Initialize(GridSystem logic)
        {
            _gridLogic = logic;
            _gridLogic.OnCellChanged += HandleCellDataChanged;
            GenerateGridVisual();
        }

        private void OnDestroy()
        {
            if (_gridLogic != null) _gridLogic.OnCellChanged -= HandleCellDataChanged;
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
                    CellView cellView = PoolingManager.Instance.Spawn(cellPrefab, new Vector3(x, y, 0), Quaternion.identity, gridParent);                    cellView.InitPosition(x, y);
                    cellView.UpdateVisual(_gridLogic.GetCell(x, y));
                    _cellViews[x, y] = cellView;
                }
            }
        }

        // === BƯỚC 1: Chỉ MARK dirty — KHÔNG spawn/despawn bất cứ thứ gì ===
        private void HandleCellDataChanged(int x, int y, CellData updatedData)
        {
            // Cell visual cập nhật ngay lập tức (rẻ, chỉ đổi màu sprite)
            _cellViews[x, y].UpdateVisual(updatedData);

            if (!string.IsNullOrEmpty(updatedData.arrowID))
            {
                // Cell thuộc về một arrow → mark arrow đó là dirty
                _dirtyArrowIDs.Add(updatedData.arrowID);
            }
            else
            {
                // Cell trở thành empty → có thể arrow nào đó vừa bị xóa
                _needCleanupStaleLines = true;
            }
        }

        // === BƯỚC 2: LateUpdate — flush toàn bộ dirty set, chỉ update những gì cần ===
        private void LateUpdate()
        {
            if (_dirtyArrowIDs.Count == 0 && !_needCleanupStaleLines) return;

            // Dọn line của arrow đã bị xóa khỏi GridSystem
            if (_needCleanupStaleLines)
            {
                CleanupStaleLines();
                _needCleanupStaleLines = false;
            }

            // Chỉ refresh đúng những arrow bị thay đổi trong frame này
            foreach (string id in _dirtyArrowIDs)
            {
                RefreshOneLine(id);
            }

            _dirtyArrowIDs.Clear();
        }

        // === Despawn/Spawn đúng 1 line duy nhất ===
        private void RefreshOneLine(string id)
        {
            var path = _gridLogic.GetArrowPath(id);

            // Arrow không còn tồn tại trong GridSystem → despawn line của nó
            if (path == null || path.Count == 0)
            {
                if (_linesByID.TryGetValue(id, out var stale) && stale != null)
                    PoolingManager.Instance.Despawn(stale.gameObject);
                _linesByID.Remove(id);
                return;
            }

            // Lấy line hiện có hoặc spawn mới từ pool
            if (!_linesByID.TryGetValue(id, out var line) || line == null)
            {
                line = PoolingManager.Instance.Spawn(arrowLinePrefab, Vector3.zero, Quaternion.identity, linesParent);
                _linesByID[id] = line;
            }

            // Update data — dùng EditorConstants, Single Source of Truth cho màu
            Color arrowColor = EditorConstants.GetArrowColor(id);
            bool isHeadFirst = _gridLogic.IsHeadFirst(id);
            line.Setup(id, path, arrowColor, isHeadFirst);
        }

        // === Dọn dẹp line của các arrow không còn tồn tại ===
        private void CleanupStaleLines()
        {
            var activeIDs = new HashSet<string>(_gridLogic.GetAllArrowIDs());
            var toRemove = new List<string>();

            foreach (var kvp in _linesByID)
            {
                if (!activeIDs.Contains(kvp.Key))
                {
                    if (kvp.Value != null)
                        PoolingManager.Instance.Despawn(kvp.Value.gameObject);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (string key in toRemove)
                _linesByID.Remove(key);
        }

        // === Đánh dấu toàn bộ arrow là dirty — dùng khi RebuildGrid hoặc Load ===
        private void MarkAllDirty()
        {
            foreach (string id in _gridLogic.GetAllArrowIDs())
                _dirtyArrowIDs.Add(id);
            _needCleanupStaleLines = true;
        }

        public void ClearVisuals()
        {
            // Dọn cells
            if (_cellViews != null)
            {
                foreach (var cell in _cellViews)
                {
                    if (cell != null)
                        PoolingManager.Instance.Despawn(cell.gameObject);
                }
            }
            _cellViews = null;

            // Dọn lines
            foreach (var kvp in _linesByID)
            {
                if (kvp.Value != null)
                    PoolingManager.Instance.Despawn(kvp.Value.gameObject);
            }
            _linesByID.Clear();

            // Reset dirty state
            _dirtyArrowIDs.Clear();
            _needCleanupStaleLines = false;
        }

        public void RebuildGrid()
        {
            ClearVisuals();
            GenerateGridVisual();
            // Sau khi rebuild, mark toàn bộ arrow hiện có để re-render lines
            MarkAllDirty();
        }
    }
}