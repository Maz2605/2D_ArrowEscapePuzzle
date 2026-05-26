using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.Logic;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class GridView : MonoBehaviour
    {
        [Header("Prefabs & References")]
        [SerializeField] private CellView _cellPrefab;
        [SerializeField] private EditorArrowLine _arrowLinePrefab;
        [SerializeField] private List<SpecialCellVisualPrefabSlot> _specialCellPrefabs = new List<SpecialCellVisualPrefabSlot>();

        [Header("Hierarchy Parents")]
        [SerializeField] private Transform _gridParent;
        [SerializeField] private Transform _linesParent;
        [SerializeField] private Transform _specialMarkerParent;

        private GridSystem _gridLogic;
        private CellView[,] _cellViews;

        private readonly Dictionary<string, EditorArrowLine> _linesByID = new Dictionary<string, EditorArrowLine>();
        private readonly Dictionary<string, LineRenderer> _linkLinesByGroupId = new Dictionary<string, LineRenderer>();
        private readonly HashSet<string> _dirtyArrowIDs = new HashSet<string>();
        private readonly Dictionary<Vector2Int, EditorSpecialCellViewBase> _specialMarkers =
            new Dictionary<Vector2Int, EditorSpecialCellViewBase>();
        private bool _needCleanupStaleLines;
        private bool _needSpecialMarkerRefresh;
        private bool _needLinkGroupRefresh;
        private Transform _linkLinesParent;

        public void Initialize(GridSystem logic)
        {
            _gridLogic = logic;
            _gridLogic.OnCellChanged += HandleCellDataChanged;
            _gridLogic.OnArrowMetadataChanged += HandleArrowMetadataChanged;
            _gridLogic.OnGridRebuilt += HandleGridRebuilt;

            EnsureSpecialMarkerParent();
            EnsureLinkLinesParent();
            GenerateGridVisual();
            MarkAllDirty();
        }

        private void OnDestroy()
        {
            if (_gridLogic != null)
            {
                _gridLogic.OnCellChanged -= HandleCellDataChanged;
                _gridLogic.OnArrowMetadataChanged -= HandleArrowMetadataChanged;
                _gridLogic.OnGridRebuilt -= HandleGridRebuilt;
            }
        }

        private void HandleGridRebuilt()
        {
            RebuildGrid();
        }

        private void GenerateGridVisual()
        {
            int width = _gridLogic.Width;
            int height = _gridLogic.Height;
            _cellViews = new CellView[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CellView cellView = PoolingManager.Instance.Spawn(_cellPrefab, new Vector3(x, y, 0f),
                        Quaternion.identity, _gridParent);
                    cellView.InitPosition(x, y);

                    SpecialCellSaveData specialCell = _gridLogic.GetSpecialCellAt(x, y);
                    cellView.UpdateVisual(_gridLogic.GetCell(x, y), ShouldUseCellFallbackVisual(specialCell) ? specialCell : null);
                    _cellViews[x, y] = cellView;
                }
            }

            _needSpecialMarkerRefresh = true;
            _needLinkGroupRefresh = true;
        }

        private void HandleCellDataChanged(int x, int y, CellData updatedData)
        {
            SpecialCellSaveData specialCell = _gridLogic.GetSpecialCellAt(x, y);

            if (_cellViews != null && _cellViews[x, y] != null)
            {
                _cellViews[x, y].UpdateVisual(updatedData, ShouldUseCellFallbackVisual(specialCell) ? specialCell : null);
            }

            _needSpecialMarkerRefresh = true;

            if (!string.IsNullOrEmpty(updatedData.arrowID))
            {
                _dirtyArrowIDs.Add(updatedData.arrowID);
                _needLinkGroupRefresh = true;
            }
            else
            {
                _needCleanupStaleLines = true;
                _needLinkGroupRefresh = true;
            }
        }

        private void HandleArrowMetadataChanged(string arrowId)
        {
            if (!string.IsNullOrWhiteSpace(arrowId))
            {
                _dirtyArrowIDs.Add(arrowId);
            }

            _needLinkGroupRefresh = true;
        }

        private void LateUpdate()
        {
            if (_dirtyArrowIDs.Count == 0 && !_needCleanupStaleLines && !_needSpecialMarkerRefresh && !_needLinkGroupRefresh)
                return;

            if (_needSpecialMarkerRefresh)
            {
                RebuildSpecialMarkers();
                _needSpecialMarkerRefresh = false;
            }

            if (_needCleanupStaleLines)
            {
                CleanupStaleLines();
                _needCleanupStaleLines = false;
            }

            foreach (string id in _dirtyArrowIDs)
            {
                RefreshOneLine(id);
            }

            _dirtyArrowIDs.Clear();

            if (_needLinkGroupRefresh)
            {
                RebuildLinkGroupLines();
                _needLinkGroupRefresh = false;
            }
        }

        private void RefreshOneLine(string id)
        {
            List<Vector2Int> path = _gridLogic.GetArrowPath(id);
            EditorArrowMetadataData metadata = _gridLogic.GetArrowMetadata(id);

            if (path == null || path.Count == 0 || metadata == null)
            {
                if (_linesByID.TryGetValue(id, out EditorArrowLine stale) && stale != null)
                {
                    PoolingManager.Instance.Despawn(stale.gameObject);
                }

                _linesByID.Remove(id);
                return;
            }

            if (!_linesByID.TryGetValue(id, out EditorArrowLine line) || line == null)
            {
                line = PoolingManager.Instance.Spawn(_arrowLinePrefab, Vector3.zero, Quaternion.identity, _linesParent);
                _linesByID[id] = line;
            }

            line.Setup(id, path, metadata, EditorConstants.GetArrowColor(id));
        }

        private void CleanupStaleLines()
        {
            HashSet<string> activeIDs = new HashSet<string>(_gridLogic.GetAllArrowIDs());
            List<string> toRemove = new List<string>();

            foreach (KeyValuePair<string, EditorArrowLine> kvp in _linesByID)
            {
                if (activeIDs.Contains(kvp.Key)) continue;
                if (kvp.Value != null) PoolingManager.Instance.Despawn(kvp.Value.gameObject);
                toRemove.Add(kvp.Key);
            }

            foreach (string key in toRemove)
            {
                _linesByID.Remove(key);
            }
        }

        private void RebuildSpecialMarkers()
        {
            ClearSpecialMarkers();

            List<SpecialCellSaveData> specialCells = _gridLogic.GetSpecialSaveData();
            for (int i = 0; i < specialCells.Count; i++)
            {
                SpecialCellSaveData specialCell = specialCells[i];
                GameObject markerPrefab = GetSpecialMarkerPrefab(specialCell.Type);
                if (markerPrefab == null) continue;

                GameObject markerObject = PoolingManager.Instance.Spawn(markerPrefab, Vector3.zero, Quaternion.identity,
                    _specialMarkerParent);
                markerObject.name = $"Special_{specialCell.Type}_{specialCell.Position.x}_{specialCell.Position.y}";

                if (!markerObject.TryGetComponent(out EditorSpecialCellViewBase view))
                {
                    Debug.LogError($"[GridView] Prefab '{markerObject.name}' đang thiếu component kế thừa từ EditorSpecialCellViewBase!");
                    PoolingManager.Instance.Despawn(markerObject);
                    continue;
                }

                view.Setup(specialCell, GetSpecialCellColor(specialCell));
                foreach (Vector2Int occupiedPos in CounterBlockUtility.GetOccupiedPositions(specialCell))
                {
                    if (_gridLogic.IsValidPosition(occupiedPos.x, occupiedPos.y))
                    {
                        _specialMarkers[occupiedPos] = view;
                    }
                }
            }
        }

        private void RebuildLinkGroupLines()
        {
            ClearLinkGroupLines();

            List<string> groupIds = _gridLogic.GetAllLinkGroupIds();
            foreach (string groupId in groupIds)
            {
                List<string> linkedArrowIds = _gridLogic.GetLinkedArrowIds(groupId);
                if (linkedArrowIds.Count < 2) continue;

                List<Vector3> positions = new List<Vector3>();
                for (int i = 0; i < linkedArrowIds.Count; i++)
                {
                    Vector2Int? endpoint = _gridLogic.GetPrimaryEndpointPosition(linkedArrowIds[i]);
                    if (!endpoint.HasValue) continue;
                    positions.Add(new Vector3(endpoint.Value.x, endpoint.Value.y, 0.3f));
                }

                if (positions.Count < 2) continue;

                LineRenderer linkLine = CreateLinkLineRenderer(groupId);
                linkLine.positionCount = positions.Count;
                linkLine.SetPositions(positions.ToArray());
                _linkLinesByGroupId[groupId] = linkLine;
            }
        }

        private LineRenderer CreateLinkLineRenderer(string groupId)
        {
            GameObject lineObject = new GameObject($"LinkGroup_{groupId}");
            lineObject.transform.SetParent(_linkLinesParent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.useWorldSpace = true;
            line.startWidth = 0.12f;
            line.endWidth = 0.12f;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.sortingOrder = 10;

            Color groupColor = GetLinkGroupColor(groupId);
            line.startColor = groupColor;
            line.endColor = groupColor;
            return line;
        }

        private void ClearLinkGroupLines()
        {
            foreach (LineRenderer line in _linkLinesByGroupId.Values)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }

            _linkLinesByGroupId.Clear();
        }

        private void ClearSpecialMarkers()
        {
            HashSet<EditorSpecialCellViewBase> uniqueViews = new HashSet<EditorSpecialCellViewBase>(_specialMarkers.Values);
            foreach (EditorSpecialCellViewBase view in uniqueViews)
            {
                if (view != null) PoolingManager.Instance.Despawn(view.gameObject);
            }

            _specialMarkers.Clear();
        }

        private void EnsureSpecialMarkerParent()
        {
            if (_specialMarkerParent != null) return;

            GameObject root = new GameObject("SpecialMarkers");
            root.transform.SetParent(_gridParent, false);
            root.transform.localPosition = Vector3.zero;
            _specialMarkerParent = root.transform;
        }

        private void EnsureLinkLinesParent()
        {
            if (_linkLinesParent != null) return;

            GameObject root = new GameObject("LinkGroupLines");
            root.transform.SetParent(_linesParent != null ? _linesParent : _gridParent, false);
            root.transform.localPosition = Vector3.zero;
            _linkLinesParent = root.transform;
        }

        private GameObject GetSpecialMarkerPrefab(BoardSpecialType type)
        {
            for (int i = 0; i < _specialCellPrefabs.Count; i++)
            {
                SpecialCellVisualPrefabSlot slot = _specialCellPrefabs[i];
                if (slot != null && slot.Type == type && slot.Prefab != null) return slot.Prefab;
            }

            return null;
        }

        private bool ShouldUseCellFallbackVisual(SpecialCellSaveData specialCell)
        {
            return specialCell != null && GetSpecialMarkerPrefab(specialCell.Type) == null;
        }

        private static Color GetSpecialCellColor(SpecialCellSaveData specialCell)
        {
            if (specialCell.Type == BoardSpecialType.Redirect)
                return new Color(0.95f, 0.73f, 0.16f, 0.95f);

            if (specialCell.Type == BoardSpecialType.CounterBlock)
                return new Color(0.18f, 0.76f, 0.65f, 0.95f);

            return PortalVisualUtility.GetPortalColor(specialCell.PortalId);
        }

        private static Color GetLinkGroupColor(string groupId)
        {
            int seed = Mathf.Abs((groupId ?? string.Empty).GetHashCode());
            float hue = (seed % 100) / 100f;
            return Color.HSVToRGB(hue, 0.45f, 0.95f);
        }

        private void MarkAllDirty()
        {
            foreach (string id in _gridLogic.GetAllArrowIDs()) _dirtyArrowIDs.Add(id);
            _needCleanupStaleLines = true;

            for (int x = 0; x < _gridLogic.Width; x++)
            {
                for (int y = 0; y < _gridLogic.Height; y++)
                {
                    SpecialCellSaveData specialCell = _gridLogic.GetSpecialCellAt(x, y);
                    if (_cellViews[x, y] != null)
                    {
                        _cellViews[x, y].UpdateVisual(_gridLogic.GetCell(x, y),
                            ShouldUseCellFallbackVisual(specialCell) ? specialCell : null);
                    }
                }
            }

            _needSpecialMarkerRefresh = true;
            _needLinkGroupRefresh = true;
        }

        public void PlayArrowBounce(string arrowID)
        {
            if (_linesByID.TryGetValue(arrowID, out EditorArrowLine line) && line != null)
            {
                line.PlayBounceEffect();
            }
        }

        public void PlaySpecialCellBounce(Vector2Int position)
        {
            if (_specialMarkers.TryGetValue(position, out EditorSpecialCellViewBase view) && view != null)
            {
                view.PlayBounceEffect();
            }
        }

        public void ClearVisuals()
        {
            if (_cellViews != null)
            {
                foreach (CellView cell in _cellViews)
                {
                    if (cell != null) PoolingManager.Instance.Despawn(cell.gameObject);
                }
            }

            _cellViews = null;
            ClearSpecialMarkers();
            ClearLinkGroupLines();

            foreach (KeyValuePair<string, EditorArrowLine> kvp in _linesByID)
            {
                if (kvp.Value != null) PoolingManager.Instance.Despawn(kvp.Value.gameObject);
            }

            _linesByID.Clear();
            _dirtyArrowIDs.Clear();
            _needCleanupStaleLines = false;
            _needSpecialMarkerRefresh = false;
            _needLinkGroupRefresh = false;
        }

        public void RebuildGrid()
        {
            ClearVisuals();
            EnsureLinkLinesParent();
            GenerateGridVisual();
            MarkAllDirty();
        }
    }
}
