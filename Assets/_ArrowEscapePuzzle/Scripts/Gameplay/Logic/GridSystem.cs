using System.Collections.Generic;
using System.Linq;
using ArrowGame.Data.Events;
using ArrowGame.Utils;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public class GridSystem
    {
        private readonly ArrowData[,] _grid;
        private readonly Dictionary<string, List<ArrowData>> _arrowGroups;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int RemainingArrows { get; private set; }

        private const string EMPTY_ID = "";

        public GridSystem(LevelSaveData levelData)
        {
            if (levelData == null) return;

            Width = levelData.Width;
            Height = levelData.Height;

            _grid = new ArrowData[Width, Height];
            _arrowGroups = new Dictionary<string, List<ArrowData>>();

            RemainingArrows = 0;

            InitEmptyGrid();
            LoadLevel(levelData);
        }

        private void InitEmptyGrid()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = new ArrowData(EMPTY_ID, x, y, CellType.EmptyDot);
                }
            }
        }

        private void LoadLevel(LevelSaveData levelData)
        {
            // Refactor: Đọc trực tiếp từ List Arrows thay vì Cells rời rạc
            if (levelData.Arrows == null || levelData.Arrows.Count == 0) return;

            foreach (var arrowSave in levelData.Arrows)
            {
                string id = arrowSave.ArrowID;
                if (string.IsNullOrEmpty(id)) continue;

                _arrowGroups[id] = new List<ArrowData>();

                for (int i = 0; i < arrowSave.Path.Count; i++)
                {
                    Vector2Int pos = arrowSave.Path[i];
                    if (!IsValidPosition(pos.x, pos.y)) continue;

                    ArrowData node = _grid[pos.x, pos.y];
                    
                    // Tự động tính toán CellType (Head/Tail/Curve) từ vị trí trong List
                    CellType calculatedType = CalculateCellType(i, arrowSave.Path, arrowSave.IsHeadFirst);
                    
                    node.SetData(id, calculatedType);
                    _arrowGroups[id].Add(node);
                }

                RemainingArrows++;
            }

            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);
        }

        private CellType CalculateCellType(int index, List<Vector2Int> path, bool isHeadFirst)
        {
            if (path.Count == 1) return CellType.ArrowHeadUp;

            bool isHead = (isHeadFirst && index == 0) || (!isHeadFirst && index == path.Count - 1);
            bool isTail = (isHeadFirst && index == path.Count - 1) || (!isHeadFirst && index == 0);

            Vector2Int current = path[index];

            if (isHead)
            {
                Vector2Int neighbor = isHeadFirst ? path[1] : path[path.Count - 2];
                if (neighbor.y < current.y) return CellType.ArrowHeadUp;
                if (neighbor.y > current.y) return CellType.ArrowHeadDown;
                if (neighbor.x < current.x) return CellType.ArrowHeadRight;
                return CellType.ArrowHeadLeft;
            }
            
            if (isTail)
            {
                Vector2Int neighbor = isHeadFirst ? path[path.Count - 2] : path[1];
                if (neighbor.y < current.y) return CellType.ArrowTailUp;
                if (neighbor.y > current.y) return CellType.ArrowTailDown;
                if (neighbor.x < current.x) return CellType.ArrowTailRight;
                return CellType.ArrowTailLeft;
            }

            Vector2Int prev = path[index - 1];
            Vector2Int next = path[index + 1];

            if (prev.x == next.x) return CellType.ArrowBodyVertical;
            if (prev.y == next.y) return CellType.ArrowBodyHorizontal;

            bool hasUp = prev.y > current.y || next.y > current.y;
            bool hasDown = prev.y < current.y || next.y < current.y;
            bool hasLeft = prev.x < current.x || next.x < current.x;
            bool hasRight = prev.x > current.x || next.x > current.x;

            if (hasUp && hasRight) return CellType.ArrowCurveTopRight;
            if (hasUp && hasLeft) return CellType.ArrowCurveTopLeft;
            if (hasDown && hasRight) return CellType.ArrowCurveBottomRight;
            return CellType.ArrowCurveBottomLeft;
        }

        // ================= CORE GAMEPLAY =================

        public void TryMoveArrow(int startX, int startY)
        {
            var startArrow = GetArrow(startX, startY);
            if (startArrow == null || startArrow.ID == EMPTY_ID) return;

            ArrowData headArrow = GetHeadOfGroup(startArrow.ID);
            if (headArrow == null) return;

            Vector2Int direction = GetDirectionFromType(headArrow.Type);

            if (CanArrowEscape(headArrow, direction))
                RemoveEntireArrow(headArrow);
            else
                EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowBlocked, headArrow);
        }

        private bool CanArrowEscape(ArrowData startArrow, Vector2Int direction)
        {
            int checkX = startArrow.X + direction.x;
            int checkY = startArrow.Y + direction.y;
            string startID = startArrow.ID;

            while (IsValidPosition(checkX, checkY))
            {
                var cell = _grid[checkX, checkY];
                if (cell.ID != EMPTY_ID && cell.ID != startID) return false;
                checkX += direction.x;
                checkY += direction.y;
            }
            return true;
        }

        private void RemoveEntireArrow(ArrowData headArrow)
        {
            string targetID = headArrow.ID;
            if (string.IsNullOrEmpty(targetID) || !_arrowGroups.TryGetValue(targetID, out var group)) return;

            EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowEscaped, group);

            foreach (var arrow in group) arrow.ResetData();

            _arrowGroups.Remove(targetID);
            RemainingArrows = Mathf.Max(0, RemainingArrows - 1);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);

            if (IsBoardEmpty()) EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
        }

        // ================= HELPERS (Phục vụ Booster & Logic) =================

        public bool IsValidPosition(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public ArrowData GetArrow(int x, int y) => IsValidPosition(x, y) ? _grid[x, y] : null;
        public bool IsBoardEmpty() => RemainingArrows <= 0;

        private bool IsHeadType(CellType type) => 
            type == CellType.ArrowHeadUp || type == CellType.ArrowHeadDown ||
            type == CellType.ArrowHeadLeft || type == CellType.ArrowHeadRight;

        public ArrowData GetHeadOfGroup(string targetID)
        {
            if (_arrowGroups.TryGetValue(targetID, out List<ArrowData> group))
                return group.Find(a => IsHeadType(a.Type));
            return null;
        }

        private Vector2Int GetDirectionFromType(CellType type) => type switch
        {
            CellType.ArrowHeadUp => new Vector2Int(0, 1),
            CellType.ArrowHeadDown => new Vector2Int(0, -1),
            CellType.ArrowHeadLeft => new Vector2Int(-1, 0),
            CellType.ArrowHeadRight => new Vector2Int(1, 0),
            _ => Vector2Int.zero
        };

        public int GetEmptyCellsBeforeBlock(ArrowData headArrow)
        {
            Vector2Int direction = GetDirectionFromType(headArrow.Type);
            int checkX = headArrow.X + direction.x;
            int checkY = headArrow.Y + direction.y;
            string startID = headArrow.ID;
            int emptySpaces = 0;

            while (IsValidPosition(checkX, checkY))
            {
                var cell = _grid[checkX, checkY];
                if (cell.ID != EMPTY_ID && cell.ID != startID) break;
                emptySpaces++;
                checkX += direction.x;
                checkY += direction.y;
            }
            return emptySpaces;
        }

        public string GetArrowIdAt(int x, int y) => GetArrow(x, y)?.ID;

        // --- Hàm dành riêng cho Booster ---

        public void ForceRemoveArrow(string targetID)
        {
            if (string.IsNullOrEmpty(targetID) || !_arrowGroups.TryGetValue(targetID, out var group)) return;

            EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowForceRemove, group);
            foreach (var arrow in group) arrow.ResetData();

            _arrowGroups.Remove(targetID);
            RemainingArrows = Mathf.Max(0, RemainingArrows - 1);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);

            if (IsBoardEmpty()) EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
        }

        public ArrowData GetOneEscapableArrow()
        {
            foreach (var group in _arrowGroups.Values)
            {
                var head = group.Find(a => IsHeadType(a.Type));
                if (head != null && CanArrowEscape(head, GetDirectionFromType(head.Type)))
                    return head;
            }
            return null;
        }

        public List<ArrowData> GetMultipleEscapableArrows(int count)
        {
            List<ArrowData> escapable = new List<ArrowData>();
            List<ArrowData> blocked = new List<ArrowData>();

            foreach (var group in _arrowGroups.Values)
            {
                var head = group.Find(a => IsHeadType(a.Type));
                if (head == null) continue;

                if (CanArrowEscape(head, GetDirectionFromType(head.Type))) escapable.Add(head);
                else blocked.Add(head);
            }

            escapable.Shuffle();
            blocked.Shuffle();

            List<ArrowData> results = new List<ArrowData>();
            results.AddRange(escapable.Take(count));
            if (results.Count < count)
                results.AddRange(blocked.Take(count - results.Count));

            return results;
        }

        public List<string> GetAllArrowIdsByType(CellType targetType, string excludeId)
        {
            List<string> results = new List<string>();
            foreach (var kvp in _arrowGroups)
            {
                if (kvp.Key == excludeId) continue;
                var head = kvp.Value.Find(a => IsHeadType(a.Type));
                if (head != null && head.Type == targetType) results.Add(kvp.Key);
            }
            return results;
        }

        public IReadOnlyDictionary<string, List<ArrowData>> ArrowGroups => _arrowGroups;
    }
}