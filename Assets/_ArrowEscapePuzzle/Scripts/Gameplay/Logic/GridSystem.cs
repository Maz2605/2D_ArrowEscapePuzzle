using System.Collections.Generic;
using ArrowGame.Data;
using ArrowGame.Data.Events;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
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

        // ================= INIT & DATA POOLING =================

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
            if (levelData.Cells == null) return;

            foreach (var cell in levelData.Cells)
            {
                if (!IsValidPosition(cell.x, cell.y)) continue;

                var arrow = _grid[cell.x, cell.y];
                arrow.SetData(cell.arrowID, cell.type); 

                if (!string.IsNullOrEmpty(cell.arrowID))
                {
                    if (!_arrowGroups.ContainsKey(cell.arrowID))
                    {
                        _arrowGroups[cell.arrowID] = new List<ArrowData>();
                    }
                    _arrowGroups[cell.arrowID].Add(arrow);
                }

                if (IsHeadType(cell.type))
                {
                    RemainingArrows++;
                }
            }
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);
            // Debug.Log($"[Logic] Loaded {RemainingArrows} arrows");
            SortAllArrowGroups();
        }

        // ================= PATH TRACING ALGORITHM =================
        
        private void SortAllArrowGroups()
        {
            List<string> keys = new List<string>(_arrowGroups.Keys);
            foreach (string id in keys)
            {
                _arrowGroups[id] = SortSingleGroup(_arrowGroups[id]);
            }
        }

        private List<ArrowData> SortSingleGroup(List<ArrowData> rawList)
        {
            if (rawList.Count <= 1) return rawList;

            List<ArrowData> sortedList = new List<ArrowData>();
            ArrowData current = rawList.Find(a => IsHeadType(a.Type));
            
            if (current == null) 
            {
                Debug.LogWarning($"[Logic] Group {rawList[0].ID} không có Head Type!");
                return rawList;
            }

            sortedList.Add(current);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (sortedList.Count < rawList.Count)
            {
                ArrowData next = null;

                foreach (var dir in directions)
                {
                    int checkX = current.X + dir.x;
                    int checkY = current.Y + dir.y;

                    ArrowData neighbor = GetArrow(checkX, checkY);

                    if (neighbor != null && neighbor.ID == current.ID && !sortedList.Contains(neighbor))
                    {
                        next = neighbor;
                        break;
                    }
                }

                if (next != null)
                {
                    sortedList.Add(next);
                    current = next;
                }
                else
                {
                    Debug.LogWarning($"[Logic] Mũi tên {current.ID} bị đứt đoạn khúc giữa map!");
                    break; 
                }
            }

            sortedList.Reverse();
            return sortedList;
        }

        // ================= GAMEPLAY CORE =================

        public void TryMoveArrow(int startX, int startY)
        {
            var startArrow = GetArrow(startX, startY);

            if (startArrow == null || startArrow.ID ==  EMPTY_ID)
                return;

            ArrowData headArrow = GetHeadOfGroup(startArrow.ID);
            
            Vector2Int direction = GetDirectionFromType(headArrow.Type);

            if (CanArrowEscape(headArrow, direction))
            {
                RemoveEntireArrow(headArrow);
            }
            else
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowBlocked, headArrow);
            }
        }

        private bool CanArrowEscape(ArrowData startArrow, Vector2Int direction)
        {
            int checkX = startArrow.X + direction.x;
            int checkY = startArrow.Y + direction.y;
            string startID = startArrow.ID;

            while (IsValidPosition(checkX, checkY))
            {
                var cell = _grid[checkX, checkY];

                if (cell.ID != EMPTY_ID && cell.ID != startID)
                {
                    return false;
                }

                checkX += direction.x;
                checkY += direction.y;
            }

            return true;
        }

        private void RemoveEntireArrow(ArrowData headArrow)
        {
            string targetID = headArrow.ID;

            if (string.IsNullOrEmpty(targetID) || !_arrowGroups.ContainsKey(targetID))
            {
                _grid[headArrow.X, headArrow.Y].ResetData(); 
                return;
            }

            var group = _arrowGroups[targetID];
            
            EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowEscaped, group);
            
            foreach (var arrow in group)
            {
                arrow.ResetData(); 
            }

            _arrowGroups.Remove(targetID);
            RemainingArrows = Mathf.Max(0, RemainingArrows - 1);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);
            // Debug.Log("[Logic] Count Arrow: " + RemainingArrows);
            if (IsBoardEmpty())
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
            }
        }

        // ================= HELPERS =================

        public bool IsValidPosition(int x, int y)
            => x >= 0 && x < Width && y >= 0 && y < Height;

        public ArrowData GetArrow(int x, int y)
            => IsValidPosition(x, y) ? _grid[x, y] : null;

        public bool IsBoardEmpty()
            => RemainingArrows <= 0;

        private bool IsHeadType(CellType type)
        {
            return type == CellType.ArrowHeadUp || type == CellType.ArrowHeadDown ||
                   type == CellType.ArrowHeadLeft || type == CellType.ArrowHeadRight;
        }

        private ArrowData GetHeadOfGroup(string targetID)
        {
            if (_arrowGroups.TryGetValue(targetID, out List<ArrowData> group))
            {
                for (int i = 0; i < group.Count; i++)
                {
                    if (IsHeadType(group[i].Type))
                    {
                        return group[i];
                    }
                }
            }
            return null;
        }

        private Vector2Int GetDirectionFromType(CellType type)
        {
            return type switch
            {
                CellType.ArrowHeadUp => new Vector2Int(0, 1),
                CellType.ArrowHeadDown => new Vector2Int(0, -1),
                CellType.ArrowHeadLeft => new Vector2Int(-1, 0),
                CellType.ArrowHeadRight => new Vector2Int(1, 0),
                _ => Vector2Int.zero
            };
        }
        
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
                if (cell.ID != EMPTY_ID && cell.ID != startID)
                {
                    break; 
                }
                emptySpaces++;
                checkX += direction.x;
                checkY += direction.y;
            }

            return emptySpaces;
        }
        
        public string GetArrowIdAt(int x, int y)
        {
            var arrow = GetArrow(x, y);
            return (arrow != null && !string.IsNullOrEmpty(arrow.ID)) ? arrow.ID : null;
        }

        public void ForceRemoveArrow(string targetID)
        {
            if (string.IsNullOrEmpty(targetID) || !_arrowGroups.ContainsKey(targetID))
            {
                return; 
            }

            var group = _arrowGroups[targetID];
            
            
            foreach (var arrow in group)
            {
                arrow.ResetData(); 
            }

            _arrowGroups.Remove(targetID);
            RemainingArrows = Mathf.Max(0, RemainingArrows - 1);

            if (IsBoardEmpty())
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
            }
        }
        
        public IReadOnlyDictionary<string, List<ArrowData>> ArrowGroups => _arrowGroups;    }
}