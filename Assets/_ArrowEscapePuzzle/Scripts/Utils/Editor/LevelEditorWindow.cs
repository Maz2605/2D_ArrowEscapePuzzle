#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ArrowGame.Data.LevelProvider;
using ArrowGame.Gameplay.Logic;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEditor;
using UnityEngine;

namespace ArrowGame.Utils.Editor
{
    public class LevelEditorWindow : EditorWindow
    {
        private static readonly Vector2Int InvalidPosition = new Vector2Int(int.MinValue, int.MinValue);

        private enum EditorToolMode
        {
            Paint,
            SelectArrow,
            LinkArrow
        }

        private LevelDataSO currentLevel;

        private sealed class EditorCell
        {
            public CellType type = CellType.None;
            public string id = string.Empty;
        }

        private sealed class EditorArrowMetadata
        {
            public string Id = string.Empty;
            public Vector2Int PrimaryEndpointPosition = InvalidPosition;
            public bool HasSecondaryEndpoint;
            public Vector2Int SecondaryEndpointPosition = InvalidPosition;
            public string LinkGroupId = string.Empty;
            public ArrowTopologyType TopologyType = ArrowTopologyType.SingleHeadSingleTail;
        }

        private sealed class EditorArrowDraft
        {
            public string Id;
            public List<Vector2Int> Path = new List<Vector2Int>();
            public List<Vector2Int> EndpointCandidates = new List<Vector2Int>();
            public bool IsValid;
            public string ValidationError = string.Empty;
        }

        private EditorCell[,] tempGrid;
        private readonly Dictionary<string, EditorArrowMetadata> _arrowMetadata = new Dictionary<string, EditorArrowMetadata>();
        private readonly Dictionary<string, EditorArrowDraft> _arrowDrafts = new Dictionary<string, EditorArrowDraft>();
        private CellType brushType = CellType.ArrowHeadUp;
        private EditorToolMode _toolMode = EditorToolMode.Paint;
        private string _selectedArrowId = string.Empty;
        private Vector2 scrollPosition;
        private float cellSize = 55f;

        [MenuItem("Tools/Arrow Level Editor (Entity-Based)")]
        public static void ShowWindow()
        {
            GetWindow<LevelEditorWindow>("Arrow Editor");
        }

