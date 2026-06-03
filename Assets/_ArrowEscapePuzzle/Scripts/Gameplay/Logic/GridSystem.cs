using System;
using System.Collections.Generic;
using System.Linq;
using ArrowGame.Data.Events;
using ArrowGame.Utils;
using ArrowGame.Gameplay.Logic.SpecialCells;
using GameCore.Utils.DesignPattern.Events;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public class GridSystem
    {
        private readonly ArrowData[,] _grid;
        private readonly Dictionary<string, ArrowModel> _arrowModels;
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
            _arrowModels = new Dictionary<string, ArrowModel>();
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
            LoadArrows(levelData.Arrows);
            LoadSpecialCells(levelData.SpecialCells);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);
        }

        private void LoadArrows(List<ArrowSaveData> arrows)
        {
            if (arrows == null || arrows.Count == 0) return;

            for (int i = 0; i < arrows.Count; i++)
            {
                ArrowSaveData arrowSave = arrows[i];
                if (!ArrowModelFactory.TryCreate(arrowSave, out ArrowModel arrowModel, out string error))
                {
                    Debug.LogError($"[GridSystem] Invalid arrow data. {error}");
                    continue;
                }

                if (_arrowModels.ContainsKey(arrowModel.ArrowId))
                {
                    Debug.LogError($"[GridSystem] Duplicate arrow id '{arrowModel.ArrowId}'.");
                    continue;
                }

                if (!CanPlaceArrow(arrowModel, out string placementError))
                {
                    Debug.LogError($"[GridSystem] Cannot place arrow '{arrowModel.ArrowId}'. {placementError}");
                    continue;
                }

                PlaceArrow(arrowModel);
                RemainingArrows++;
            }
        }

        private bool CanPlaceArrow(ArrowModel arrowModel, out string error)
        {
            for (int i = 0; i < arrowModel.Path.Count; i++)
            {
                Vector2Int pos = arrowModel.Path[i];
                if (!IsValidPosition(pos.x, pos.y))
                {
                    error = $"Position ({pos.x}, {pos.y}) is outside the board.";
                    return false;
                }

                if (_grid[pos.x, pos.y].ID != EMPTY_ID)
                {
                    error = $"Position ({pos.x}, {pos.y}) is already occupied by arrow '{_grid[pos.x, pos.y].ID}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private void PlaceArrow(ArrowModel arrowModel)
        {
            _arrowModels[arrowModel.ArrowId] = arrowModel;

            List<CellType> legacyCellTypes = ArrowCellTypeBuilder.BuildLegacyCellTypes(arrowModel);
            List<ArrowData> group = new List<ArrowData>(arrowModel.Path.Count);

            for (int i = 0; i < arrowModel.Path.Count; i++)
            {
                Vector2Int pos = arrowModel.Path[i];
                ArrowData node = _grid[pos.x, pos.y];
                node.SetData(arrowModel.ArrowId, legacyCellTypes[i]);
                group.Add(node);
            }

            _arrowGroups[arrowModel.ArrowId] = group;
        }

        private void LoadSpecialCells(List<SpecialCellSaveData> specialCells)
        {
            _specialCellsByPosition.Clear();
            _portalGroups.Clear();

            if (specialCells == null) return;

            List<SpecialCellSaveData> uniqueSpecialCells = CounterBlockUtility.GetUniqueRoots(specialCells);
            foreach (SpecialCellSaveData specialCell in uniqueSpecialCells)
            {
                if (specialCell == null || !IsValidPosition(specialCell.Position.x, specialCell.Position.y)) continue;

                Vector2Int position = specialCell.Position;
                if (_grid[position.x, position.y].ID != EMPTY_ID)
                {
                    Debug.LogWarning(
                        $"[GridSystem] Skip special cell {specialCell.Type} at occupied cell ({position.x}, {position.y}).");
                    continue;
                }

                string cellId = specialCell.Id;
                if (specialCell.Type == BoardSpecialType.CounterBlock && string.IsNullOrEmpty(cellId))
                {
                    cellId = "Blocker_" + Guid.NewGuid().ToString().Substring(0, 4);
                }

                SpecialCellSaveData normalized = new SpecialCellSaveData(position, specialCell.Type,
                    specialCell.ExitDirection, specialCell.PortalId, specialCell.Counter,
                    CounterBlockUtility.CloneOffsets(specialCell.OccupiedOffsets), cellId);

                _specialCellsByPosition[position] = normalized;

                if (normalized.OccupiedOffsets != null)
                {
                    foreach (Vector2Int offset in normalized.OccupiedOffsets)
                    {
                        if (offset == Vector2Int.zero) continue;
                        Vector2Int targetPos = position + offset;
                        if (IsValidPosition(targetPos.x, targetPos.y))
                        {
                            if (_grid[targetPos.x, targetPos.y].ID != EMPTY_ID)
                            {
                                Debug.LogWarning(
                                    $"[GridSystem] Skip offset ({targetPos.x}, {targetPos.y}) for special cell {specialCell.Type} - cell occupied.");
                                continue;
                            }

                            _specialCellsByPosition[targetPos] = normalized;
                        }
                    }
                }

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

        public void TryMoveArrow(int startX, int startY)
        {
            ArrowData startArrow = GetArrow(startX, startY);
            if (startArrow == null || startArrow.ID == EMPTY_ID) return;

            Vector2Int tappedCell = new Vector2Int(startX, startY);
            ArrowActivationResult activationResult = TryMoveArrowGroup(startArrow.ID, tappedCell);
            if (activationResult == null) return;

            if (activationResult.AllSucceeded)
            {
                RemoveActivatedArrows(activationResult, LogicGameEventID.ArrowEscaped);
            }
            else
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowBlocked, activationResult);
            }
        }

        public ArrowActivationResult TryMoveArrowGroup(string arrowId, Vector2Int tappedCell)
        {
            ArrowModel triggerModel = GetArrowModel(arrowId);
            if (triggerModel == null) return null;

            List<ArrowModel> activationModels = GetActivationModels(triggerModel);
            if (activationModels.Count == 0) return null;
            List<ArrowActivationEntry> entries = new List<ArrowActivationEntry>(activationModels.Count);
            string activationGroupKey = BuildActivationGroupKey(triggerModel, tappedCell);
            bool allSucceeded = true;

            for (int i = 0; i < activationModels.Count; i++)
            {
                ArrowModel arrowModel = activationModels[i];
                ArrowEndpoint endpoint = arrowModel.ArrowId == arrowId
                    ? ResolveEndpointFromTap(arrowModel.ArrowId, tappedCell)
                    : arrowModel.PrimaryEndpoint;
                if (endpoint == null)
                {
                    allSucceeded = false;
                    continue;
                }

                EscapeTraceResult traceResult = TraceEscapeRoute(arrowModel.ArrowId, endpoint, true, activationGroupKey);
                if (!traceResult.CanEscape)
                {
                    allSucceeded = false;
                }

                entries.Add(new ArrowActivationEntry(arrowModel.ArrowId, endpoint, traceResult,
                    CreateArrowGroupSnapshot(arrowModel.ArrowId)));
            }

            return new ArrowActivationResult(arrowId, tappedCell, triggerModel.LinkGroupId, entries, allSucceeded);
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId, ArrowEndpoint endpoint)
        {
            return TraceEscapeRoute(arrowId, endpoint, true);
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId, ArrowEndpoint endpoint, bool cacheLiveResult,
            string activationGroupKey = "")
        {
            Direction4 initialDirection = endpoint != null ? endpoint.ExitDirection : Direction4.Up;
            EscapeTraceResult result = new EscapeTraceResult(arrowId, initialDirection,
                endpoint?.EndpointKey ?? string.Empty, endpoint?.PathIndex ?? -1);
            result.ActivationGroupKey = activationGroupKey ?? string.Empty;

            if (string.IsNullOrEmpty(arrowId) || endpoint == null)
            {
                if (cacheLiveResult) StoreTraceResult(result);
                return result;
            }

            Vector2Int direction = initialDirection.ToVector2Int();
            int checkX = endpoint.Position.x + direction.x;
            int checkY = endpoint.Position.y + direction.y;
            string startId = arrowId;
            HashSet<string> visitedStates = new HashSet<string>();

            while (true)
            {
                if (!IsValidPosition(checkX, checkY))
                {
                    result.CanEscape = true;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    if (cacheLiveResult) StoreTraceResult(result);
                    return result;
                }

                string stateKey = $"{checkX}:{checkY}:{direction.x}:{direction.y}";
                if (!visitedStates.Add(stateKey))
                {
                    result.BlockReason = EscapeBlockReason.Loop;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    if (cacheLiveResult) StoreTraceResult(result);
                    return result;
                }

                ArrowData cell = _grid[checkX, checkY];
                if (cell.ID != EMPTY_ID && cell.ID != startId)
                {
                    result.BlockReason = EscapeBlockReason.OtherArrow;
                    result.BlockerId = cell.ID;
                    result.FinalDirection = Direction4Extensions.FromVector(direction);
                    if (cacheLiveResult) StoreTraceResult(result);
                    return result;
                }

                Vector2Int currentPosition = new Vector2Int(checkX, checkY);
                result.AddWaypoint(currentPosition, 1f);

                if (_specialCellsByPosition.TryGetValue(currentPosition, out SpecialCellSaveData specialCell))
                {
                    ISpecialCellLogic logic = SpecialCellLogicFactory.GetLogic(specialCell.Type);
                    if (logic != null)
                    {
                        logic.OnSteppedOn(ref checkX, ref checkY, ref direction, result, this, specialCell);

                        if (checkX == -1 && checkY == -1)
                        {
                            if (cacheLiveResult) StoreTraceResult(result);
                            return result;
                        }

                        continue;
                    }
                }

                result.FinalDirection = Direction4Extensions.FromVector(direction);
                checkX += direction.x;
                checkY += direction.y;
            }
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId)
        {
            ArrowEndpoint endpoint = GetPrimaryEndpoint(arrowId);
            return TraceEscapeRoute(arrowId, endpoint);
        }

        public EscapeTraceResult TraceEscapeRoute(ArrowData headArrow)
        {
            if (headArrow == null || string.IsNullOrEmpty(headArrow.ID))
            {
                return new EscapeTraceResult(string.Empty, Direction4.Up);
            }

            return TraceEscapeRoute(headArrow.ID);
        }

        public SpecialCellSaveData ResolveExitPortal(SpecialCellSaveData entryPortal)
        {
            string portalId = entryPortal?.PortalId ?? string.Empty;
            if (string.IsNullOrEmpty(portalId)) return null;
            if (!_portalGroups.TryGetValue(portalId, out List<SpecialCellSaveData> group)) return null;
            if (group.Count != 2) return null;

            if (group[0].Position == entryPortal.Position) return group[1];
            if (group[1].Position == entryPortal.Position) return group[0];
            return null;
        }

        private void RemoveArrowInternal(string targetID, ArrowEndpoint endpoint, LogicGameEventID eventToPost)
        {
            if (string.IsNullOrEmpty(targetID) || !_arrowGroups.TryGetValue(targetID, out List<ArrowData> group)) return;

            EventManager<LogicGameEventID>.Post(eventToPost, group);

            if (eventToPost == LogicGameEventID.ArrowEscaped || eventToPost == LogicGameEventID.ArrowForceRemove)
            {
                Vector2Int dir = Vector2Int.zero;
                endpoint ??= GetPrimaryEndpoint(targetID);
                if (endpoint != null)
                {
                    dir = endpoint.ExitDirection.ToVector2Int();
                }

                DecrementCounterBlocks(dir);
            }

            foreach (ArrowData arrow in group) arrow.ResetData();

            _arrowModels.Remove(targetID);
            _arrowGroups.Remove(targetID);
            RemoveTraceCacheEntries(targetID);
            RemainingArrows = Mathf.Max(0, RemainingArrows - 1);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);

            if (IsBoardEmpty()) EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
        }

        private void RemoveActivatedArrows(ArrowActivationResult activationResult, LogicGameEventID eventToPost)
        {
            if (activationResult == null || activationResult.Entries == null || activationResult.Entries.Count == 0)
            {
                return;
            }

            EventManager<LogicGameEventID>.Post(eventToPost, activationResult);

            int removedCount = 0;
            for (int i = 0; i < activationResult.Entries.Count; i++)
            {
                ArrowActivationEntry entry = activationResult.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.ArrowId)) continue;
                if (!_arrowGroups.TryGetValue(entry.ArrowId, out List<ArrowData> group)) continue;

                Vector2Int dir = entry.Endpoint != null ? entry.Endpoint.ExitDirection.ToVector2Int() : Vector2Int.zero;
                if (eventToPost == LogicGameEventID.ArrowEscaped || eventToPost == LogicGameEventID.ArrowForceRemove)
                {
                    DecrementCounterBlocks(dir);
                }

                foreach (ArrowData arrow in group)
                {
                    arrow.ResetData();
                }

                _arrowModels.Remove(entry.ArrowId);
                _arrowGroups.Remove(entry.ArrowId);
                RemoveTraceCacheEntries(entry.ArrowId);
                removedCount++;
            }

            if (removedCount <= 0) return;

            RemainingArrows = Mathf.Max(0, RemainingArrows - removedCount);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);

            if (IsBoardEmpty())
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelComplete);
            }
        }

        private void DecrementCounterBlocks(Vector2Int impactDir)
        {
            List<SpecialCellSaveData> counterBlocks = GetUniqueSpecialCells(BoardSpecialType.CounterBlock);
            List<SpecialCellSaveData> toRemove = new List<SpecialCellSaveData>();

            for (int i = 0; i < counterBlocks.Count; i++)
            {
                SpecialCellSaveData counterBlock = counterBlocks[i];
                counterBlock.Counter--;
                EventManager<LogicGameEventID>.Post<(SpecialCellSaveData, Vector2Int)>(LogicGameEventID.SpecialCellChanged, (counterBlock, impactDir));

                if (counterBlock.Counter <= 0)
                {
                    toRemove.Add(counterBlock);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                RemoveSpecialCellFootprint(toRemove[i]);
                EventManager<LogicGameEventID>.Post(LogicGameEventID.SpecialCellDestroyed, toRemove[i].Position);
            }
        }

        private void RemoveSpecialCellFootprint(SpecialCellSaveData specialCell)
        {
            foreach (Vector2Int occupiedPos in CounterBlockUtility.GetOccupiedPositions(specialCell))
            {
                _specialCellsByPosition.Remove(occupiedPos);
            }
        }

        private List<SpecialCellSaveData> GetUniqueSpecialCells(BoardSpecialType? type = null)
        {
            List<SpecialCellSaveData> uniqueRoots = CounterBlockUtility.GetUniqueRoots(_specialCellsByPosition.Values);
            if (!type.HasValue) return uniqueRoots;

            uniqueRoots.RemoveAll(cell => cell.Type != type.Value);
            return uniqueRoots;
        }

        public bool IsValidPosition(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public ArrowData GetArrow(int x, int y) => IsValidPosition(x, y) ? _grid[x, y] : null;
        public bool IsBoardEmpty() => RemainingArrows <= 0;

        public ArrowData GetHeadOfGroup(string targetID)
        {
            ArrowEndpoint primaryEndpoint = GetPrimaryEndpoint(targetID);
            if (primaryEndpoint == null) return null;

            return GetArrow(primaryEndpoint.Position.x, primaryEndpoint.Position.y);
        }

        public ArrowModel GetArrowModel(string arrowId)
        {
            if (string.IsNullOrEmpty(arrowId)) return null;
            _arrowModels.TryGetValue(arrowId, out ArrowModel model);
            return model;
        }

        public IReadOnlyCollection<ArrowModel> GetArrowModels()
        {
            return _arrowModels.Values.ToList().AsReadOnly();
        }

        public ArrowEndpoint GetPrimaryEndpoint(string arrowId)
        {
            return GetArrowModel(arrowId)?.PrimaryEndpoint;
        }

        public ArrowEndpoint ResolveEndpointFromTap(string arrowId, Vector2Int tappedCell)
        {
            IReadOnlyList<ArrowEndpoint> endpoints = GetAvailableEndpoints(arrowId);
            if (endpoints == null || endpoints.Count == 0) return null;
            if (endpoints.Count == 1) return endpoints[0];

            ArrowEndpoint bestEndpoint = null;
            float bestDistance = float.MaxValue;
            bool bestIsPrimary = false;

            for (int i = 0; i < endpoints.Count; i++)
            {
                ArrowEndpoint endpoint = endpoints[i];
                float distance = Vector2Int.Distance(endpoint.Position, tappedCell);
                bool isPrimary = endpoint.IsPrimary;
                if (distance < bestDistance - 0.001f ||
                    (Mathf.Abs(distance - bestDistance) <= 0.001f && isPrimary && !bestIsPrimary))
                {
                    bestEndpoint = endpoint;
                    bestDistance = distance;
                    bestIsPrimary = isPrimary;
                }
            }

            return bestEndpoint ?? endpoints[0];
        }

        public Direction4? GetPrimaryExitDirection(string arrowId)
        {
            ArrowEndpoint endpoint = GetPrimaryEndpoint(arrowId);
            return endpoint?.ExitDirection;
        }

        public IReadOnlyList<ArrowEndpoint> GetAvailableEndpoints(string arrowId)
        {
            ArrowModel model = GetArrowModel(arrowId);
            return model != null ? model.Endpoints : Array.Empty<ArrowEndpoint>();
        }

        public List<string> GetArrowIdsByPrimaryDirection(Direction4 direction, string excludeId = "")
        {
            List<string> results = new List<string>();
            foreach (KeyValuePair<string, ArrowModel> kvp in _arrowModels)
            {
                if (kvp.Key == excludeId) continue;

                ArrowEndpoint endpoint = kvp.Value.PrimaryEndpoint;
                if (endpoint != null && endpoint.ExitDirection == direction)
                {
                    results.Add(kvp.Key);
                }
            }

            return results;
        }

        public int GetEmptyCellsBeforeBlock(ArrowData headArrow)
        {
            if (headArrow == null) return 0;

            ArrowEndpoint primaryEndpoint = GetPrimaryEndpoint(headArrow.ID);
            if (primaryEndpoint == null) return 0;

            EscapeTraceResult trace = TraceEscapeRoute(headArrow.ID, primaryEndpoint);
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
            ArrowEndpoint endpoint = GetPrimaryEndpoint(arrowId);
            if (string.IsNullOrEmpty(arrowId) || endpoint == null) return null;

            return GetCachedTraceResult(arrowId, endpoint.EndpointKey);
        }

        public EscapeTraceResult GetCachedTraceResult(string arrowId, string endpointKey)
        {
            if (string.IsNullOrEmpty(arrowId) || string.IsNullOrEmpty(endpointKey)) return null;

            _traceCache.TryGetValue(BuildTraceCacheKey(arrowId, endpointKey), out EscapeTraceResult trace);
            return trace;
        }

        public EscapeTraceResult GetLiveTraceResult(string arrowId)
        {
            ArrowEndpoint endpoint = GetPrimaryEndpoint(arrowId);
            if (endpoint == null) return null;

            return GetLiveTraceResult(arrowId, endpoint);
        }

        public EscapeTraceResult GetLiveTraceResult(string arrowId, ArrowEndpoint endpoint)
        {
            if (endpoint == null) return null;
            return TraceEscapeRoute(arrowId, endpoint);
        }

        public void ForceRemoveArrow(string targetID)
        {
            RemoveArrowInternal(targetID, GetPrimaryEndpoint(targetID), LogicGameEventID.ArrowForceRemove);
        }
        
        public List<List<string>> GetLinkedGroups()
        {
            Dictionary<string, List<string>> groupedDict = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, ArrowModel> kvp in _arrowModels)
            {
                ArrowModel model = kvp.Value;
                // Bỏ qua những mũi tên không có Link Group
                if (model == null || string.IsNullOrEmpty(model.LinkGroupId)) continue;

                if (!groupedDict.ContainsKey(model.LinkGroupId))
                {
                    groupedDict[model.LinkGroupId] = new List<string>();
                }
                groupedDict[model.LinkGroupId].Add(model.ArrowId);
            }

            // Chỉ lọc ra những nhóm có từ 2 mũi tên trở lên để vẽ dây nối
            List<List<string>> validGroups = new List<List<string>>();
            foreach (List<string> group in groupedDict.Values)
            {
                if (group.Count >= 2)
                {
                    // Check if they are on the same vertical line
                    bool sameXForGroup = true;
                    int? firstX = null;
                    foreach (string id in group)
                    {
                        ArrowEndpoint ep = GetPrimaryEndpoint(id);
                        if (ep != null)
                        {
                            if (firstX == null) firstX = ep.Position.x;
                            else if (ep.Position.x != firstX.Value)
                            {
                                sameXForGroup = false;
                                break;
                            }
                        }
                    }

                    // Sort the group so that if they are in a straight line, they are linked consecutively
                    group.Sort((a, b) =>
                    {
                        ArrowEndpoint epA = GetPrimaryEndpoint(a);
                        ArrowEndpoint epB = GetPrimaryEndpoint(b);
                        if (epA == null && epB == null) return 0;
                        if (epA == null) return -1;
                        if (epB == null) return 1;

                        if (sameXForGroup)
                        {
                            // If same X, sort by Y
                            return epA.Position.y.CompareTo(epB.Position.y);
                        }

                        // Otherwise, sort by X then Y
                        int xComp = epA.Position.x.CompareTo(epB.Position.x);
                        if (xComp != 0) return xComp;
                        return epA.Position.y.CompareTo(epB.Position.y);
                    });

                    validGroups.Add(group);
                }
            }

            return validGroups;
        }

        public ArrowData GetOneEscapableArrow()
        {
            foreach (ArrowModel arrowModel in _arrowModels.Values)
            {
                ArrowEndpoint endpoint = arrowModel.PrimaryEndpoint;
                if (endpoint == null) continue;

                EscapeTraceResult trace = TraceEscapeRoute(arrowModel.ArrowId, endpoint);
                if (trace.CanEscape) return GetHeadOfGroup(arrowModel.ArrowId);
            }

            return null;
        }

        public List<ArrowData> GetMultipleEscapableArrows(int count)
        {
            List<ArrowData> escapable = new List<ArrowData>();
            List<ArrowData> blocked = new List<ArrowData>();

            foreach (ArrowModel arrowModel in _arrowModels.Values)
            {
                ArrowEndpoint endpoint = arrowModel.PrimaryEndpoint;
                if (endpoint == null) continue;

                EscapeTraceResult trace = TraceEscapeRoute(arrowModel.ArrowId, endpoint);
                ArrowData head = GetHeadOfGroup(arrowModel.ArrowId);
                if (head == null) continue;

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
            if (!ArrowCellTypeBuilder.TryGetHeadDirection(targetType, out Direction4 direction))
            {
                return new List<string>();
            }

            return GetArrowIdsByPrimaryDirection(direction, excludeId);
        }

        private void StoreTraceResult(EscapeTraceResult traceResult)
        {
            if (traceResult == null || string.IsNullOrEmpty(traceResult.ArrowId)) return;

            _traceCache[BuildTraceCacheKey(traceResult.ArrowId, traceResult.StartEndpointKey)] = traceResult;
        }

        private void RemoveTraceCacheEntries(string arrowId)
        {
            if (string.IsNullOrEmpty(arrowId) || _traceCache.Count == 0) return;

            List<string> keysToRemove = new List<string>();
            foreach (string key in _traceCache.Keys)
            {
                if (key.StartsWith(arrowId + "|"))
                {
                    keysToRemove.Add(key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _traceCache.Remove(keysToRemove[i]);
            }
        }

        private static string BuildTraceCacheKey(string arrowId, string endpointKey)
        {
            return $"{arrowId}|{endpointKey}";
        }

        private List<ArrowModel> GetActivationModels(ArrowModel triggerModel)
        {
            List<ArrowModel> activationModels = new List<ArrowModel>();
            if (triggerModel == null) return activationModels;

            if (string.IsNullOrEmpty(triggerModel.LinkGroupId))
            {
                activationModels.Add(triggerModel);
                return activationModels;
            }

            foreach (ArrowModel arrowModel in _arrowModels.Values)
            {
                if (arrowModel == null || arrowModel.LinkGroupId != triggerModel.LinkGroupId) continue;
                activationModels.Add(arrowModel);
            }

            activationModels.Sort((a, b) =>
            {
                if (a.ArrowId == triggerModel.ArrowId) return -1;
                if (b.ArrowId == triggerModel.ArrowId) return 1;
                return string.CompareOrdinal(a.ArrowId, b.ArrowId);
            });

            return activationModels;
        }

        private List<ArrowData> CreateArrowGroupSnapshot(string arrowId)
        {
            List<ArrowData> snapshot = new List<ArrowData>();
            if (!_arrowGroups.TryGetValue(arrowId, out List<ArrowData> group) || group == null)
            {
                return snapshot;
            }

            for (int i = 0; i < group.Count; i++)
            {
                ArrowData cell = group[i];
                snapshot.Add(new ArrowData(cell.ID, cell.X, cell.Y, cell.Type));
            }

            return snapshot;
        }

        private static string BuildActivationGroupKey(ArrowModel triggerModel, Vector2Int tappedCell)
        {
            string groupId = string.IsNullOrEmpty(triggerModel?.LinkGroupId)
                ? triggerModel?.ArrowId ?? string.Empty
                : triggerModel.LinkGroupId;
            return $"{groupId}@{tappedCell.x}:{tappedCell.y}";
        }

        public IReadOnlyDictionary<string, List<ArrowData>> ArrowGroups => _arrowGroups;
        public IReadOnlyDictionary<string, ArrowModel> ArrowModels => _arrowModels;
        public IReadOnlyCollection<SpecialCellSaveData> SpecialCells => GetUniqueSpecialCells();
    }
}
