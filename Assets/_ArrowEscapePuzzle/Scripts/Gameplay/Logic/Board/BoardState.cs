using System.Collections.Generic;
using System.Linq;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class BoardState : ITraceBoardView
    {
        public const string EmptyId = "";

        private readonly ArrowData[,] _grid;
        private readonly Dictionary<string, ArrowModel> _arrowModels = new Dictionary<string, ArrowModel>();
        private readonly Dictionary<string, List<ArrowData>> _arrowGroups = new Dictionary<string, List<ArrowData>>();
        private readonly Dictionary<Vector2Int, SpecialCellSaveData> _specialCellsByPosition =
            new Dictionary<Vector2Int, SpecialCellSaveData>();
        private readonly Dictionary<string, List<SpecialCellSaveData>> _portalGroups =
            new Dictionary<string, List<SpecialCellSaveData>>();
        private readonly Dictionary<string, EscapeTraceResult> _traceCache = new Dictionary<string, EscapeTraceResult>();

        public int Width { get; }
        public int Height { get; }
        public int RemainingArrows { get; set; }
        public IReadOnlyDictionary<string, List<ArrowData>> ArrowGroups => _arrowGroups;
        public IReadOnlyDictionary<string, ArrowModel> ArrowModels => _arrowModels;
        public IReadOnlyCollection<SpecialCellSaveData> SpecialCells => GetUniqueSpecialCells();

        internal Dictionary<string, ArrowModel> MutableArrowModels => _arrowModels;
        internal Dictionary<string, List<ArrowData>> MutableArrowGroups => _arrowGroups;

        public BoardState(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new ArrowData[Width, Height];
        }

        public void SetEmptyCell(int x, int y)
        {
            _grid[x, y] = new ArrowData(EmptyId, x, y, CellType.EmptyDot);
        }

        public bool IsValidPosition(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        public ArrowData GetArrow(int x, int y) => IsValidPosition(x, y) ? _grid[x, y] : null;
        public bool IsBoardEmpty() => RemainingArrows <= 0;

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

        public IReadOnlyList<ArrowEndpoint> GetAvailableEndpoints(string arrowId)
        {
            ArrowModel model = GetArrowModel(arrowId);
            return model != null ? model.Endpoints : System.Array.Empty<ArrowEndpoint>();
        }

        public ArrowData GetHeadOfGroup(string targetId)
        {
            ArrowEndpoint primaryEndpoint = GetPrimaryEndpoint(targetId);
            if (primaryEndpoint == null) return null;

            return GetArrow(primaryEndpoint.Position.x, primaryEndpoint.Position.y);
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

        public string GetArrowIdAt(int x, int y) => GetArrow(x, y)?.ID;

        public SpecialCellSaveData GetSpecialCellAt(int x, int y)
        {
            _specialCellsByPosition.TryGetValue(new Vector2Int(x, y), out SpecialCellSaveData specialCell);
            return specialCell;
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

        public void AddArrow(ArrowModel model, List<ArrowData> group)
        {
            _arrowModels[model.ArrowId] = model;
            _arrowGroups[model.ArrowId] = group;
            RemainingArrows++;
        }

        public void RemoveArrow(string arrowId)
        {
            _arrowModels.Remove(arrowId);
            _arrowGroups.Remove(arrowId);
            RemoveTraceCacheEntries(arrowId);
        }

        public void ClearSpecialCells()
        {
            _specialCellsByPosition.Clear();
            _portalGroups.Clear();
        }

        public void SetSpecialCellPosition(Vector2Int position, SpecialCellSaveData specialCell)
        {
            _specialCellsByPosition[position] = specialCell;
        }

        public void RemoveSpecialCellPosition(Vector2Int position)
        {
            _specialCellsByPosition.Remove(position);
        }

        public void RegisterPortal(SpecialCellSaveData portal)
        {
            string portalId = portal?.PortalId ?? string.Empty;
            if (!_portalGroups.TryGetValue(portalId, out List<SpecialCellSaveData> group))
            {
                group = new List<SpecialCellSaveData>();
                _portalGroups.Add(portalId, group);
            }

            group.Add(portal);
        }

        public List<SpecialCellSaveData> GetUniqueSpecialCells(BoardSpecialType? type = null)
        {
            List<SpecialCellSaveData> uniqueRoots = CounterBlockUtility.GetUniqueRoots(_specialCellsByPosition.Values);
            if (!type.HasValue) return uniqueRoots;

            uniqueRoots.RemoveAll(cell => cell.Type != type.Value);
            return uniqueRoots;
        }

        public EscapeTraceResult GetCachedTraceResult(string arrowId, string endpointKey)
        {
            if (string.IsNullOrEmpty(arrowId) || string.IsNullOrEmpty(endpointKey)) return null;

            _traceCache.TryGetValue(BuildTraceCacheKey(arrowId, endpointKey), out EscapeTraceResult trace);
            return trace;
        }

        public void StoreTraceResult(EscapeTraceResult traceResult)
        {
            if (traceResult == null || string.IsNullOrEmpty(traceResult.ArrowId)) return;

            _traceCache[BuildTraceCacheKey(traceResult.ArrowId, traceResult.StartEndpointKey)] = traceResult;
        }

        public void RemoveTraceCacheEntries(string arrowId)
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
    }
}