        private void OnGUI()
        {
            GUILayout.Label("Arrow Level Editor", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            currentLevel = (LevelDataSO)EditorGUILayout.ObjectField("Data SO:", currentLevel, typeof(LevelDataSO), false);
            if (EditorGUI.EndChangeCheck() && currentLevel != null)
            {
                LoadDataToGrid();
            }

            if (currentLevel == null)
            {
                EditorGUILayout.HelpBox("Kéo file LevelDataSO vào đây để bắt đầu", MessageType.Warning);
                return;
            }

            HandleHotkeys();

            GUILayout.BeginVertical("box");
            currentLevel.levelID = EditorGUILayout.TextField("Level ID:", currentLevel.levelID);

            EditorGUI.BeginChangeCheck();
            int newWidth = EditorGUILayout.IntField("Width (Ngang)", currentLevel.width);
            int newHeight = EditorGUILayout.IntField("Height (Dọc)", currentLevel.height);
            currentLevel.difficulty = (LevelDifficulty)EditorGUILayout.EnumPopup("Độ khó:", currentLevel.difficulty);

            if (EditorGUI.EndChangeCheck() || tempGrid == null || tempGrid.GetLength(0) != newWidth ||
                tempGrid.GetLength(1) != newHeight)
            {
                currentLevel.width = Mathf.Max(3, newWidth);
                currentLevel.height = Mathf.Max(3, newHeight);
                LoadDataToGrid();
            }

            GUILayout.EndVertical();

            DrawPalette();
            DrawMechanicToolsPanel();
            DrawArrowAuthoringPanel();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Zoom:", GUILayout.Width(50));
            cellSize = GUILayout.HorizontalSlider(cellSize, 30f, 100f, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Save Level", GUILayout.Width(150), GUILayout.Height(30)))
            {
                SaveGridToData();
            }

            GUILayout.EndHorizontal();

            DrawGrid();
        }

        private void DrawPalette()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Grid Paint", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            DrawBrushButton("↑ Đầu", CellType.ArrowHeadUp);
            DrawBrushButton("↓ Đầu", CellType.ArrowHeadDown);
            DrawBrushButton("← Đầu", CellType.ArrowHeadLeft);
            DrawBrushButton("→ Đầu", CellType.ArrowHeadRight);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawBrushButton("═ Thân", CellType.ArrowBodyHorizontal);
            DrawBrushButton("║ Thân", CellType.ArrowBodyVertical);
            DrawBrushButton("• Trống", CellType.EmptyDot);
            DrawBrushButton("X Xóa", CellType.None);
            GUILayout.EndHorizontal();

            GUILayout.Label("Brush head chỉ còn dùng để tạo ID nhanh. Endpoint thực tế được author ở panel bên dưới.");
            GUILayout.EndVertical();
        }

        private void DrawMechanicToolsPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Mechanic Tools", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            DrawToolModeButton("[Paint] Grid Paint", EditorToolMode.Paint);
            DrawToolModeButton("[5] Select/Edit", EditorToolMode.SelectArrow);
            DrawToolModeButton("[6] Link Arrow", EditorToolMode.LinkArrow);
            GUILayout.EndHorizontal();

            EditorArrowDraft selectedDraft = GetSelectedDraft();
            if (selectedDraft == null)
            {
                EditorGUILayout.HelpBox("Chọn một arrow để chỉnh mechanic metadata. Trong mode Link, click arrow đầu tiên để set base arrow.", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            EditorArrowMetadata metadata = GetOrCreateMetadata(selectedDraft);
            GUILayout.Label($"Selected Arrow: {selectedDraft.Id}");
            GUILayout.Label($"Current Tool: {GetToolModeLabel(_toolMode)}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("[Q] Prev", GUILayout.Height(24))) SelectAdjacentArrow(-1);
            if (GUILayout.Button("[E] Next", GUILayout.Height(24))) SelectAdjacentArrow(1);
            if (GUILayout.Button("[R] Swap Primary", GUILayout.Height(24))) SwapPrimaryEndpointForSelectedArrow();
            if (GUILayout.Button("[T] Toggle 2-Head", GUILayout.Height(24))) ToggleSecondaryEndpointForSelectedArrow();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("[G] New Link Group", GUILayout.Height(24))) AssignNewLinkGroupToSelectedArrow();
            if (GUILayout.Button("[Backspace] Clear Link", GUILayout.Height(24))) ClearLinkGroupForSelectedArrow();
            GUILayout.EndHorizontal();

            if (_toolMode == EditorToolMode.LinkArrow)
            {
                string activeGroup = string.IsNullOrWhiteSpace(metadata.LinkGroupId)
                    ? "(sẽ auto tạo khi click arrow khác)"
                    : metadata.LinkGroupId;
                EditorGUILayout.HelpBox(
                    $"Link Mode: click arrow khác trên grid để toggle vào/ra Link Group của arrow {selectedDraft.Id}. Group hiện tại: {activeGroup}",
                    MessageType.None);
            }
            else if (_toolMode == EditorToolMode.SelectArrow)
            {
                EditorGUILayout.HelpBox("Select/Edit Mode: click arrow trên grid để chọn nhanh, sau đó dùng R/T/G/Delete để chỉnh metadata.", MessageType.None);
            }

            GUILayout.EndVertical();
        }

        private void DrawArrowAuthoringPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Arrow Endpoints", EditorStyles.boldLabel);

            if (_arrowDrafts.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa có arrow nào trên grid.", MessageType.Info);
                GUILayout.EndVertical();
                return;
            }

            foreach (EditorArrowDraft draft in _arrowDrafts.Values.OrderBy(d => GetSortableId(d.Id)))
            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Label(IsSelectedArrow(draft.Id) ? $"Arrow {draft.Id}  [Selected]" : $"Arrow {draft.Id}");
                if (GUILayout.Button(IsSelectedArrow(draft.Id) ? "Selected" : "Select", GUILayout.Width(80)))
                {
                    SelectArrow(draft.Id);
                }
                GUILayout.EndHorizontal();

                if (!draft.IsValid)
                {
                    EditorGUILayout.HelpBox(draft.ValidationError, MessageType.Warning);
                    GUILayout.EndVertical();
                    continue;
                }

                EditorArrowMetadata metadata = GetOrCreateMetadata(draft);
                GUILayout.Label($"Path Length: {draft.Path.Count}");

                string[] endpointOptions = BuildEndpointOptions(draft.EndpointCandidates);
                int currentPrimaryIndex = GetEndpointIndex(draft.EndpointCandidates, metadata.PrimaryEndpointPosition);
                int newPrimaryIndex = EditorGUILayout.Popup("Primary Endpoint", currentPrimaryIndex, endpointOptions);
                if (newPrimaryIndex != currentPrimaryIndex)
                {
                    metadata.PrimaryEndpointPosition = draft.EndpointCandidates[newPrimaryIndex];
                    if (metadata.HasSecondaryEndpoint &&
                        metadata.SecondaryEndpointPosition == metadata.PrimaryEndpointPosition)
                    {
                        metadata.SecondaryEndpointPosition = GetOtherEndpointPosition(draft, metadata.PrimaryEndpointPosition);
                    }

                    RebuildArrowDraftsAndVisuals();
                }

                bool hasSecondary = draft.EndpointCandidates.Count > 1 &&
                                    EditorGUILayout.Toggle("Add Secondary Endpoint", metadata.HasSecondaryEndpoint);
                if (hasSecondary != metadata.HasSecondaryEndpoint)
                {
                    metadata.HasSecondaryEndpoint = hasSecondary;
                    metadata.SecondaryEndpointPosition = hasSecondary
                        ? GetOtherEndpointPosition(draft, metadata.PrimaryEndpointPosition)
                        : InvalidPosition;
                    RebuildArrowDraftsAndVisuals();
                }

                using (new EditorGUI.DisabledScope(!metadata.HasSecondaryEndpoint || draft.EndpointCandidates.Count <= 1))
                {
                    int secondaryIndex = GetEndpointIndex(draft.EndpointCandidates, metadata.SecondaryEndpointPosition);
                    secondaryIndex = Mathf.Max(secondaryIndex, 0);
                    string secondaryLabel = metadata.HasSecondaryEndpoint
                        ? endpointOptions[secondaryIndex]
                        : "Disabled";
                    EditorGUILayout.TextField("Secondary Endpoint", secondaryLabel);
                }

                ArrowTopologyType newTopology =
                    (ArrowTopologyType)EditorGUILayout.EnumPopup("Topology", metadata.TopologyType);
                if (newTopology != metadata.TopologyType)
                {
                    metadata.TopologyType = newTopology;
                    RebuildArrowDraftsAndVisuals();
                }

                string newLinkGroup = EditorGUILayout.TextField("Link Group", metadata.LinkGroupId ?? string.Empty);
                if (newLinkGroup != metadata.LinkGroupId)
                {
                    metadata.LinkGroupId = newLinkGroup;
                }

                if (!string.IsNullOrWhiteSpace(metadata.LinkGroupId) && CountArrowsInLinkGroup(metadata.LinkGroupId) <= 1)
                {
                    EditorGUILayout.HelpBox("Link Group này hiện chỉ có 1 arrow. Runtime vẫn load được, nhưng linked mechanic sẽ chưa có tác dụng.", MessageType.Warning);
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }

        private static int GetSortableId(string id)
        {
            return int.TryParse(id, out int numericId) ? numericId : int.MaxValue;
        }

        private string[] BuildEndpointOptions(List<Vector2Int> endpointCandidates)
        {
            string[] options = new string[endpointCandidates.Count];
            for (int i = 0; i < endpointCandidates.Count; i++)
            {
                Vector2Int endpoint = endpointCandidates[i];
                options[i] = $"({endpoint.x}, {endpoint.y})";
            }

            return options;
        }

        private void DrawBrushButton(string label, CellType type)
        {
            Color oldColor = GUI.backgroundColor;
            if (brushType == type) GUI.backgroundColor = Color.yellow;
            else GUI.backgroundColor = GetColorForType(type);

            if (GUILayout.Button(label, GUILayout.Height(25))) brushType = type;
            GUI.backgroundColor = oldColor;
        }

        private void DrawToolModeButton(string label, EditorToolMode mode)
        {
            Color oldColor = GUI.backgroundColor;
            if (_toolMode == mode) GUI.backgroundColor = new Color(0.95f, 0.85f, 0.35f);

            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                SetToolMode(mode);
            }

            GUI.backgroundColor = oldColor;
        }

        private void DrawGrid()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUILayout.Space(10);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(cellSize * 0.25f));

