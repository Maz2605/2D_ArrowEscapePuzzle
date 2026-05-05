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
        private readonly Dictionary<Vector2Int, SpecialCellSaveData> _specialCellsByPosition;
        private readonly Dictionary<string, List<SpecialCellSaveData>> _portalGroups;
        private readonly Dictionary<string, EscapeTraceResult> _traceCache;

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
            _specialCellsByPosition = new Dictionary<Vector2Int, SpecialCellSaveData>();
            _portalGroups = new Dictionary<string, List<SpecialCellSaveData>>();
            _traceCache = new Dictionary<string, EscapeTraceResult>();

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
            if (levelData.Arrows != null && levelData.Arrows.Count > 0)
            {
                foreach (ArrowSaveData arrowSave in levelData.Arrows)
                {
                    string id = arrowSave.ArrowID;
                    if (string.IsNullOrEmpty(id)) continue;

                    _arrowGroups[id] = new List<ArrowData>();

                    for (int i = 0; i < arrowSave.Path.Count; i++)
                    {
                        Vector2Int pos = arrowSave.Path[i];
                        if (!IsValidPosition(pos.x, pos.y)) continue;

                        ArrowData node = _grid[pos.x, pos.y];
                        CellType calculatedType = CalculateCellType(i, arrowSave.Path, arrowSave.IsHeadFirst);

                        node.SetData(id, calculatedType);
                        _arrowGroups[id].Add(node);
                    }

                    RemainingArrows++;
                }
            }

            LoadSpecialCells(levelData.SpecialCells);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);
        }

        private void LoadSpecialCells(List<SpecialCellSaveData> specialCells)
        {
            _specialCellsByPosition.Clear();
            _portalGroups.Clear();

            if (specialCells == null) return;

            foreach (SpecialCellSaveData specialCell in specialCells)
            {
                if (specialCell == null || !IsValidPosition(specialCell.Position.x, specialCell.Position.y)) continue;

                Vector2Int position = specialCell.Position;
                if (_grid[position.x, position.y].ID != EMPTY_ID)
                {
                    Debug.LogWarning(
                        $"[GridSystem] Skip special cell {specialCell.Type} at occupied cell ({position.x}, {position.y}).");
                    continue;
                }

                SpecialCellSaveData normalized = new SpecialCellSaveData(position, specialCell.Type,
                    specialCell.ExitDirection, specialCell.PortalId);

                _specialCellsByPosition[position] = normalized;

                if (normalized.Type == BoardSpecialType.Portal)
                {
                    string portalId = normalized.PortalId ?? string.Empty;
                    if (!_portalGroups.TryGetValue(portalId, out List<SpecialCellSaveData> group))
                    {
                        group = new List<SpecialCellSaveData>();
                        _portalGroups.Add(portalId, group);
                    }

                    group.Add(normalized);
                }
            }
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

        public void TryMoveArrow(int startX, int startY)
        {
            ArrowData startArrow = GetArrow(startX, startY);
            if (startArrow == null || startArrow.ID == EMPTY_ID) return;

            ArrowData headArrow = GetHeadOfGroup(startArrow.ID);
            if (headArrow == null) return;

            EscapeTraceResult traceResult = TraceEscapeRoute(headArrow);
            _traceCache[headArrow.ID] = traceResult;

            if (traceResult.CanEscape)
                RemoveEntireArrow(headArrow);
            else
                EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowBlocked, headArrow);
        }

        public EscapeTraceResult TraceEscapeRoute(ArrowData headArrow)
        {
            Direction4 initialDirection = GetDirectionFromType(headArrow.Type);
            EscapeTraceResult result = new EscapeTraceResult(headArrow.ID, initialDirection);

            if (string.IsNullOrEmpty(headArrow.ID)) return result;

            Vector2Int direction = initialDirection.ToVector2Int();
            int checkX = headArrow.X + direction.x;
            int checkY = headArrow.Y + direction.y;
            string startId = headArrow.ID;
            HashSet<string> visitedStates = new HashSet<string>();

            while (true)
            {
                if (!IsValidPosition(checkX, checkY))
                {
                    result.CanEscape = true;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    return result;
                }

                string stateKey = $"{checkX}:{checkY}:{direction.x}:{direction.y}";
                if (!visitedStates.Add(stateKey))
                {
                    result.BlockReason = EscapeBlockReason.Loop;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    return result;
                }

                ArrowData cell = _grid[checkX, checkY];
                if (cell.ID != EMPTY_ID && cell.ID != startId)
                {
                    result.BlockReason = EscapeBlockReason.OtherArrow;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    return result;
                }

                Vector2Int currentPosition = new Vector2Int(checkX, checkY);
                result.AddWaypoint(currentPosition, 1f);

                if (_specialCellsByPosition.TryGetValue(currentPosition, out SpecialCellSaveData specialCell))
                {
                    if (specialCell.Type == BoardSpecialType.Redirect)
                    {
                        direction = specialCell.ExitDirection.ToVector2Int();
                        result.FinalDirection = specialCell.ExitDirection;
                        checkX = currentPosition.x + direction.x;
                        checkY = currentPosition.y + direction.y;
                        continue;
                    }

                    if (specialCell.Type == BoardSpecialType.Portal)
                    {
                        SpecialCellSaveData exitPortal = ResolveExitPortal(specialCell);
                        if (exitPortal == null)
                        {
                            result.BlockReason = EscapeBlockReason.InvalidPortal;
                            result.FinalDirection = Direction4Extensions.FromVector(direction);
                            return result;
                        }

                        Vector2Int exitPosition = exitPortal.Position;
                        if (exitPosition != currentPosition)
                        {
                            result.AddWaypoint(exitPosition, 0f, true);
                        }

                        direction = exitPortal.ExitDirection.ToVector2Int();
                        result.FinalDirection = exitPortal.ExitDirection;
                        checkX = exitPosition.x + direction.x;
                        checkY = exitPosition.y + direction.y;
                        continue;
                    }
                }

                result.FinalDirection = Direction4Extensions.FromVector(direction);
                checkX += direction.x;
                checkY += direction.y;
            }
        }

        private SpecialCellSaveData ResolveExitPortal(SpecialCellSaveData entryPortal)
        {
            string portalId = entryPortal?.PortalId ?? string.Empty;
            if (string.IsNullOrEmpty(portalId)) return null;
            if (!_portalGroups.TryGetValue(portalId, out List<SpecialCellSaveData> group)) return null;
            if (group.Count != 2) return null;

            if (group[0].Position == entryPortal.Position) return group[1];
            if (group[1].Position == entryPortal.Position) return group[0];
            return null;
        }

        private void RemoveEntireArrow(ArrowData headArrow)
        {
            string targetID = headArrow.ID;
            if (string.IsNullOrEmpty(targetID) || !_arrowGroups.TryGetValue(targetID, out List<ArrowData> group)) return;

            EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowEscaped, group);

            foreach (ArrowData arrow in group) arrow.ResetData();

            _arrowGroups.Remove(targetID);
            _traceCache.Remove(targetID);
            RemainingArrows = Mathf.Max(0, RemainingArrows - 1);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);

            if (IsBoardEmpty()) EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
        }

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

        public Direction4 GetDirectionFromType(CellType type) => type switch
        {
            CellType.ArrowHeadUp => Direction4.Up,
            CellType.ArrowHeadDown => Direction4.Down,
            CellType.ArrowHeadLeft => Direction4.Left,
            CellType.ArrowHeadRight => Direction4.Right,
            _ => Direction4.Up
        };

        public int GetEmptyCellsBeforeBlock(ArrowData headArrow)
        {
            if (headArrow == null) return 0;
            EscapeTraceResult trace = TraceEscapeRoute(headArrow);
            _traceCache[headArrow.ID] = trace;
            return trace.DistanceBeforeStop;
        }

        public string GetArrowIdAt(int x, int y) => GetArrow(x, y)?.ID;

        public SpecialCellSaveData GetSpecialCellAt(int x, int y)
        {
            _specialCellsByPosition.TryGetValue(new Vector2Int(x, y), out SpecialCellSaveData specialCell);
            return specialCell;
        }

        public EscapeTraceResult GetCachedTraceResult(string arrowId)
        {
            if (string.IsNullOrEmpty(arrowId)) return null;
            _traceCache.TryGetValue(arrowId, out EscapeTraceResult trace);
            return trace;
        }

        public EscapeTraceResult GetLiveTraceResult(string arrowId)
        {
            ArrowData head = GetHeadOfGroup(arrowId);
            if (head == null) return null;

            EscapeTraceResult trace = TraceEscapeRoute(head);
            _traceCache[arrowId] = trace;
            return trace;
        }

        public void ForceRemoveArrow(string targetID)
        {
            if (string.IsNullOrEmpty(targetID) || !_arrowGroups.TryGetValue(targetID, out List<ArrowData> group)) return;

            EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowForceRemove, group);
            foreach (ArrowData arrow in group) arrow.ResetData();

            _arrowGroups.Remove(targetID);
            _traceCache.Remove(targetID);
            RemainingArrows = Mathf.Max(0, RemainingArrows - 1);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);

            if (IsBoardEmpty()) EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
        }

        public ArrowData GetOneEscapableArrow()
        {
            foreach (List<ArrowData> group in _arrowGroups.Values)
            {
                ArrowData head = group.Find(a => IsHeadType(a.Type));
                if (head == null) continue;

                EscapeTraceResult trace = TraceEscapeRoute(head);
                _traceCache[head.ID] = trace;
                if (trace.CanEscape) return head;
            }

            return null;
        }

        public List<ArrowData> GetMultipleEscapableArrows(int count)
        {
            List<ArrowData> escapable = new List<ArrowData>();
            List<ArrowData> blocked = new List<ArrowData>();

            foreach (List<ArrowData> group in _arrowGroups.Values)
            {
                ArrowData head = group.Find(a => IsHeadType(a.Type));
                if (head == null) continue;

                EscapeTraceResult trace = TraceEscapeRoute(head);
                _traceCache[head.ID] = trace;

                if (trace.CanEscape) escapable.Add(head);
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
            foreach (KeyValuePair<string, List<ArrowData>> kvp in _arrowGroups)
            {
                if (kvp.Key == excludeId) continue;
                ArrowData head = kvp.Value.Find(a => IsHeadType(a.Type));
                if (head != null && head.Type == targetType) results.Add(kvp.Key);
            }

            return results;
        }

        public IReadOnlyDictionary<string, List<ArrowData>> ArrowGroups => _arrowGroups;
        public IReadOnlyCollection<SpecialCellSaveData> SpecialCells => _specialCellsByPosition.Values;
    }
}
