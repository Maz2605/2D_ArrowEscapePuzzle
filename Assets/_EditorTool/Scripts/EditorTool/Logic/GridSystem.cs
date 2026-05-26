using System;
using System.Collections.Generic;
using EditorTool.Scripts.Data;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Logic
{
    public class GridSystem
    {
        private CellData[,] _grid;
        private readonly Dictionary<string, List<Vector2Int>> _arrowPaths = new Dictionary<string, List<Vector2Int>>();
        private readonly Dictionary<string, EditorArrowMetadataData> _arrowMetadata =
            new Dictionary<string, EditorArrowMetadataData>();
        private readonly Dictionary<Vector2Int, SpecialCellSaveData> _specialCells =
            new Dictionary<Vector2Int, SpecialCellSaveData>();

        public int Width { get; private set; }
        public int Height { get; private set; }

        public event Action<int, int, CellData> OnCellChanged;
        public event Action<string> OnArrowMetadataChanged;
        public event Action OnGridRebuilt;

        public bool SuppressEvents { get; private set; }

        public void BeginBulkLoad()
        {
            SuppressEvents = true;
        }

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
            _arrowMetadata.Clear();
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
                _arrowMetadata[arrowID] = CreateDefaultMetadata(arrowID, headFirst);
            }

            List<Vector2Int> path = _arrowPaths[arrowID];

            if (path.Count == 0)
            {
                if (_grid[x, y].type != CellType.EmptyDot) return;
                path.Add(newPos);
                NormalizeArrowMetadata(arrowID);
                UpdatePathVisuals(arrowID);
                return;
            }

            if (path[path.Count - 1] == newPos) return;

            if (path.Count >= 2 && path[path.Count - 2] == newPos)
            {
                Vector2Int removedPos = path[path.Count - 1];
                path.RemoveAt(path.Count - 1);
                SetCellVisual(removedPos.x, removedPos.y, CellType.EmptyDot, string.Empty);
                NormalizeArrowMetadata(arrowID);
                UpdatePathVisuals(arrowID);
                return;
            }

            Vector2Int lastPos = path[path.Count - 1];
            if (Mathf.Abs(newPos.x - lastPos.x) + Mathf.Abs(newPos.y - lastPos.y) == 1)
            {
                if (_grid[x, y].arrowID != string.Empty && _grid[x, y].arrowID != arrowID) return;
                path.Add(newPos);
                NormalizeArrowMetadata(arrowID);
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
                _arrowMetadata.Remove(id);
                ForceCleanupID(id);
                RaiseArrowMetadataChanged(id);
            }
            else
            {
                NormalizeArrowMetadata(id);
                UpdatePathVisuals(id);
            }
        }

        public bool SetSpecialCell(int x, int y, BoardSpecialType type, Direction4 exitDirection, string portalId = "",
            int counter = 0)
        {
            if (!IsValidPosition(x, y)) return false;
            if (_grid[x, y].arrowID != string.Empty) return false;

            Vector2Int position = new Vector2Int(x, y);
            if (_specialCells.ContainsKey(position)) return false;

            string normalizedPortalId = (portalId ?? string.Empty).Trim();
            string cellId = string.Empty;
            if (type == BoardSpecialType.CounterBlock)
            {
                normalizedPortalId = counter.ToString();
                cellId = "Blocker_" + Guid.NewGuid().ToString().Substring(0, 4);
            }

            _specialCells[position] =
                new SpecialCellSaveData(position, type, exitDirection, normalizedPortalId, counter, null, cellId);
            OnCellChanged?.Invoke(x, y, _grid[x, y]);
            return true;
        }

        public void MapOffsetToSpecialCell(Vector2Int position, SpecialCellSaveData specialCell)
        {
            if (!IsValidPosition(position.x, position.y)) return;
            _specialCells[position] = specialCell;
            OnCellChanged?.Invoke(position.x, position.y, _grid[position.x, position.y]);
        }

        public bool TryExpandCounterBlock(Vector2Int position)
        {
            if (!IsValidPosition(position.x, position.y)) return false;
            if (_grid[position.x, position.y].arrowID != string.Empty) return false;
            if (_specialCells.ContainsKey(position)) return false;

            Vector2Int[] neighborDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < neighborDirs.Length; i++)
            {
                Vector2Int candidatePos = position + neighborDirs[i];
                SpecialCellSaveData specialCell = GetSpecialCellAt(candidatePos.x, candidatePos.y);
                if (specialCell == null || specialCell.Type != BoardSpecialType.CounterBlock) continue;

                Vector2Int offset = position - specialCell.Position;
                if (offset == Vector2Int.zero) return false;

                if (specialCell.OccupiedOffsets == null)
                    specialCell.OccupiedOffsets = new List<Vector2Int>();

                if (specialCell.OccupiedOffsets.Contains(offset)) return false;

                specialCell.OccupiedOffsets.Add(offset);
                _specialCells[position] = specialCell;
                OnCellChanged?.Invoke(specialCell.Position.x, specialCell.Position.y,
                    _grid[specialCell.Position.x, specialCell.Position.y]);
                OnCellChanged?.Invoke(position.x, position.y, _grid[position.x, position.y]);
                return true;
            }

            return false;
        }

        public void RemoveSpecialCellAt(int x, int y)
        {
            if (!IsValidPosition(x, y)) return;
            Vector2Int position = new Vector2Int(x, y);
            if (!_specialCells.TryGetValue(position, out SpecialCellSaveData specialCell)) return;

            if (specialCell.Type != BoardSpecialType.CounterBlock)
            {
                if (_specialCells.Remove(position))
                {
                    OnCellChanged?.Invoke(x, y, _grid[x, y]);
                }

                return;
            }

            foreach (Vector2Int occupiedPos in CounterBlockUtility.GetOccupiedPositions(specialCell))
            {
                if (!IsValidPosition(occupiedPos.x, occupiedPos.y)) continue;
                if (_specialCells.Remove(occupiedPos))
                {
                    OnCellChanged?.Invoke(occupiedPos.x, occupiedPos.y, _grid[occupiedPos.x, occupiedPos.y]);
                }
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
            {
                UpdatePathVisuals(id);
            }
        }

        public List<ArrowSaveData> GetSaveData()
        {
            List<ArrowSaveData> result = new List<ArrowSaveData>();
            foreach (KeyValuePair<string, List<Vector2Int>> kvp in _arrowPaths)
            {
                ArrowSaveData arrowSave = BuildArrowSaveData(kvp.Key);
                if (arrowSave != null)
                {
                    result.Add(arrowSave);
                }
            }

            return result;
        }

        public List<SpecialCellSaveData> GetSpecialSaveData()
        {
            List<SpecialCellSaveData> uniqueRoots = CounterBlockUtility.GetUniqueRoots(_specialCells.Values);
            List<SpecialCellSaveData> saveData = new List<SpecialCellSaveData>(uniqueRoots.Count);

            for (int i = 0; i < uniqueRoots.Count; i++)
            {
                saveData.Add(CounterBlockUtility.Clone(uniqueRoots[i]));
            }

            return saveData;
        }

        public void LoadFromSaveData(List<ArrowSaveData> arrows, List<SpecialCellSaveData> specialCells)
        {
            _arrowPaths.Clear();
            _arrowMetadata.Clear();
            _specialCells.Clear();
            ClearGridCells();

            if (arrows != null)
            {
                for (int i = 0; i < arrows.Count; i++)
                {
                    ArrowSaveData arrow = arrows[i];
                    if (arrow == null || string.IsNullOrWhiteSpace(arrow.ArrowID)) continue;

                    _arrowPaths[arrow.ArrowID] = arrow.Path != null
                        ? new List<Vector2Int>(arrow.Path)
                        : new List<Vector2Int>();
                    _arrowMetadata[arrow.ArrowID] = CreateMetadataFromSaveData(arrow);
                    NormalizeArrowMetadata(arrow.ArrowID);
                    UpdatePathVisuals(arrow.ArrowID);
                }
            }

            if (specialCells != null)
            {
                List<SpecialCellSaveData> uniqueSpecialCells = CounterBlockUtility.GetUniqueRoots(specialCells);
                foreach (SpecialCellSaveData specialCell in uniqueSpecialCells)
                {
                    if (!IsValidPosition(specialCell.Position.x, specialCell.Position.y)) continue;

                    string cellId = specialCell.Id;
                    if (string.IsNullOrEmpty(cellId) && specialCell.Type == BoardSpecialType.CounterBlock)
                    {
                        cellId = "Blocker_" + Guid.NewGuid().ToString().Substring(0, 4);
                    }

                    SpecialCellSaveData normalized = new SpecialCellSaveData(specialCell.Position, specialCell.Type,
                        specialCell.ExitDirection, specialCell.PortalId, specialCell.Counter,
                        CounterBlockUtility.CloneOffsets(specialCell.OccupiedOffsets), cellId);

                    _specialCells[specialCell.Position] = normalized;
                    OnCellChanged?.Invoke(specialCell.Position.x, specialCell.Position.y,
                        _grid[specialCell.Position.x, specialCell.Position.y]);

                    if (normalized.OccupiedOffsets == null) continue;

                    foreach (Vector2Int offset in normalized.OccupiedOffsets)
                    {
                        if (offset == Vector2Int.zero) continue;
                        Vector2Int targetPos = specialCell.Position + offset;
                        if (!IsValidPosition(targetPos.x, targetPos.y)) continue;

                        _specialCells[targetPos] = normalized;
                        OnCellChanged?.Invoke(targetPos.x, targetPos.y, _grid[targetPos.x, targetPos.y]);
                    }
                }
            }
        }

        public List<string> GetAllArrowIDs() => new List<string>(_arrowPaths.Keys);

        public List<Vector2Int> GetArrowPath(string arrowID)
        {
            return _arrowPaths.TryGetValue(arrowID, out List<Vector2Int> path) ? path : null;
        }

        public EditorArrowMetadataData GetArrowMetadata(string arrowId)
        {
            return _arrowMetadata.TryGetValue(arrowId, out EditorArrowMetadataData metadata) ? metadata.Clone() : null;
        }

        public bool IsHeadFirst(string arrowID)
        {
            return _arrowMetadata.TryGetValue(arrowID, out EditorArrowMetadataData metadata) &&
                   metadata.PrimaryEndpointPathIndex == 0;
        }

        public Vector2Int? GetPrimaryEndpointPosition(string arrowId)
        {
            if (!TryGetPathAndMetadata(arrowId, out List<Vector2Int> path, out EditorArrowMetadataData metadata))
                return null;

            if (path.Count == 0 || metadata.PrimaryEndpointPathIndex < 0 || metadata.PrimaryEndpointPathIndex >= path.Count)
                return null;

            return path[metadata.PrimaryEndpointPathIndex];
        }

        public Vector2Int? GetSecondaryEndpointPosition(string arrowId)
        {
            if (!TryGetPathAndMetadata(arrowId, out List<Vector2Int> path, out EditorArrowMetadataData metadata))
                return null;

            if (!metadata.HasSecondaryEndpoint ||
                metadata.SecondaryEndpointPathIndex < 0 ||
                metadata.SecondaryEndpointPathIndex >= path.Count)
                return null;

            return path[metadata.SecondaryEndpointPathIndex];
        }

        public bool ToggleTwoHead(string arrowId)
        {
            if (!TryGetPathAndMetadata(arrowId, out List<Vector2Int> path, out EditorArrowMetadataData metadata))
                return false;
            if (path.Count < 2) return false;

            metadata.HasSecondaryEndpoint = !metadata.HasSecondaryEndpoint;
            if (metadata.HasSecondaryEndpoint)
            {
                metadata.SecondaryEndpointPathIndex = GetOppositeEndpointIndex(metadata.PrimaryEndpointPathIndex, path.Count);
                metadata.TopologyType = ArrowTopologyType.MultiEndpointSharedPath;
            }
            else
            {
                metadata.SecondaryEndpointPathIndex = GetOppositeEndpointIndex(metadata.PrimaryEndpointPathIndex, path.Count);
                metadata.TopologyType = ArrowTopologyType.SingleHeadSingleTail;
            }

            NormalizeArrowMetadata(arrowId);
            UpdatePathVisuals(arrowId);
            return true;
        }

        public bool SetPrimaryEndpoint(string arrowId, int pathIndex)
        {
            if (!TryGetPathAndMetadata(arrowId, out List<Vector2Int> path, out EditorArrowMetadataData metadata))
                return false;

            if (!IsEndpointIndex(pathIndex, path.Count)) return false;

            metadata.PrimaryEndpointPathIndex = pathIndex;
            if (metadata.HasSecondaryEndpoint)
            {
                metadata.SecondaryEndpointPathIndex = GetOppositeEndpointIndex(pathIndex, path.Count);
            }

            NormalizeArrowMetadata(arrowId);
            UpdatePathVisuals(arrowId);
            return true;
        }

        public void SetLinkGroup(string arrowId, string linkGroupId)
        {
            if (!_arrowMetadata.TryGetValue(arrowId, out EditorArrowMetadataData metadata)) return;

            metadata.LinkGroupId = (linkGroupId ?? string.Empty).Trim();
            RaiseArrowMetadataChanged(arrowId);
        }

        public void ClearLinkGroup(string arrowId)
        {
            SetLinkGroup(arrowId, string.Empty);
        }

        public List<string> GetLinkedArrowIds(string linkGroupId)
        {
            List<string> ids = new List<string>();
            if (string.IsNullOrWhiteSpace(linkGroupId)) return ids;

            string normalized = linkGroupId.Trim();
            foreach (KeyValuePair<string, EditorArrowMetadataData> kvp in _arrowMetadata)
            {
                if (string.Equals(kvp.Value.LinkGroupId, normalized, StringComparison.Ordinal))
                {
                    ids.Add(kvp.Key);
                }
            }

            ids.Sort(StringComparer.Ordinal);
            return ids;
        }

        public List<string> GetAllLinkGroupIds()
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (EditorArrowMetadataData metadata in _arrowMetadata.Values)
            {
                if (!string.IsNullOrWhiteSpace(metadata.LinkGroupId))
                {
                    ids.Add(metadata.LinkGroupId.Trim());
                }
            }

            List<string> result = new List<string>(ids);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        public void FlipArrowPath(string arrowID)
        {
            if (!TryGetPathAndMetadata(arrowID, out List<Vector2Int> path, out EditorArrowMetadataData metadata) ||
                path.Count == 0)
                return;

            int nextPrimary = GetOppositeEndpointIndex(metadata.PrimaryEndpointPathIndex, path.Count);
            metadata.PrimaryEndpointPathIndex = nextPrimary;
            if (metadata.HasSecondaryEndpoint)
            {
                metadata.SecondaryEndpointPathIndex = GetOppositeEndpointIndex(nextPrimary, path.Count);
            }

            NormalizeArrowMetadata(arrowID);
            UpdatePathVisuals(arrowID);
        }

        public void ClearAllPaths()
        {
            _arrowPaths.Clear();
            _arrowMetadata.Clear();
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
            HashSet<string> uniqueIDs = new HashSet<string>();

            foreach (SpecialCellSaveData specialCell in GetSpecialSaveData())
            {
                if (specialCell.Type != type) continue;

                if (specialCell.Type == BoardSpecialType.CounterBlock)
                {
                    uniqueIDs.Add(!string.IsNullOrEmpty(specialCell.Id)
                        ? specialCell.Id
                        : $"{specialCell.Position.x},{specialCell.Position.y}");
                    continue;
                }

                if (!string.IsNullOrEmpty(specialCell.PortalId))
                {
                    uniqueIDs.Add(specialCell.PortalId);
                }
                else
                {
                    uniqueIDs.Add($"{specialCell.Position.x},{specialCell.Position.y}");
                }
            }

            return new List<string>(uniqueIDs);
        }

        private void UpdatePathVisuals(string arrowID)
        {
            if (!_arrowPaths.TryGetValue(arrowID, out List<Vector2Int> path) || path.Count == 0) return;
            ApplyLegacyFallbackVisuals(arrowID, path);
            RaiseArrowMetadataChanged(arrowID);
        }

        private void ApplyLegacyFallbackVisuals(string arrowID, List<Vector2Int> path)
        {
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
                    {
                        SetCellVisual(x, y, CellType.EmptyDot, string.Empty);
                    }
                }
            }
        }

        private void SetCellVisual(int x, int y, CellType type, string id)
        {
            if (!IsValidPosition(x, y)) return;

            CellData cell = _grid[x, y];
            cell.type = type;
            cell.arrowID = id;
            if (!SuppressEvents)
            {
                OnCellChanged?.Invoke(x, y, cell);
            }
        }

        private void ClearGridCells()
        {
            if (_grid == null) return;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y].type = CellType.EmptyDot;
                    _grid[x, y].arrowID = string.Empty;
                }
            }
        }

        private EditorArrowMetadataData CreateDefaultMetadata(string arrowId, bool headFirst)
        {
            return new EditorArrowMetadataData
            {
                ArrowId = arrowId,
                PrimaryEndpointPathIndex = headFirst ? 0 : 1,
                HasSecondaryEndpoint = false,
                SecondaryEndpointPathIndex = 0,
                LinkGroupId = string.Empty,
                TopologyType = ArrowTopologyType.SingleHeadSingleTail
            };
        }

        private EditorArrowMetadataData CreateMetadataFromSaveData(ArrowSaveData arrow)
        {
            EditorArrowMetadataData metadata = new EditorArrowMetadataData
            {
                ArrowId = arrow.ArrowID,
                LinkGroupId = arrow.LinkGroupId ?? string.Empty,
                TopologyType = arrow.TopologyType
            };

            if (arrow.Endpoints != null && arrow.Endpoints.Count > 0)
            {
                int primaryIndex = -1;
                int secondaryIndex = -1;

                for (int i = 0; i < arrow.Endpoints.Count; i++)
                {
                    ArrowEndpointSaveData endpoint = arrow.Endpoints[i];
                    if (endpoint == null) continue;

                    if (endpoint.IsPrimary || primaryIndex < 0)
                    {
                        if (primaryIndex >= 0 && secondaryIndex < 0)
                        {
                            secondaryIndex = primaryIndex;
                        }

                        primaryIndex = endpoint.PathIndex;
                    }
                    else if (secondaryIndex < 0)
                    {
                        secondaryIndex = endpoint.PathIndex;
                    }
                }

                metadata.PrimaryEndpointPathIndex = primaryIndex >= 0 ? primaryIndex : ResolveLegacyPrimaryIndex(arrow);
                metadata.HasSecondaryEndpoint = secondaryIndex >= 0;
                metadata.SecondaryEndpointPathIndex = secondaryIndex >= 0 ? secondaryIndex : 0;
            }
            else
            {
                metadata.PrimaryEndpointPathIndex = ResolveLegacyPrimaryIndex(arrow);
                metadata.HasSecondaryEndpoint = false;
                metadata.SecondaryEndpointPathIndex = 0;
            }

            return metadata;
        }

        private ArrowSaveData BuildArrowSaveData(string arrowId)
        {
            if (!TryGetPathAndMetadata(arrowId, out List<Vector2Int> path, out EditorArrowMetadataData metadata))
                return null;

            bool isHeadFirst = metadata.PrimaryEndpointPathIndex == 0;
            List<ArrowEndpointSaveData> endpoints = new List<ArrowEndpointSaveData>
            {
                CreateEndpointSaveData(path, metadata.PrimaryEndpointPathIndex, true)
            };

            if (metadata.HasSecondaryEndpoint)
            {
                endpoints.Add(CreateEndpointSaveData(path, metadata.SecondaryEndpointPathIndex, false));
            }

            return new ArrowSaveData(arrowId, path, isHeadFirst, endpoints, metadata.LinkGroupId, metadata.TopologyType);
        }

        private static ArrowEndpointSaveData CreateEndpointSaveData(IReadOnlyList<Vector2Int> path, int pathIndex,
            bool isPrimary)
        {
            return new ArrowEndpointSaveData(pathIndex, BuildEndpointDirection(path, pathIndex), isPrimary);
        }

        private void NormalizeArrowMetadata(string arrowId)
        {
            if (!_arrowPaths.TryGetValue(arrowId, out List<Vector2Int> path) || path.Count == 0)
            {
                if (_arrowMetadata.Remove(arrowId))
                {
                    RaiseArrowMetadataChanged(arrowId);
                }

                return;
            }

            if (!_arrowMetadata.TryGetValue(arrowId, out EditorArrowMetadataData metadata))
            {
                metadata = CreateDefaultMetadata(arrowId, true);
                _arrowMetadata[arrowId] = metadata;
            }

            metadata.ArrowId = arrowId;
            metadata.PrimaryEndpointPathIndex = NormalizeEndpointIndex(metadata.PrimaryEndpointPathIndex, path.Count,
                preferStartIfInvalid: metadata.PrimaryEndpointPathIndex == 0);

            if (metadata.HasSecondaryEndpoint && path.Count >= 2)
            {
                metadata.SecondaryEndpointPathIndex = GetOppositeEndpointIndex(metadata.PrimaryEndpointPathIndex, path.Count);
                metadata.TopologyType = ArrowTopologyType.MultiEndpointSharedPath;
            }
            else
            {
                metadata.HasSecondaryEndpoint = false;
                metadata.SecondaryEndpointPathIndex = GetOppositeEndpointIndex(metadata.PrimaryEndpointPathIndex, path.Count);
                metadata.TopologyType = ArrowTopologyType.SingleHeadSingleTail;
            }

            metadata.LinkGroupId = (metadata.LinkGroupId ?? string.Empty).Trim();
        }

        private bool TryGetPathAndMetadata(string arrowId, out List<Vector2Int> path, out EditorArrowMetadataData metadata)
        {
            bool hasPath = _arrowPaths.TryGetValue(arrowId, out path);
            bool hasMetadata = _arrowMetadata.TryGetValue(arrowId, out metadata);
            return hasPath && hasMetadata;
        }

        private void RaiseArrowMetadataChanged(string arrowId)
        {
            OnArrowMetadataChanged?.Invoke(arrowId);
        }

        private static int NormalizeEndpointIndex(int pathIndex, int pathCount, bool preferStartIfInvalid)
        {
            if (pathCount <= 1) return preferStartIfInvalid ? 0 : 1;
            if (pathIndex == 0 || pathIndex == pathCount - 1) return pathIndex;
            return preferStartIfInvalid ? 0 : pathCount - 1;
        }

        private static bool IsEndpointIndex(int pathIndex, int pathCount)
        {
            return pathCount > 0 && (pathIndex == 0 || pathIndex == pathCount - 1);
        }

        private static int ResolveLegacyPrimaryIndex(ArrowSaveData arrow)
        {
            int pathCount = arrow.Path != null ? arrow.Path.Count : 0;
            if (pathCount <= 1) return 0;
            return arrow.IsHeadFirst ? 0 : pathCount - 1;
        }

        private static int GetOppositeEndpointIndex(int pathIndex, int pathCount)
        {
            if (pathCount <= 1) return 0;
            return pathIndex == 0 ? pathCount - 1 : 0;
        }

        private static Direction4 BuildEndpointDirection(IReadOnlyList<Vector2Int> path, int pathIndex)
        {
            if (path == null || path.Count <= 1) return Direction4.Up;

            int safeIndex = Mathf.Clamp(pathIndex, 0, path.Count - 1);
            Vector2Int current = path[safeIndex];
            int neighborIndex = safeIndex == 0 ? 1 : path.Count - 2;
            Vector2Int direction = current - path[neighborIndex];
            return Direction4Extensions.FromVector(direction);
        }
    }
}