            for (int y = currentLevel.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int x = 0; x < currentLevel.width; x++)
                {
                    EditorCell cell = tempGrid[x, y];
                    GUI.backgroundColor = GetColorForType(cell.type);
                    string label = GetLabelForType(cell.type, cell.id);

                    if (GUILayout.Button(label, btnStyle, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        HandleGridCellInteraction(x, y, cell);
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
        }

        private void HandleGridCellInteraction(int x, int y, EditorCell cell)
        {
            switch (_toolMode)
            {
                case EditorToolMode.Paint:
                    ApplyBrush(x, y, cell);
                    break;
                case EditorToolMode.SelectArrow:
                    HandleSelectArrowCell(cell);
                    break;
                case EditorToolMode.LinkArrow:
                    HandleLinkArrowCell(cell);
                    break;
            }
        }

        private void ApplyBrush(int x, int y, EditorCell cell)
        {
            string previousId = cell.id;

            if (brushType == CellType.None || brushType == CellType.EmptyDot)
            {
                cell.type = brushType;
                cell.id = string.Empty;
            }
            else if (ArrowCellTypeBuilder.IsHeadType(brushType))
            {
                cell.type = brushType;
                cell.id = GetNextAvailableID();
            }
            else
            {
                string adjacentID = GetAdjacentArrowID(x, y);
                if (!string.IsNullOrEmpty(adjacentID))
                {
                    cell.type = brushType;
                    cell.id = adjacentID;
                }
                else
                {
                    EditorUtility.DisplayDialog("Lỗi", "Hãy vẽ Thân sát cạnh một ô arrow đã có sẵn!", "OK");
                    return;
                }
            }

            if (!string.IsNullOrEmpty(previousId) && previousId != cell.id && !GridContainsArrow(previousId))
            {
                _arrowMetadata.Remove(previousId);
            }

            RebuildArrowDraftsAndVisuals();
        }

        private void HandleSelectArrowCell(EditorCell cell)
        {
            if (cell == null || string.IsNullOrEmpty(cell.id))
            {
                _selectedArrowId = string.Empty;
                Repaint();
                return;
            }

            SelectArrow(cell.id);
        }

        private void HandleLinkArrowCell(EditorCell cell)
        {
            if (cell == null || string.IsNullOrEmpty(cell.id))
            {
                return;
            }

            if (string.IsNullOrEmpty(_selectedArrowId) || !_arrowDrafts.ContainsKey(_selectedArrowId))
            {
                SelectArrow(cell.id);
                return;
            }

            if (cell.id == _selectedArrowId)
            {
                SelectArrow(cell.id);
                return;
            }

            ToggleArrowLinkMembership(_selectedArrowId, cell.id);
        }

        private void SaveGridToData()
        {
            if (!ValidateGridLogic(out List<string> errors))
            {
                EditorUtility.DisplayDialog("Không thể lưu level", string.Join("\n", errors), "OK");
                return;
            }

            currentLevel.arrows.Clear();
            foreach (EditorArrowDraft draft in _arrowDrafts.Values.OrderBy(d => GetSortableId(d.Id)))
            {
                EditorArrowMetadata metadata = GetOrCreateMetadata(draft);
                ArrowSaveData arrowSaveData = BuildArrowSaveData(draft, metadata);
                if (arrowSaveData != null)
                {
                    currentLevel.arrows.Add(arrowSaveData);
                }
            }

            EditorUtility.SetDirty(currentLevel);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"<color=green>Đã lưu Level {currentLevel.levelID} với {currentLevel.arrows.Count} arrow dùng endpoint metadata.</color>");
        }

        private void LoadDataToGrid()
        {
            tempGrid = new EditorCell[currentLevel.width, currentLevel.height];
            for (int x = 0; x < currentLevel.width; x++)
            {
                for (int y = 0; y < currentLevel.height; y++)
                {
                    tempGrid[x, y] = new EditorCell { type = CellType.EmptyDot };
                }
            }

            _arrowMetadata.Clear();
            _arrowDrafts.Clear();
            _selectedArrowId = string.Empty;

            if (currentLevel.arrows == null) return;

            for (int i = 0; i < currentLevel.arrows.Count; i++)
            {
                ArrowSaveData arrow = currentLevel.arrows[i];
                if (arrow == null || string.IsNullOrEmpty(arrow.ArrowID) || arrow.Path == null) continue;

                for (int j = 0; j < arrow.Path.Count; j++)
                {
                    Vector2Int pos = arrow.Path[j];
                    if (IsWithinEditorBounds(pos))
                    {
                        tempGrid[pos.x, pos.y].id = arrow.ArrowID;
                        tempGrid[pos.x, pos.y].type = CellType.ArrowBodyHorizontal;
                    }
                }

                EditorArrowMetadata metadata = CreateMetadataFromArrow(arrow);
                _arrowMetadata[arrow.ArrowID] = metadata;
            }

            RebuildArrowDraftsAndVisuals();
        }

        private EditorArrowMetadata CreateMetadataFromArrow(ArrowSaveData arrow)
        {
            EditorArrowMetadata metadata = new EditorArrowMetadata
            {
                Id = arrow.ArrowID,
                LinkGroupId = arrow.LinkGroupId ?? string.Empty,
                TopologyType = arrow.TopologyType
            };

            if (arrow.Path == null || arrow.Path.Count == 0)
            {
                metadata.PrimaryEndpointPosition = InvalidPosition;
                metadata.SecondaryEndpointPosition = InvalidPosition;
                metadata.HasSecondaryEndpoint = false;
                return metadata;
            }

            if (arrow.Endpoints != null && arrow.Endpoints.Count > 0)
            {
                ArrowEndpointSaveData primary = arrow.Endpoints.FirstOrDefault(endpoint => endpoint != null && endpoint.IsPrimary)
                                                ?? arrow.Endpoints.FirstOrDefault(endpoint => endpoint != null);
                if (primary != null && IsValidPathIndex(primary.PathIndex, arrow.Path.Count))
                {
                    metadata.PrimaryEndpointPosition = arrow.Path[primary.PathIndex];
                }

                ArrowEndpointSaveData secondary = arrow.Endpoints.FirstOrDefault(endpoint =>
                    endpoint != null && !ReferenceEquals(endpoint, primary));
                if (secondary != null && IsValidPathIndex(secondary.PathIndex, arrow.Path.Count))
                {
                    metadata.HasSecondaryEndpoint = true;
                    metadata.SecondaryEndpointPosition = arrow.Path[secondary.PathIndex];
                }
            }
            else
            {
                int legacyIndex = arrow.IsHeadFirst ? 0 : arrow.Path.Count - 1;
                metadata.PrimaryEndpointPosition = arrow.Path[Mathf.Clamp(legacyIndex, 0, arrow.Path.Count - 1)];
                metadata.HasSecondaryEndpoint = false;
                metadata.SecondaryEndpointPosition = InvalidPosition;
            }

            if (!metadata.HasSecondaryEndpoint)
            {
                metadata.SecondaryEndpointPosition = InvalidPosition;
            }

            return metadata;
        }

        private void RebuildArrowDraftsAndVisuals()
        {
            _arrowDrafts.Clear();

            Dictionary<string, List<Vector2Int>> arrowPositions = CollectArrowPositionsFromGrid();
            PruneMissingMetadata(arrowPositions.Keys);

            foreach (KeyValuePair<string, List<Vector2Int>> kvp in arrowPositions)
            {
                EditorArrowDraft draft = BuildDraft(kvp.Key, kvp.Value);
                _arrowDrafts[kvp.Key] = draft;

                if (!draft.IsValid) continue;

                EditorArrowMetadata metadata = GetOrCreateMetadata(draft);
                NormalizeMetadata(metadata, draft);
                ApplyDraftVisuals(draft, metadata);
            }

            if (!string.IsNullOrEmpty(_selectedArrowId) && !_arrowDrafts.ContainsKey(_selectedArrowId))
            {
                _selectedArrowId = string.Empty;
            }

            ResetEmptyCells();
        }

        private Dictionary<string, List<Vector2Int>> CollectArrowPositionsFromGrid()
        {
            Dictionary<string, List<Vector2Int>> arrowPositions = new Dictionary<string, List<Vector2Int>>();
            for (int x = 0; x < currentLevel.width; x++)
            {
                for (int y = 0; y < currentLevel.height; y++)
                {
                    string id = tempGrid[x, y].id;
                    if (string.IsNullOrEmpty(id)) continue;

                    if (!arrowPositions.TryGetValue(id, out List<Vector2Int> positions))
                    {
                        positions = new List<Vector2Int>();
                        arrowPositions.Add(id, positions);
                    }

                    positions.Add(new Vector2Int(x, y));
                }
            }

            return arrowPositions;
        }

        private void PruneMissingMetadata(IEnumerable<string> activeIds)
        {
            HashSet<string> idSet = new HashSet<string>(activeIds);
            List<string> toRemove = new List<string>();
            foreach (string id in _arrowMetadata.Keys)
            {
                if (!idSet.Contains(id))
                {
                    toRemove.Add(id);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                _arrowMetadata.Remove(toRemove[i]);
            }
        }

        private EditorArrowDraft BuildDraft(string id, List<Vector2Int> positions)
        {
            EditorArrowDraft draft = new EditorArrowDraft
            {
                Id = id
            };

            if (TryReconstructPath(id, positions, out List<Vector2Int> path, out List<Vector2Int> endpoints,
                    out string error))
            {
                draft.Path = path;
                draft.EndpointCandidates = endpoints;
                draft.IsValid = true;
            }
            else
            {
                draft.Path = positions.OrderBy(pos => pos.y).ThenBy(pos => pos.x).ToList();
                draft.EndpointCandidates = new List<Vector2Int>();
                draft.IsValid = false;
                draft.ValidationError = error;
            }

            return draft;
        }

        private bool TryReconstructPath(string id, List<Vector2Int> positions, out List<Vector2Int> path,
            out List<Vector2Int> endpoints, out string error)
        {
            path = new List<Vector2Int>();
            endpoints = new List<Vector2Int>();
            error = string.Empty;

            if (positions == null || positions.Count == 0)
            {
                error = $"Arrow {id} không có ô nào.";
                return false;
            }

            HashSet<Vector2Int> nodeSet = new HashSet<Vector2Int>(positions);
            Dictionary<Vector2Int, List<Vector2Int>> graph = new Dictionary<Vector2Int, List<Vector2Int>>();
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int pos = positions[i];
                List<Vector2Int> neighbors = new List<Vector2Int>();
                for (int d = 0; d < dirs.Length; d++)
                {
                    Vector2Int next = pos + dirs[d];
                    if (nodeSet.Contains(next))
                    {
                        neighbors.Add(next);
                    }
                }

                graph[pos] = neighbors;
            }

            if (positions.Count == 1)
            {
                path.Add(positions[0]);
                endpoints.Add(positions[0]);
                return true;
            }

            foreach (KeyValuePair<Vector2Int, List<Vector2Int>> kvp in graph)
            {
                if (kvp.Value.Count == 1)
                {
                    endpoints.Add(kvp.Key);
                }
                else if (kvp.Value.Count != 2)
                {
                    error = $"Arrow {id} không phải path tuyến tính. Ô {kvp.Key} có {kvp.Value.Count} hàng xóm.";
                    return false;
                }
            }

            if (endpoints.Count != 2)
            {
                error = $"Arrow {id} phải có đúng 2 đầu mút, hiện có {endpoints.Count}.";
                return false;
            }

            Vector2Int start = ResolvePreferredStartPosition(id, endpoints);
            Vector2Int current = start;
            Vector2Int previous = InvalidPosition;

            while (path.Count < positions.Count)
            {
                path.Add(current);
                List<Vector2Int> neighbors = graph[current];
                Vector2Int next = InvalidPosition;
                for (int i = 0; i < neighbors.Count; i++)
                {
                    if (neighbors[i] != previous)
                    {
                        next = neighbors[i];
                        break;
                    }
                }

                if (next == InvalidPosition) break;
                previous = current;
                current = next;
            }

            if (path.Count != positions.Count)
            {
                error = $"Arrow {id} không liên thông hoàn chỉnh.";
                return false;
            }

            endpoints = new List<Vector2Int> { path[0], path[path.Count - 1] };
            return true;
        }

        private Vector2Int ResolvePreferredStartPosition(string id, List<Vector2Int> endpoints)
        {
            if (_arrowMetadata.TryGetValue(id, out EditorArrowMetadata metadata))
            {
                for (int i = 0; i < endpoints.Count; i++)
                {
                    if (endpoints[i] == metadata.PrimaryEndpointPosition)
                    {
                        return endpoints[i];
                    }
                }
            }

            List<Vector2Int> existingHeadCells = GetExistingHeadCells(id);
            for (int i = 0; i < existingHeadCells.Count; i++)
            {
                Vector2Int headCell = existingHeadCells[i];
                for (int j = 0; j < endpoints.Count; j++)
                {
                    if (endpoints[j] == headCell)
                    {
                        return headCell;
                    }
                }
            }

            return endpoints.OrderBy(pos => pos.x).ThenBy(pos => pos.y).First();
        }

        private List<Vector2Int> GetExistingHeadCells(string id)
        {
            List<Vector2Int> heads = new List<Vector2Int>();
            for (int x = 0; x < currentLevel.width; x++)
            {
                for (int y = 0; y < currentLevel.height; y++)
                {
                    if (tempGrid[x, y].id == id && ArrowCellTypeBuilder.IsHeadType(tempGrid[x, y].type))
                    {
                        heads.Add(new Vector2Int(x, y));
                    }
                }
            }

            return heads;
        }

        private EditorArrowMetadata GetOrCreateMetadata(EditorArrowDraft draft)
        {
            if (!_arrowMetadata.TryGetValue(draft.Id, out EditorArrowMetadata metadata))
            {
                metadata = new EditorArrowMetadata
                {
                    Id = draft.Id,
                    PrimaryEndpointPosition = draft.EndpointCandidates.Count > 0 ? draft.EndpointCandidates[0] : InvalidPosition,
                    HasSecondaryEndpoint = false,
                    SecondaryEndpointPosition = InvalidPosition,
                    LinkGroupId = string.Empty,
                    TopologyType = ArrowTopologyType.SingleHeadSingleTail
                };
                _arrowMetadata.Add(draft.Id, metadata);
            }

            return metadata;
        }

        private EditorArrowDraft GetSelectedDraft()
        {
            if (string.IsNullOrEmpty(_selectedArrowId)) return null;
            _arrowDrafts.TryGetValue(_selectedArrowId, out EditorArrowDraft draft);
            return draft;
        }

        private void NormalizeMetadata(EditorArrowMetadata metadata, EditorArrowDraft draft)
        {
            if (draft.EndpointCandidates.Count == 0)
            {
                metadata.PrimaryEndpointPosition = InvalidPosition;
                metadata.SecondaryEndpointPosition = InvalidPosition;
                metadata.HasSecondaryEndpoint = false;
                return;
            }

            if (!draft.EndpointCandidates.Contains(metadata.PrimaryEndpointPosition))
            {
                metadata.PrimaryEndpointPosition = draft.EndpointCandidates[0];
            }

            if (draft.EndpointCandidates.Count <= 1)
            {
                metadata.HasSecondaryEndpoint = false;
                metadata.SecondaryEndpointPosition = InvalidPosition;
            }
            else
            {
                Vector2Int otherEndpoint = GetOtherEndpointPosition(draft, metadata.PrimaryEndpointPosition);
                if (metadata.HasSecondaryEndpoint)
                {
                    metadata.SecondaryEndpointPosition = otherEndpoint;
                }
                else if (metadata.SecondaryEndpointPosition == metadata.PrimaryEndpointPosition)
                {
                    metadata.SecondaryEndpointPosition = InvalidPosition;
                }
            }

            if (metadata.HasSecondaryEndpoint && metadata.TopologyType == ArrowTopologyType.SingleHeadSingleTail)
            {
                metadata.TopologyType = ArrowTopologyType.MultiEndpointSharedPath;
            }
        }

        private Vector2Int GetOtherEndpointPosition(EditorArrowDraft draft, Vector2Int primaryPosition)
        {
            for (int i = 0; i < draft.EndpointCandidates.Count; i++)
            {
                Vector2Int candidate = draft.EndpointCandidates[i];
                if (candidate != primaryPosition)
                {
                    return candidate;
                }
            }

            return primaryPosition;
        }

        private void ApplyDraftVisuals(EditorArrowDraft draft, EditorArrowMetadata metadata)
        {
            ArrowSaveData saveData = BuildArrowSaveData(draft, metadata);
            if (saveData == null) return;

            if (!ArrowModelFactory.TryCreate(saveData, out ArrowModel arrowModel, out _))
            {
                return;
            }

            List<CellType> cellTypes = ArrowCellTypeBuilder.BuildLegacyCellTypes(arrowModel);
            for (int i = 0; i < draft.Path.Count && i < cellTypes.Count; i++)
            {
                Vector2Int pos = draft.Path[i];
                tempGrid[pos.x, pos.y].id = draft.Id;
                tempGrid[pos.x, pos.y].type = cellTypes[i];
            }
        }

        private ArrowSaveData BuildArrowSaveData(EditorArrowDraft draft, EditorArrowMetadata metadata)
        {
            if (draft == null || !draft.IsValid || draft.Path == null || draft.Path.Count == 0) return null;

            Vector2Int primaryPos = metadata.PrimaryEndpointPosition;
            int primaryIndex = draft.Path.IndexOf(primaryPos);
            if (primaryIndex < 0) primaryIndex = 0;

            List<ArrowEndpointSaveData> endpoints = new List<ArrowEndpointSaveData>
            {
                new ArrowEndpointSaveData(primaryIndex, BuildEndpointDirection(draft.Path, primaryIndex), true)
            };

            if (metadata.HasSecondaryEndpoint && draft.EndpointCandidates.Count > 1)
            {
                Vector2Int secondaryPos = GetOtherEndpointPosition(draft, primaryPos);
                int secondaryIndex = draft.Path.IndexOf(secondaryPos);
                if (secondaryIndex >= 0 && secondaryIndex != primaryIndex)
                {
                    endpoints.Add(new ArrowEndpointSaveData(secondaryIndex,
                        BuildEndpointDirection(draft.Path, secondaryIndex), false));
                }
            }

            ArrowTopologyType topologyType = metadata.TopologyType;
            if (endpoints.Count > 1)
            {
                topologyType = ArrowTopologyType.MultiEndpointSharedPath;
            }

            return new ArrowSaveData(draft.Id, draft.Path, primaryIndex == 0, endpoints,
                (metadata.LinkGroupId ?? string.Empty).Trim(), topologyType);
        }

        private static Direction4 BuildEndpointDirection(IReadOnlyList<Vector2Int> path, int pathIndex)
        {
            if (path == null || path.Count <= 1) return Direction4.Up;

            Vector2Int current = path[pathIndex];
            int neighborIndex = pathIndex == 0 ? 1 : path.Count - 2;
            Vector2Int direction = current - path[neighborIndex];
            return Direction4Extensions.FromVector(direction);
        }

        private bool ValidateGridLogic(out List<string> errors)
        {
            errors = new List<string>();
            RebuildArrowDraftsAndVisuals();

            foreach (EditorArrowDraft draft in _arrowDrafts.Values.OrderBy(d => GetSortableId(d.Id)))
            {
                if (!draft.IsValid)
                {
                    errors.Add(draft.ValidationError);
                    continue;
                }

                EditorArrowMetadata metadata = GetOrCreateMetadata(draft);
                int endpointCount = 1 + (metadata.HasSecondaryEndpoint ? 1 : 0);
                if (metadata.TopologyType == ArrowTopologyType.MultiEndpointSharedPath && endpointCount != 2)
                {
                    errors.Add($"Arrow {draft.Id} topology MultiEndpointSharedPath phải có đúng 2 endpoints.");
                }

                if (metadata.TopologyType == ArrowTopologyType.SingleHeadSingleTail && endpointCount != 1)
                {
                    errors.Add($"Arrow {draft.Id} topology SingleHeadSingleTail phải có đúng 1 endpoint.");
                }

                ArrowSaveData arrowSaveData = BuildArrowSaveData(draft, metadata);
                if (arrowSaveData == null)
                {
                    errors.Add($"Arrow {draft.Id} không dựng được save data.");
                    continue;
                }

                if (!ArrowModelFactory.TryCreate(arrowSaveData, out _, out string error))
                {
                    errors.Add(error);
                }
            }

            return errors.Count == 0;
        }

        private void ResetEmptyCells()
        {
            for (int x = 0; x < currentLevel.width; x++)
            {
                for (int y = 0; y < currentLevel.height; y++)
                {
                    if (string.IsNullOrEmpty(tempGrid[x, y].id))
                    {
                        tempGrid[x, y].type = CellType.EmptyDot;
                    }
                }
            }
        }

        private bool GridContainsArrow(string arrowId)
        {
            for (int x = 0; x < currentLevel.width; x++)
            {
                for (int y = 0; y < currentLevel.height; y++)
                {
                    if (tempGrid[x, y].id == arrowId) return true;
                }
            }

            return false;
        }

        private bool IsWithinEditorBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < currentLevel.width && pos.y >= 0 && pos.y < currentLevel.height;
        }

