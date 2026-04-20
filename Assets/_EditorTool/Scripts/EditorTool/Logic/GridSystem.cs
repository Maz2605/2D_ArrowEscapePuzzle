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
        public int Width { get; private set; }
        public int Height { get; private set; }

        public event Action<int, int, CellData> OnCellChanged;

        private Dictionary<string, List<Vector2Int>> _arrowPaths = new Dictionary<string, List<Vector2Int>>();
        private Dictionary<string, bool> _isHeadFirst = new Dictionary<string, bool>();

        public void Initialize(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new CellData[Width, Height];
            _arrowPaths.Clear();
            _isHeadFirst.Clear();
            
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
            if (!_arrowPaths.ContainsKey(arrowID))
            {
                _arrowPaths[arrowID] = new List<Vector2Int>();
                _isHeadFirst[arrowID] = headFirst;
            }

            var path = _arrowPaths[arrowID];
            Vector2Int newPos = new Vector2Int(x, y);

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
            var cell = GetCell(x, y);
            if (cell == null || string.IsNullOrEmpty(cell.arrowID)) return;

            string id = cell.arrowID;
            if (!_arrowPaths.TryGetValue(id, out var path)) 
            {
                SetCellVisual(x, y, CellType.EmptyDot, string.Empty);
                return;
            }

            int index = path.IndexOf(new Vector2Int(x, y));
            if (index >= 0)
            {
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
                else UpdatePathVisuals(id);
            }
        }

        private void ForceCleanupID(string id)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_grid[x, y].arrowID == id)
                        SetCellVisual(x, y, CellType.EmptyDot, string.Empty);
        }

        public void SyncGridWithPaths()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_grid[x, y].type != CellType.None)
                        SetCellVisual(x, y, CellType.EmptyDot, string.Empty);

            foreach (var id in _arrowPaths.Keys)
                UpdatePathVisuals(id);
        }

        private void UpdatePathVisuals(string arrowID)
        {
            if (!_arrowPaths.TryGetValue(arrowID, out var path) || path.Count == 0) return;
            bool headFirst = _isHeadFirst[arrowID];

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int current = path[i];
                CellType type = CellType.ArrowBodyVertical;

                if (path.Count == 1) type = CellType.ArrowHeadUp;
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

        private void SetCellVisual(int x, int y, CellType type, string id)
        {
            if (!IsValidPosition(x, y)) return;
            var cell = _grid[x, y];
            cell.type = type;
            cell.arrowID = id;
            OnCellChanged?.Invoke(x, y, cell);
        }

        public List<ArrowSaveData> GetSaveData()
        {
            List<ArrowSaveData> result = new List<ArrowSaveData>();
            foreach (var kvp in _arrowPaths)
                result.Add(new ArrowSaveData(kvp.Key, kvp.Value, _isHeadFirst[kvp.Key]));
            return result;
        }

        public void LoadFromSaveData(List<ArrowSaveData> arrows)
        {
            _arrowPaths.Clear();
            _isHeadFirst.Clear();
            foreach (var a in arrows)
            {
                _arrowPaths[a.ArrowID] = new List<Vector2Int>(a.Path);
                _isHeadFirst[a.ArrowID] = a.IsHeadFirst;
                UpdatePathVisuals(a.ArrowID);
            }
        }

        public List<string> GetAllArrowIDs() => new List<string>(_arrowPaths.Keys);
        public List<Vector2Int> GetArrowPath(string arrowID) => _arrowPaths.ContainsKey(arrowID) ? _arrowPaths[arrowID] : null;
        public bool IsHeadFirst(string arrowID) => _isHeadFirst.ContainsKey(arrowID) && _isHeadFirst[arrowID];
        
        public void FlipArrowPath(string arrowID)
        {
            if (!_arrowPaths.TryGetValue(arrowID, out var path) || path.Count == 0) return;
            
            // CHỈ đổi flag Visual, không làm đảo thứ tự Array để giữ nguyên điểm nối vẽ
            _isHeadFirst[arrowID] = !_isHeadFirst[arrowID];
            UpdatePathVisuals(arrowID);
        }
        
        public void ClearAllPaths()
        {
            _arrowPaths.Clear();
            _isHeadFirst.Clear();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    SetCellVisual(x, y, CellType.EmptyDot, string.Empty);
        }
    }
}