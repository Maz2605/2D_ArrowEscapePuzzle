using System;
using System.Collections.Generic;
using ShareCore.Data;
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
            
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = new CellData(x, y, CellType.EmptyDot, string.Empty);
                }
            }
        }

        // --- CÁC HÀM CƠ BẢN ---
        public void SetCell(int x, int y, CellType type, string arrowID = "")
        {
            if (!IsValidPosition(x, y)) return;
            var cell = _grid[x, y];
            if (cell.type == type && cell.arrowID == arrowID) return;

            cell.type = type;
            cell.arrowID = arrowID;
            OnCellChanged?.Invoke(x, y, cell);
        }

        public CellData GetCell(int x, int y) => IsValidPosition(x, y) ? _grid[x, y] : null;
        private bool IsValidPosition(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public void ExtendArrowPath(int x, int y, string arrowID, bool headFirst = false)
        {
            if (!IsValidPosition(x, y)) return;

            if (!_arrowPaths.ContainsKey(arrowID)) _arrowPaths[arrowID] = new List<Vector2Int>();

            var path = _arrowPaths[arrowID];
            Vector2Int newPos = new Vector2Int(x, y);

            if (path.Count == 0)
            {
                if (_grid[x, y].type != CellType.EmptyDot && _grid[x, y].type != CellType.None) return;
                
                // Lưu lại hướng vẽ khi chạm nét đầu tiên
                _isHeadFirst[arrowID] = headFirst; 
                path.Add(newPos);
                UpdatePathVisuals(arrowID);
                return;
            }

            if (path[path.Count - 1] == newPos) return;

            if (path.Count >= 2 && path[path.Count - 2] == newPos)
            {
                Vector2Int removedPos = path[path.Count - 1];
                path.RemoveAt(path.Count - 1);
                SetCell(removedPos.x, removedPos.y, CellType.EmptyDot, string.Empty); 
                UpdatePathVisuals(arrowID);
                return;
            }

            Vector2Int lastPos = path[path.Count - 1];
            if (Mathf.Abs(newPos.x - lastPos.x) + Mathf.Abs(newPos.y - lastPos.y) == 1)
            {
                if (_grid[x, y].type != CellType.EmptyDot && _grid[x, y].type != CellType.None) return;
                path.Add(newPos);
                UpdatePathVisuals(arrowID);
            }
        }

        public void RemoveArrowPathFrom(int x, int y)
        {
            var cell = GetCell(x, y);
            if (cell == null || string.IsNullOrEmpty(cell.arrowID)) return;

            string id = cell.arrowID;
            if (!_arrowPaths.ContainsKey(id)) return;

            var path = _arrowPaths[id];
            int index = path.IndexOf(new Vector2Int(x, y));
            
            if (index >= 0)
            {
                for (int i = path.Count - 1; i >= index; i--)
                {
                    SetCell(path[i].x, path[i].y, CellType.EmptyDot, string.Empty);
                    path.RemoveAt(i);
                }

                if (path.Count == 0)
                {
                    // Xóa hết path → dọn hoàn toàn khỏi dictionary
                    // Nếu còn key rỗng, GetAllArrowIDs() vẫn trả về id này
                    // → CleanupStaleLines sẽ không despawn line renderer (bug)
                    _arrowPaths.Remove(id);
                    _isHeadFirst.Remove(id);
                }
                else
                {
                    UpdatePathVisuals(id);
                }
            }
        }

        public bool IsHeadFirst(string arrowID)
        {
            return _isHeadFirst.ContainsKey(arrowID) && _isHeadFirst[arrowID];
        }

        private void UpdatePathVisuals(string arrowID)
        {
            var path = _arrowPaths[arrowID];
            if (path.Count == 0) return;

            bool headFirst = IsHeadFirst(arrowID);

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
                            bool hasUp = prev.y > current.y || next.y > current.y;
                            bool hasDown = prev.y < current.y || next.y < current.y;
                            bool hasLeft = prev.x < current.x || next.x < current.x;
                            bool hasRight = prev.x > current.x || next.x > current.x;

                            if (hasUp && hasRight) type = CellType.ArrowCurveTopRight;
                            else if (hasUp && hasLeft) type = CellType.ArrowCurveTopLeft;
                            else if (hasDown && hasRight) type = CellType.ArrowCurveBottomRight;
                            else if (hasDown && hasLeft) type = CellType.ArrowCurveBottomLeft;
                        }
                    }
                }

                SetCell(current.x, current.y, type, arrowID);
            }
        }

        public List<Vector2Int> GetArrowPath(string arrowID)
        {
            if (_arrowPaths.ContainsKey(arrowID))
                return _arrowPaths[arrowID];
            return null;
        }

        public List<string> GetAllArrowIDs()
        {
            return new List<string>(_arrowPaths.Keys);
        }
        
        public void ClearAllPaths()
        {
            _arrowPaths.Clear();
            _isHeadFirst.Clear();
            
            // Trả toàn bộ ô về EmptyDot (Đường đi xám)
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = _grid[x, y];
                    cell.type = CellType.EmptyDot;
                    cell.arrowID = string.Empty;
                    OnCellChanged?.Invoke(x, y, cell);
                }
            }
        }
        
        public void ReconstructPathsFromGrid()
        {
            _arrowPaths.Clear();
            _isHeadFirst.Clear();

            // 1. Lấy tất cả ID đang có trên map
            HashSet<string> activeIDs = new HashSet<string>();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (!string.IsNullOrEmpty(_grid[x, y].arrowID)) 
                        activeIDs.Add(_grid[x, y].arrowID);
                }
            }

            // 2. Mò đường cho từng ID (Từ Đầu -> Đuôi)
            foreach (string id in activeIDs)
            {
                Vector2Int headPos = new Vector2Int(-1, -1);

                // Tìm vị trí của cái Đầu
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (_grid[x, y].arrowID == id && IsHead(_grid[x, y].type))
                        {
                            headPos = new Vector2Int(x, y);
                            break;
                        }
                    }

                    if (headPos.x != -1) break;
                }

                if (headPos.x != -1)
                {
                    List<Vector2Int> path = new List<Vector2Int>();
                    Vector2Int curr = headPos;
                    Vector2Int prev = new Vector2Int(-1, -1);

                    // Thay thế vòng lặp while(true) trong hàm ReconstructPathsFromGrid thành:
                    while (true)
                    {
                        path.Add(curr);
                        if (IsTail(_grid[curr.x, curr.y].type)) break; // Chạm đuôi thì xong

                        Vector2Int next = new Vector2Int(-1, -1);
                        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

                        foreach (var d in dirs)
                        {
                            Vector2Int n = curr + d;
                            if (IsValidPosition(n.x, n.y) && n != prev && _grid[n.x, n.y].arrowID == id)
                            {
                                next = n;
                                break;
                            }
                        }

                        // ĐÃ THÊM: path.Contains(next) để chặn đứng vòng lặp vô hạn nếu designer vẽ 1 hình vuông khép kín
                        if (next.x == -1 || path.Contains(next)) break;
                        prev = curr;
                        curr = next;
                    }
                    _arrowPaths[id] = path;
                    CellType firstCellType = _grid[path[0].x, path[0].y].type;
                    _isHeadFirst[id] = IsHead(firstCellType);

                }
            }
        }
        // ==========================================
        // THÊM HÀM MỚI NÀY VÀO DƯỚI CÙNG GRIDSYSTEM.CS
        // ==========================================
        public void ForceRefreshVisual()
        {
            if (_grid == null) return;

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    OnCellChanged?.Invoke(x, y, _grid[x, y]);
                }
            }

            if (Width > 0 && Height > 0)
            {
                OnCellChanged?.Invoke(0, 0, _grid[0, 0]);
            }
        }

        private bool IsHead(CellType type) => type == CellType.ArrowHeadUp || type == CellType.ArrowHeadDown || type == CellType.ArrowHeadLeft || type == CellType.ArrowHeadRight;
        private bool IsTail(CellType type) => type == CellType.ArrowTailUp || type == CellType.ArrowTailDown || type == CellType.ArrowTailLeft || type == CellType.ArrowTailRight;
        
        public void FlipArrowPath(string arrowID)
        {
            if (!_arrowPaths.ContainsKey(arrowID) || _arrowPaths[arrowID].Count == 0) return;

            // 1. Đảo ngược danh sách tọa độ
            _arrowPaths[arrowID].Reverse();

            // 2. Đảo ngược flag hướng
            if (_isHeadFirst.ContainsKey(arrowID))
            {
                _isHeadFirst[arrowID] = !_isHeadFirst[arrowID];
            }
            else
            {
                _isHeadFirst[arrowID] = true; // Mặc định nếu chưa có
            }

            UpdatePathVisuals(arrowID);
        }
    }
}