        private static bool IsValidPathIndex(int pathIndex, int pathCount)
        {
            return pathIndex >= 0 && pathIndex < pathCount;
        }

        private int GetEndpointIndex(List<Vector2Int> endpointCandidates, Vector2Int endpointPosition)
        {
            int index = endpointCandidates.IndexOf(endpointPosition);
            return index >= 0 ? index : 0;
        }

        private string GetNextAvailableID()
        {
            int maxId = 0;
            for (int x = 0; x < currentLevel.width; x++)
            {
                for (int y = 0; y < currentLevel.height; y++)
                {
                    if (int.TryParse(tempGrid[x, y].id, out int id) && id > maxId)
                    {
                        maxId = id;
                    }
                }
            }

            return (maxId + 1).ToString();
        }

        private string GetAdjacentArrowID(int x, int y)
        {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < dirs.Length; i++)
            {
                int nx = x + dirs[i].x;
                int ny = y + dirs[i].y;
                if (nx >= 0 && nx < currentLevel.width && ny >= 0 && ny < currentLevel.height)
                {
                    if (!string.IsNullOrEmpty(tempGrid[nx, ny].id)) return tempGrid[nx, ny].id;
                }
            }

            return string.Empty;
        }

        private void HandleHotkeys()
        {
            Event currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (EditorGUIUtility.editingTextField &&
                currentEvent.keyCode != KeyCode.Escape &&
                currentEvent.keyCode != KeyCode.KeypadEnter &&
                currentEvent.keyCode != KeyCode.Return)
            {
                return;
            }

            bool consumed = true;
            switch (currentEvent.keyCode)
            {
                case KeyCode.Alpha5:
                case KeyCode.Keypad5:
                    SetToolMode(EditorToolMode.SelectArrow);
                    break;
                case KeyCode.Alpha6:
                case KeyCode.Keypad6:
                    SetToolMode(EditorToolMode.LinkArrow);
                    break;
                case KeyCode.Escape:
                    SetToolMode(EditorToolMode.Paint);
                    break;
                case KeyCode.Q:
                    SelectAdjacentArrow(-1);
                    break;
                case KeyCode.E:
                    SelectAdjacentArrow(1);
                    break;
                case KeyCode.R:
                    SwapPrimaryEndpointForSelectedArrow();
                    break;
                case KeyCode.T:
                    ToggleSecondaryEndpointForSelectedArrow();
                    break;
                case KeyCode.G:
                    AssignNewLinkGroupToSelectedArrow();
                    break;
                case KeyCode.Backspace:
                case KeyCode.Delete:
                    ClearLinkGroupForSelectedArrow();
                    break;
                default:
                    consumed = false;
                    break;
            }

            if (consumed)
            {
                currentEvent.Use();
                Repaint();
            }
        }

