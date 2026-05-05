using System;
using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Logic
{
    public class GridSystem
    {
        private CellData[,] _grid;
        private readonly Dictionary<string, List<Vector2Int>> _arrowPaths = new Dictionary<string, List<Vector2Int>>();
        private readonly Dictionary<string, bool> _isHeadFirst = new Dictionary<string, bool>();
        private readonly Dictionary<Vector2Int, SpecialCellSaveData> _specialCells =
            new Dictionary<Vector2Int, SpecialCellSaveData>();

        public int Width { get; private set; }
        public int Height { get; private set; }

        public event Action<int, int, CellData> OnCellChanged;
        // Bắn ra sau khi toàn bộ dữ liệu hàng loạt đã được nạp xong
        public event Action OnGridRebuilt;

        // Khi true, SetCellVisual sẽ không bắn OnCellChanged (Silent Mode)
        public bool SuppressEvents { get; private set; } = false;

        /// <summary>Bật Silent Mode — tắt tất cả event cập nhật visual trong lúc nạp dữ liệu hàng loạt.</summary>
        public void BeginBulkLoad()
        {
            SuppressEvents = true;
        }

        /// <summary>Tắt Silent Mode — bắn OnGridRebuilt một lần để GridView vẽ lại toàn bộ Map.</summary>
        public void EndBulkLoad()
        {
            SuppressEvents = false;
            OnGridRebuilt?.Invoke();
        }

        public void Initialize(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new CellData[Width, Height];
            _arrowPaths.Clear();
            _isHeadFirst.Clear();
            _specialCells.Clear();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = new CellData(x, y, CellType.EmptyDot, string.Empty);
                }
            }
        }

        public CellData GetCell(int x, int y) => IsValidPosition(x, y) ? _grid[x, y] : null;
        public bool IsValidPosition(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public void ExtendArrowPath(int x, int y, string arrowID, bool headFirst = false)
        {
            if (!IsValidPosition(x, y)) return;

            Vector2Int newPos = new Vector2Int(x, y);
            if (_specialCells.ContainsKey(newPos)) return;

            if (!_arrowPaths.ContainsKey(arrowID))
            {
                _arrowPaths[arrowID] = new List<Vector2Int>();
                _isHeadFirst[arrowID] = headFirst;
            }

            List<Vector2Int> path = _arrowPaths[arrowID];

            if (path.Count == 0)
            {
                if (_grid[x, y].type != CellType.EmptyDot) return;
                path.Add(newPos);
                UpdatePathVisuals(arrowID);
                return;
            }

            if (path[path.Count - 1] == newPos) return;

            if (path.Count >= 2 && path[path.Count - 2] == newPos)
            {
                Vector2Int removedPos = path[path.Count - 1];
                path.RemoveAt(path.Count - 1);
                SetCellVisual(removedPos.x, removedPos.y, CellType.EmptyDot, string.Empty);
                UpdatePathVisuals(arrowID);
                return;
            }

            Vector2Int lastPos = path[path.Count - 1];
            if (Mathf.Abs(newPos.x - lastPos.x) + Mathf.Abs(newPos.y - lastPos.y) == 1)
            {
                if (_grid[x, y].arrowID != string.Empty && _grid[x, y].arrowID != arrowID) return;
                path.Add(newPos);
                UpdatePathVisuals(arrowID);
            }
        }

        public void RemoveArrowPathFrom(int x, int y)
        {
            CellData cell = GetCell(x, y);
            if (cell == null || string.IsNullOrEmpty(cell.arrowID)) return;

            string id = cell.arrowID;
            if (!_arrowPaths.TryGetValue(id, out List<Vector2Int> path))
            {
                SetCellVisual(x, y, CellType.EmptyDot, string.Empty);
                return;
            }

            int index = path.IndexOf(new Vector2Int(x, y));
            if (index < 0) return;

            for (int i = path.Count - 1; i >= index; i--)
            {
                Vector2Int pos = path[i];
                SetCellVisual(pos.x, pos.y, CellType.EmptyDot, string.Empty);
                path.RemoveAt(i);
            }

            if (path.Count == 0)
            {
                _arrowPaths.Remove(id);
                _isHeadFirst.Remove(id);
                ForceCleanupID(id);
            }
            else
            {
                UpdatePathVisuals(id);
            }
        }

        public bool SetSpecialCell(int x, int y, BoardSpecialType type, Direction4 exitDirection, string portalId = "")
        {
            if (!IsValidPosition(x, y)) return false;
            if (_grid[x, y].arrowID != string.Empty) return false;

            Vector2Int position = new Vector2Int(x, y);
            string normalizedPortalId = (portalId ?? string.Empty).Trim();
            _specialCells[position] = new SpecialCellSaveData(position, type, exitDirection, normalizedPortalId);
            OnCellChanged?.Invoke(x, y, _grid[x, y]);
            return true;
        }

        public void RemoveSpecialCellAt(int x, int y)
        {
            if (!IsValidPosition(x, y)) return;
            if (_specialCells.Remove(new Vector2Int(x, y)))
            {
                OnCellChanged?.Invoke(x, y, _grid[x, y]);
            }
        }

        public SpecialCellSaveData GetSpecialCellAt(int x, int y)
        {
            _specialCells.TryGetValue(new Vector2Int(x, y), out SpecialCellSaveData specialCell);
            return specialCell;
        }

        public void SyncGridWithPaths()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_grid[x, y].type != CellType.None)
                    {
                        SetCellVisual(x, y, CellType.EmptyDot, string.Empty);
                    }
                }
            }

            foreach (string id in _arrowPaths.Keys)
                UpdatePathVisuals(id);
        }

        private void UpdatePathVisuals(string arrowID)
        {
            if (!_arrowPaths.TryGetValue(arrowID, out List<Vector2Int> path) || path.Count == 0) return;
            bool headFirst = _isHeadFirst[arrowID];

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int current = path[i];
                CellType type = CellType.ArrowBodyVertical;

                if (path.Count == 1)
                {
                    type = CellType.ArrowHeadUp;
                }
                else
                {
                    bool isHead = (headFirst && i == 0) || (!headFirst && i == path.Count - 1);
                    bool isTail = (headFirst && i == path.Count - 1) || (!headFirst && i == 0);

                    if (isHead)
                    {
                        Vector2Int neighbor = headFirst ? path[1] : path[path.Count - 2];
                        if (neighbor.y < current.y) type = CellType.ArrowHeadUp;
                        else if (neighbor.y > current.y) type = CellType.ArrowHeadDown;
                        else if (neighbor.x < current.x) type = CellType.ArrowHeadRight;
                        else type = CellType.ArrowHeadLeft;
                    }
                    else if (isTail)
                    {
                        Vector2Int neighbor = headFirst ? path[path.Count - 2] : path[1];
                        if (neighbor.y < current.y) type = CellType.ArrowTailUp;
                        else if (neighbor.y > current.y) type = CellType.ArrowTailDown;
                        else if (neighbor.x < current.x) type = CellType.ArrowTailRight;
                        else type = CellType.ArrowTailLeft;
                    }
                    else
                    {
                        Vector2Int prev = path[i - 1];
                        Vector2Int next = path[i + 1];
                        if (prev.x == next.x) type = CellType.ArrowBodyVertical;
                        else if (prev.y == next.y) type = CellType.ArrowBodyHorizontal;
                        else
                        {
                            bool u = prev.y > current.y || next.y > current.y;
                            bool d = prev.y < current.y || next.y < current.y;
                            bool l = prev.x < current.x || next.x < current.x;
                            bool r = prev.x > current.x || next.x > current.x;

                            if (u && r) type = CellType.ArrowCurveTopRight;
                            else if (u && l) type = CellType.ArrowCurveTopLeft;
                            else if (d && r) type = CellType.ArrowCurveBottomRight;
                            else type = CellType.ArrowCurveBottomLeft;
                        }
                    }
                }

                SetCellVisual(current.x, current.y, type, arrowID);
            }
        }

        private void ForceCleanupID(string id)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (_grid[x, y].arrowID == id)
                        SetCellVisual(x, y, CellType.EmptyDot, string.Empty);
                }
            }
        }

        private void SetCellVisual(int x, int y, CellType type, string id)
        {
            if (!IsValidPosition(x, y)) return;
            CellData cell = _grid[x, y];
            cell.type = type;
            cell.arrowID = id;
            // Không bắn event khi đang ở Silent Mode (đang nạp dữ liệu hàng loạt)
            if (!SuppressEvents)
                OnCellChanged?.Invoke(x, y, cell);
        }

        public List<ArrowSaveData> GetSaveData()
        {
            List<ArrowSaveData> result = new List<ArrowSaveData>();
            foreach (KeyValuePair<string, List<Vector2Int>> kvp in _arrowPaths)
                result.Add(new ArrowSaveData(kvp.Key, kvp.Value, _isHeadFirst[kvp.Key]));
            return result;
        }

        public List<SpecialCellSaveData> GetSpecialSaveData()
        {
            return new List<SpecialCellSaveData>(_specialCells.Values);
        }

        public void LoadFromSaveData(List<ArrowSaveData> arrows, List<SpecialCellSaveData> specialCells)
        {
            _arrowPaths.Clear();
            _isHeadFirst.Clear();
            _specialCells.Clear();

            if (arrows != null)
            {
                foreach (ArrowSaveData arrow in arrows)
                {
                    _arrowPaths[arrow.ArrowID] = new List<Vector2Int>(arrow.Path);
                    _isHeadFirst[arrow.ArrowID] = arrow.IsHeadFirst;
                    UpdatePathVisuals(arrow.ArrowID);
                }
            }

            if (specialCells != null)
            {
                foreach (SpecialCellSaveData specialCell in specialCells)
                {
                    if (!IsValidPosition(specialCell.Position.x, specialCell.Position.y)) continue;
                    _specialCells[specialCell.Position] =
                        new SpecialCellSaveData(specialCell.Position, specialCell.Type, specialCell.ExitDirection,
                            specialCell.PortalId);
                    OnCellChanged?.Invoke(specialCell.Position.x, specialCell.Position.y,
                        _grid[specialCell.Position.x, specialCell.Position.y]);
                }
            }
        }

        public List<string> GetAllArrowIDs() => new List<string>(_arrowPaths.Keys);
        public List<Vector2Int> GetArrowPath(string arrowID) => _arrowPaths.ContainsKey(arrowID) ? _arrowPaths[arrowID] : null;
        public bool IsHeadFirst(string arrowID) => _isHeadFirst.ContainsKey(arrowID) && _isHeadFirst[arrowID];

        public void FlipArrowPath(string arrowID)
        {
            if (!_arrowPaths.TryGetValue(arrowID, out List<Vector2Int> path) || path.Count == 0) return;

            _isHeadFirst[arrowID] = !_isHeadFirst[arrowID];
            UpdatePathVisuals(arrowID);
        }

        public void ClearAllPaths()
        {
            _arrowPaths.Clear();
            _isHeadFirst.Clear();
            _specialCells.Clear();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    SetCellVisual(x, y, CellType.EmptyDot, string.Empty);
                }
            }
        }
        
        public List<string> GetSpecialIDs(BoardSpecialType type)
        {
            // Dùng HashSet để tự động lọc trùng. 
            // Ví dụ: Portal 'A' chiếm 2 ô trên Grid, nhưng ta chỉ muốn hiển thị 1 item 'A' trên UI List.
            HashSet<string> uniqueIDs = new HashSet<string>();

            foreach (KeyValuePair<Vector2Int, SpecialCellSaveData> kvp in _specialCells)
            {
                if (kvp.Value.Type == type)
                {
                    // Ưu tiên dùng PortalId nếu có, nếu không thì dùng tọa độ (để đảm bảo tính duy nhất trong danh sách)
                    if (!string.IsNullOrEmpty(kvp.Value.PortalId))
                    {
                        uniqueIDs.Add(kvp.Value.PortalId);
                    }
                    else
                    {
                        uniqueIDs.Add($"{kvp.Key.x},{kvp.Key.y}");
                    }
                }
            }

            // Ép kiểu về List để UI dễ sử dụng
            return new List<string>(uniqueIDs);
        }
        
    }
}
