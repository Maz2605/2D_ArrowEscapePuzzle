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
        private readonly BoardState _state;
        private readonly ArrowActivationPlanner _activationPlanner;
        private readonly ArrowEscapeTracer _tracer;
        private readonly BoardOutcomeProcessor _outcomeProcessor;

        public int Width => _state?.Width ?? 0;
        public int Height => _state?.Height ?? 0;
        public int RemainingArrows => _state?.RemainingArrows ?? 0;

        public GridSystem(LevelSaveData levelData)
        {
            if (levelData == null) return;

            _state = new BoardState(levelData.Width, levelData.Height);
            _tracer = new ArrowEscapeTracer(_state);
            _activationPlanner = new ArrowActivationPlanner(_state, _tracer);
            _outcomeProcessor = new BoardOutcomeProcessor(_state);

            BoardInitializer initializer = new BoardInitializer(_state);
            initializer.Initialize(levelData);
            EventManager<LogicGameEventID>.Post<int>(LogicGameEventID.ArrowCountChanged, RemainingArrows);
        }

        public void TryMoveArrow(int startX, int startY)
        {
            ArrowData startArrow = GetArrow(startX, startY);
            if (startArrow == null || startArrow.ID == BoardState.EmptyId) return;

            Vector2Int tappedCell = new Vector2Int(startX, startY);
            ArrowActivationResult activationResult = TryMoveArrowGroup(startArrow.ID, tappedCell);
            if (activationResult == null) return;

            if (activationResult.AllSucceeded)
            {
                _outcomeProcessor.RemoveActivatedArrows(activationResult, LogicGameEventID.ArrowEscaped);
            }
            else
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.ArrowBlocked, activationResult);
            }
        }

        public ArrowActivationResult TryMoveArrowGroup(string arrowId, Vector2Int tappedCell)
        {
            return _activationPlanner?.TryMoveArrowGroup(arrowId, tappedCell);
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId, ArrowEndpoint endpoint)
        {
            return _tracer?.TraceEscapeRoute(arrowId, endpoint);
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId, ArrowEndpoint endpoint, bool cacheLiveResult,
            string activationGroupKey = "")
        {
            return _tracer?.TraceEscapeRoute(arrowId, endpoint, cacheLiveResult, activationGroupKey);
        }

        public EscapeTraceResult TraceEscapeRoute(string arrowId)
        {
            return _tracer?.TraceEscapeRoute(arrowId);
        }

        public EscapeTraceResult TraceEscapeRoute(ArrowData headArrow)
        {
            if (_tracer == null)
            {
                return new EscapeTraceResult(string.Empty, Direction4.Up);
            }

            return _tracer.TraceEscapeRoute(headArrow);
        }

        public SpecialCellSaveData ResolveExitPortal(SpecialCellSaveData entryPortal)
        {
            return _state?.ResolveExitPortal(entryPortal);
        }

        public bool IsValidPosition(int x, int y) => _state != null && _state.IsValidPosition(x, y);
        public ArrowData GetArrow(int x, int y) => _state?.GetArrow(x, y);
        public bool IsBoardEmpty() => _state == null || _state.IsBoardEmpty();
        public ArrowData GetHeadOfGroup(string targetID) => _state?.GetHeadOfGroup(targetID);
        public ArrowModel GetArrowModel(string arrowId) => _state?.GetArrowModel(arrowId);
        public IReadOnlyCollection<ArrowModel> GetArrowModels() => _state?.GetArrowModels() ?? System.Array.Empty<ArrowModel>();
        public ArrowEndpoint GetPrimaryEndpoint(string arrowId) => _state?.GetPrimaryEndpoint(arrowId);
        public ArrowEndpoint ResolveEndpointFromTap(string arrowId, Vector2Int tappedCell) => _activationPlanner?.ResolveEndpointFromTap(arrowId, tappedCell);

        public Direction4? GetPrimaryExitDirection(string arrowId)
        {
            ArrowEndpoint endpoint = GetPrimaryEndpoint(arrowId);
            return endpoint?.ExitDirection;
        }

        public IReadOnlyList<ArrowEndpoint> GetAvailableEndpoints(string arrowId)
        {
            return _state?.GetAvailableEndpoints(arrowId) ?? System.Array.Empty<ArrowEndpoint>();
        }

        public List<string> GetArrowIdsByPrimaryDirection(Direction4 direction, string excludeId = "")
        {
            return _state?.GetArrowIdsByPrimaryDirection(direction, excludeId) ?? new List<string>();
        }

        public int GetEmptyCellsBeforeBlock(ArrowData headArrow)
        {
            if (headArrow == null) return 0;

            ArrowEndpoint primaryEndpoint = GetPrimaryEndpoint(headArrow.ID);
            if (primaryEndpoint == null) return 0;

            EscapeTraceResult trace = TraceEscapeRoute(headArrow.ID, primaryEndpoint);
            return trace != null ? trace.DistanceBeforeStop : 0;
        }

        public string GetArrowIdAt(int x, int y) => _state?.GetArrowIdAt(x, y);
        public SpecialCellSaveData GetSpecialCellAt(int x, int y) => _state?.GetSpecialCellAt(x, y);

        public EscapeTraceResult GetCachedTraceResult(string arrowId)
        {
            ArrowEndpoint endpoint = GetPrimaryEndpoint(arrowId);
            if (string.IsNullOrEmpty(arrowId) || endpoint == null) return null;

            return GetCachedTraceResult(arrowId, endpoint.EndpointKey);
        }

        public EscapeTraceResult GetCachedTraceResult(string arrowId, string endpointKey)
        {
            return _state?.GetCachedTraceResult(arrowId, endpointKey);
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
            _outcomeProcessor?.RemoveArrowInternal(targetID, GetPrimaryEndpoint(targetID), LogicGameEventID.ArrowForceRemove);
        }

        public List<List<string>> GetLinkedGroups()
        {
            Dictionary<string, List<string>> groupedDict = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, ArrowModel> kvp in ArrowModels)
            {
                ArrowModel model = kvp.Value;
                if (model == null || string.IsNullOrEmpty(model.LinkGroupId)) continue;

                if (!groupedDict.ContainsKey(model.LinkGroupId))
                {
                    groupedDict[model.LinkGroupId] = new List<string>();
                }
                groupedDict[model.LinkGroupId].Add(model.ArrowId);
            }

            List<List<string>> validGroups = new List<List<string>>();
            foreach (List<string> group in groupedDict.Values)
            {
                if (group.Count < 2) continue;

                bool sameXForGroup = true;
                int? firstX = null;
                foreach (string id in group)
                {
                    ArrowEndpoint ep = GetPrimaryEndpoint(id);
                    if (ep == null) continue;

                    if (firstX == null) firstX = ep.Position.x;
                    else if (ep.Position.x != firstX.Value)
                    {
                        sameXForGroup = false;
                        break;
                    }
                }

                group.Sort((a, b) =>
                {
                    ArrowEndpoint epA = GetPrimaryEndpoint(a);
                    ArrowEndpoint epB = GetPrimaryEndpoint(b);
                    if (epA == null && epB == null) return 0;
                    if (epA == null) return -1;
                    if (epB == null) return 1;

                    if (sameXForGroup)
                    {
                        return epA.Position.y.CompareTo(epB.Position.y);
                    }

                    int xComp = epA.Position.x.CompareTo(epB.Position.x);
                    if (xComp != 0) return xComp;
                    return epA.Position.y.CompareTo(epB.Position.y);
                });

                validGroups.Add(group);
            }

            return validGroups;
        }

        public ArrowData GetOneEscapableArrow()
        {
            foreach (ArrowModel arrowModel in ArrowModels.Values)
            {
                ArrowEndpoint endpoint = arrowModel.PrimaryEndpoint;
                if (endpoint == null) continue;

                EscapeTraceResult trace = TraceEscapeRoute(arrowModel.ArrowId, endpoint);
                if (trace != null && trace.CanEscape) return GetHeadOfGroup(arrowModel.ArrowId);
            }

            return null;
        }

        public List<ArrowData> GetMultipleEscapableArrows(int count)
        {
            List<ArrowData> escapable = new List<ArrowData>();
            List<ArrowData> blocked = new List<ArrowData>();

            foreach (ArrowModel arrowModel in ArrowModels.Values)
            {
                ArrowEndpoint endpoint = arrowModel.PrimaryEndpoint;
                if (endpoint == null) continue;

                EscapeTraceResult trace = TraceEscapeRoute(arrowModel.ArrowId, endpoint);
                ArrowData head = GetHeadOfGroup(arrowModel.ArrowId);
                if (head == null) continue;

                if (trace != null && trace.CanEscape) escapable.Add(head);
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

        public IReadOnlyDictionary<string, List<ArrowData>> ArrowGroups => _state?.ArrowGroups;
        public IReadOnlyDictionary<string, ArrowModel> ArrowModels => _state?.ArrowModels;
        public IReadOnlyCollection<SpecialCellSaveData> SpecialCells => _state?.SpecialCells;
    }
}