        private void SetToolMode(EditorToolMode mode)
        {
            _toolMode = mode;
        }

        private static string GetToolModeLabel(EditorToolMode mode)
        {
            return mode switch
            {
                EditorToolMode.Paint => "Paint",
                EditorToolMode.SelectArrow => "Select/Edit",
                EditorToolMode.LinkArrow => "Link Arrow",
                _ => "Unknown"
            };
        }

        private bool IsSelectedArrow(string arrowId)
        {
            return !string.IsNullOrEmpty(arrowId) && arrowId == _selectedArrowId;
        }

        private void SelectArrow(string arrowId)
        {
            if (string.IsNullOrEmpty(arrowId) || !_arrowDrafts.ContainsKey(arrowId))
            {
                return;
            }

            _selectedArrowId = arrowId;
            if (_toolMode == EditorToolMode.Paint)
            {
                _toolMode = EditorToolMode.SelectArrow;
            }
        }

        private void SelectAdjacentArrow(int direction)
        {
            List<string> ids = _arrowDrafts.Keys.OrderBy(GetSortableId).ThenBy(id => id).ToList();
            if (ids.Count == 0)
            {
                _selectedArrowId = string.Empty;
                return;
            }

            if (string.IsNullOrEmpty(_selectedArrowId))
            {
                _selectedArrowId = ids[0];
                return;
            }

            int currentIndex = ids.IndexOf(_selectedArrowId);
            if (currentIndex < 0)
            {
                _selectedArrowId = ids[0];
                return;
            }

            int nextIndex = (currentIndex + direction + ids.Count) % ids.Count;
            _selectedArrowId = ids[nextIndex];
        }

        private void SwapPrimaryEndpointForSelectedArrow()
        {
            EditorArrowDraft draft = GetSelectedDraft();
            if (draft == null || !draft.IsValid || draft.EndpointCandidates.Count <= 1) return;

            EditorArrowMetadata metadata = GetOrCreateMetadata(draft);
            Vector2Int oldPrimary = metadata.PrimaryEndpointPosition;
            metadata.PrimaryEndpointPosition = GetOtherEndpointPosition(draft, oldPrimary);
            if (metadata.HasSecondaryEndpoint)
            {
                metadata.SecondaryEndpointPosition = oldPrimary;
            }

            RebuildArrowDraftsAndVisuals();
        }

        private void ToggleSecondaryEndpointForSelectedArrow()
        {
            EditorArrowDraft draft = GetSelectedDraft();
            if (draft == null || !draft.IsValid || draft.EndpointCandidates.Count <= 1) return;

            EditorArrowMetadata metadata = GetOrCreateMetadata(draft);
            metadata.HasSecondaryEndpoint = !metadata.HasSecondaryEndpoint;
            metadata.SecondaryEndpointPosition = metadata.HasSecondaryEndpoint
                ? GetOtherEndpointPosition(draft, metadata.PrimaryEndpointPosition)
                : InvalidPosition;
            RebuildArrowDraftsAndVisuals();
        }

        private void AssignNewLinkGroupToSelectedArrow()
        {
            EditorArrowDraft draft = GetSelectedDraft();
            if (draft == null) return;

            EditorArrowMetadata metadata = GetOrCreateMetadata(draft);
            metadata.LinkGroupId = GenerateNextLinkGroupId();
            Repaint();
        }

        private void ClearLinkGroupForSelectedArrow()
        {
            EditorArrowDraft draft = GetSelectedDraft();
            if (draft == null) return;

            EditorArrowMetadata metadata = GetOrCreateMetadata(draft);
            metadata.LinkGroupId = string.Empty;
            Repaint();
        }

        private void ToggleArrowLinkMembership(string baseArrowId, string targetArrowId)
        {
            if (!_arrowDrafts.TryGetValue(baseArrowId, out EditorArrowDraft baseDraft) ||
                !_arrowDrafts.TryGetValue(targetArrowId, out EditorArrowDraft targetDraft))
            {
                return;
            }

            EditorArrowMetadata baseMetadata = GetOrCreateMetadata(baseDraft);
            EditorArrowMetadata targetMetadata = GetOrCreateMetadata(targetDraft);

            string linkGroupId = (baseMetadata.LinkGroupId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(linkGroupId))
            {
                linkGroupId = GenerateNextLinkGroupId();
                baseMetadata.LinkGroupId = linkGroupId;
            }

            if (string.Equals((targetMetadata.LinkGroupId ?? string.Empty).Trim(), linkGroupId, StringComparison.Ordinal))
            {
                targetMetadata.LinkGroupId = string.Empty;
            }
            else
            {
                targetMetadata.LinkGroupId = linkGroupId;
            }

            SelectArrow(targetArrowId);
            Repaint();
        }

        private string GenerateNextLinkGroupId()
        {
            HashSet<string> used = new HashSet<string>(
                _arrowMetadata.Values
                    .Where(metadata => metadata != null && !string.IsNullOrWhiteSpace(metadata.LinkGroupId))
                    .Select(metadata => metadata.LinkGroupId.Trim()));

            int index = 1;
            while (used.Contains($"Link_{index}"))
            {
                index++;
            }

            return $"Link_{index}";
        }

        private int CountArrowsInLinkGroup(string linkGroupId)
        {
            if (string.IsNullOrWhiteSpace(linkGroupId)) return 0;

            int count = 0;
            foreach (EditorArrowMetadata metadata in _arrowMetadata.Values)
            {
                if (metadata != null && string.Equals((metadata.LinkGroupId ?? string.Empty).Trim(),
                        linkGroupId.Trim(), StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private string GetLabelForType(CellType type, string id)
        {
            string dID = string.IsNullOrEmpty(id) ? string.Empty : $" \n({id})";
            return type switch
            {
                CellType.None => string.Empty,
                CellType.EmptyDot => "•",
                CellType.ArrowHeadUp => $"↑{dID}",
                CellType.ArrowHeadDown => $"↓{dID}",
                CellType.ArrowHeadLeft => $"←{dID}",
                CellType.ArrowHeadRight => $"→{dID}",
                CellType.ArrowTailUp => $"╨{dID}",
                CellType.ArrowTailDown => $"╥{dID}",
                CellType.ArrowTailLeft => $"╡{dID}",
                CellType.ArrowTailRight => $"╞{dID}",
                CellType.ArrowBodyHorizontal => $"═{dID}",
                CellType.ArrowBodyVertical => $"║{dID}",
                CellType.ArrowCurveTopRight => $"╚{dID}",
                CellType.ArrowCurveTopLeft => $"╝{dID}",
                CellType.ArrowCurveBottomRight => $"╔{dID}",
                CellType.ArrowCurveBottomLeft => $"╗{dID}",
                _ => string.Empty
            };
        }

        private Color GetColorForType(CellType type)
        {
            if (ArrowCellTypeBuilder.IsHeadType(type)) return new Color(0.3f, 0.9f, 0.3f);
            if (type == CellType.None) return new Color(0.2f, 0.2f, 0.2f);
            if (type == CellType.EmptyDot) return Color.gray;
            return new Color(0.6f, 0.8f, 1f);
        }
    }
}
#endif
