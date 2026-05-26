using System;
using System.Collections.Generic;
using DG.Tweening;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.System;
using EditorTool.Scripts.UI.Panels;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Controller
{
    [DefaultExecutionOrder(-50)]
    public class EditorController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private InputController inputController;

        [Header("UI Panels")]
        [SerializeField] private DrawingToolPanel drawingToolPanel;
        [SerializeField] private SearchPanel searchPanel;
        [SerializeField] private MapSettingsPanel mapSettingsPanel;
        [SerializeField] private MechanicDrawerPanel mechanicDrawerPanel;

        [Header("Panel Roots")]
        [SerializeField] private GameObject leftPanelRoot;
        [SerializeField] private GameObject rightPanelRoot;

        private int _currentArrowId = 1;
        private bool _isLeftOpen = true;
        private bool _isRightOpen = true;
        private Vector2 _leftStartPos;
        private Vector2 _rightStartPos;
        private RectTransform _leftRect;
        private RectTransform _rightRect;
        private string _linkAnchorArrowId = string.Empty;
        private bool _savedArrowDrawHeadFirst = false;
        private readonly Stack<EditorUndoState> _undoStack = new Stack<EditorUndoState>();
        private const int MaxUndoSteps = 50;

        private sealed class EditorUndoState
        {
            public LevelSaveData LevelState;
            public string CurrentArrowId;
            public bool DrawHeadFirst;
            public bool SavedDrawHeadFirst;
            public EditorBrushMode BrushMode;
            public CellType CurrentBrush;
            public bool IsSelectMode;
            public BoardSpecialType SpecialType;
            public Direction4 SpecialDirection;
            public string PortalId;
            public int CounterBlockValue;
            public EditorArrowMechanicMode ArrowMechanicMode;
        }

        private void Start()
        {
            SetArrowDrawDirection(false);
            AutoDiscoverPanels();
            InitializePanelPositions();
            EnsureMechanicDrawerPanel();
            WireInputController();
            WireDrawingToolPanel();
            WireSearchPanel();
            WireMapSettingsPanel();
            WireMechanicDrawerPanel();

            drawingToolPanel.Initialize();
            searchPanel.Initialize();
            mapSettingsPanel.Initialize();
            mechanicDrawerPanel?.Initialize();

            RefreshToolingStatus();
        }

        private void WireInputController()
        {
            inputController.OnNewArrowHotkey = HandleNewArrow;
            inputController.OnSwapHotkey = HandleSwap;
            inputController.OnEraseHotkey = HandleErase;
            inputController.OnSelectHotkey = HandleToggleSelect;
            inputController.OnArrowSelectedFromMap = HandleArrowSelected;
            inputController.OnArrowModeHotkey = HandleArrowMode;
            inputController.OnPortalBrushHotkey = HandlePortalBrush;
            inputController.OnRedirectBrushHotkey = HandleRedirectBrush;
            inputController.OnCounterBlockBrushHotkey = HandleCounterBlockBrush;
            inputController.OnTwoHeadModeHotkey = HandleTwoHeadMode;
            inputController.OnLinkModeHotkey = HandleLinkMode;
            inputController.OnCancelArrowMechanicModeHotkey = HandleCancelArrowMechanicMode;
            inputController.OnArrowTwoHeadClicked = HandleArrowTwoHeadClicked;
            inputController.OnArrowLinkClicked = HandleArrowLinkClicked;
            inputController.OnRotateDirectionHotkey = HandleRotateSpecialDirection;
            inputController.OnCyclePortalIdHotkey = HandleCyclePortalId;
            inputController.OnSelectPreviousArrowHotkey = () => CycleArrowSelection(-1);
            inputController.OnSelectNextArrowHotkey = () => CycleArrowSelection(1);
            inputController.OnToggleSelectedTwoHeadHotkey = HandleToggleSelectedTwoHead;
            inputController.OnCreateLinkGroupHotkey = HandleCreateLinkGroup;
            inputController.OnClearLinkGroupHotkey = HandleClearLinkGroup;
            inputController.OnUndoHotkey = HandleUndo;
            inputController.OnDirectionHotkey = HandleSpecialDirectionChanged;
            inputController.OnToggleLeftPanelHotkey = HandleToggleLeftPanel;
            inputController.OnToggleRightPanelHotkey = HandleToggleRightPanel;
            inputController.OnSpecialSelectedFromMap = HandleSpecialSelected;
            inputController.OnSpecialCellPlaced = HandleSpecialCellPlaced;
            inputController.OnSpecialCellRemoved = RefreshToolingStatus;
            inputController.OnBeginMutation = PushUndoState;
        }

        private void WireDrawingToolPanel()
        {
            drawingToolPanel.OnNewArrow = HandleNewArrow;
            drawingToolPanel.OnSwap = HandleSwap;
            drawingToolPanel.OnErase = HandleErase;
            drawingToolPanel.OnSelect = HandleToggleSelect;
            drawingToolPanel.OnResetMap = HandleResetMap;
            drawingToolPanel.OnSaveMap = HandleSaveMap;
            drawingToolPanel.OnArrowSelectedFromList = HandleArrowSelected;
        }

        private void WireSearchPanel()
        {
            searchPanel.OnCheckUnsavedChanges = CheckUnsavedChanges;
            searchPanel.OnDataPreviewLoaded = HandleDataPreview;
            searchPanel.OnLoadButtonClicked = HandleLoadLevel;
        }

        private void WireMapSettingsPanel()
        {
            mapSettingsPanel.OnMapResized = HandleMapResized;
        }

        private void WireMechanicDrawerPanel()
        {
            if (mechanicDrawerPanel == null) return;

            mechanicDrawerPanel.OnArrowMode = HandleArrowMode;
            mechanicDrawerPanel.OnPortalBrush = HandlePortalBrush;
            mechanicDrawerPanel.OnRedirectBrush = HandleRedirectBrush;
            mechanicDrawerPanel.OnCounterBlockBrush = HandleCounterBlockBrush;
            mechanicDrawerPanel.OnTwoHeadMode = HandleTwoHeadMode;
            mechanicDrawerPanel.OnLinkMode = HandleLinkMode;
            mechanicDrawerPanel.OnDirectionChanged = HandleSpecialDirectionChanged;
            mechanicDrawerPanel.OnPortalIdChanged = HandlePortalIdChanged;
            mechanicDrawerPanel.OnCounterChanged = HandleCounterChanged;
            mechanicDrawerPanel.OnMechanicSelectedFromList = HandleMechanicSelected;
        }

        private void EnsureMechanicDrawerPanel()
        {
            if (mechanicDrawerPanel != null) return;

            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                Transform leftPanel = canvas.transform.Find("LeftPanel");
                if (leftPanel == null) continue;

                leftPanel.gameObject.SetActive(true);
                mechanicDrawerPanel = leftPanel.GetComponent<MechanicDrawerPanel>();
                if (mechanicDrawerPanel == null)
                {
                    mechanicDrawerPanel = leftPanel.gameObject.AddComponent<MechanicDrawerPanel>();
                }

                return;
            }
        }

        private void HandleArrowSelected(string arrowID)
        {
            SelectArrow(arrowID, preserveMechanicMode: inputController.arrowMechanicMode != EditorArrowMechanicMode.None);
        }

        private void PushUndoState()
        {
            LevelMakerManager manager = LevelMakerManager.Instance;
            if (manager == null || manager.GridSystem == null) return;

            if (_undoStack.Count >= MaxUndoSteps)
            {
                EditorUndoState[] existing = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = existing.Length - 2; i >= 0; i--)
                {
                    _undoStack.Push(existing[i]);
                }
            }

            _undoStack.Push(new EditorUndoState
            {
                LevelState = manager.CaptureEditorState(),
                CurrentArrowId = inputController.currentArrowID,
                DrawHeadFirst = inputController.drawHeadFirst,
                SavedDrawHeadFirst = _savedArrowDrawHeadFirst,
                BrushMode = inputController.brushMode,
                CurrentBrush = inputController.currentBrush,
                IsSelectMode = inputController.isSelectMode,
                SpecialType = inputController.currentSpecialType,
                SpecialDirection = inputController.currentSpecialDirection,
                PortalId = inputController.currentPortalId,
                CounterBlockValue = inputController.currentCounterBlockValue,
                ArrowMechanicMode = inputController.arrowMechanicMode
            });
        }

        private void HandleUndo()
        {
            if (_undoStack.Count == 0)
            {
                Debug.Log("<color=yellow>[Undo] Không còn thao tác nào để hoàn tác.</color>");
                return;
            }

            RestoreUndoState(_undoStack.Pop());
        }

        private void RestoreUndoState(EditorUndoState state)
        {
            if (state?.LevelState == null) return;

            LevelMakerManager.Instance.RestoreEditorState(state.LevelState);
            searchPanel.SetSearchText(LevelMakerManager.Instance.currentLevelID);
            mapSettingsPanel.Refresh();
            drawingToolPanel.RefreshArrowList();
            searchPanel.ScanSavedLevels();

            inputController.currentArrowID = string.IsNullOrWhiteSpace(state.CurrentArrowId) ? "1" : state.CurrentArrowId;
            inputController.brushMode = state.BrushMode;
            inputController.currentBrush = state.CurrentBrush;
            inputController.isSelectMode = state.IsSelectMode;
            inputController.currentSpecialType = state.SpecialType;
            inputController.currentSpecialDirection = state.SpecialDirection;
            inputController.currentPortalId = string.IsNullOrWhiteSpace(state.PortalId) ? "A" : state.PortalId;
            inputController.currentCounterBlockValue = Mathf.Max(1, state.CounterBlockValue);
            inputController.arrowMechanicMode = state.ArrowMechanicMode;
            _savedArrowDrawHeadFirst = state.SavedDrawHeadFirst;
            inputController.drawHeadFirst = state.DrawHeadFirst;
            _linkAnchorArrowId = inputController.arrowMechanicMode == EditorArrowMechanicMode.Link
                ? inputController.currentArrowID
                : string.Empty;

            List<string> arrowIds = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();
            if (arrowIds.Count > 0 && !arrowIds.Contains(inputController.currentArrowID))
            {
                arrowIds.Sort(CompareArrowIds);
                inputController.currentArrowID = arrowIds[arrowIds.Count - 1];
            }

            RefreshToolingStatus();
        }

        private void SelectArrow(string arrowID, bool preserveMechanicMode)
        {
            if (string.IsNullOrWhiteSpace(arrowID)) return;

            inputController.currentArrowID = arrowID;
            inputController.currentBrush = CellType.ArrowBodyVertical;
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.isSelectMode = false;

            bool arrowExists = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs().Contains(arrowID);
            if (arrowExists && preserveMechanicMode)
            {
                SetArrowDrawDirection(LevelMakerManager.Instance.GridSystem.IsHeadFirst(arrowID), savePreference: false);
            }
            else
            {
                RestoreSavedArrowDrawDirection();
            }

            if (int.TryParse(arrowID, out int parsed))
            {
                _currentArrowId = parsed;
            }

            if (!preserveMechanicMode)
            {
                inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
                _linkAnchorArrowId = string.Empty;
            }
            else if (inputController.arrowMechanicMode == EditorArrowMechanicMode.Link)
            {
                _linkAnchorArrowId = arrowID;
            }

            RefreshToolingStatus();
            LevelMakerManager.Instance.GridView.PlayArrowBounce(arrowID);
        }

        private void HandleNewArrow()
        {
            string newID = GetNextAvailableArrowID();
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            SelectArrow(newID, preserveMechanicMode: false);
        }

        private void HandleSwap()
        {
            if (string.IsNullOrWhiteSpace(inputController.currentArrowID)) return;
            PushUndoState();
            LevelMakerManager.Instance.GridSystem.FlipArrowPath(inputController.currentArrowID);
            SetArrowDrawDirection(LevelMakerManager.Instance.GridSystem.IsHeadFirst(inputController.currentArrowID));
            RefreshToolingStatus();
        }

        private void HandleErase()
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.currentBrush = CellType.EmptyDot;
            inputController.isSelectMode = false;
            RefreshToolingStatus();
        }

        private void HandleToggleSelect()
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.isSelectMode = !inputController.isSelectMode;
            RefreshToolingStatus();
        }

        private void HandleArrowMode()
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.currentBrush = CellType.ArrowBodyVertical;
            inputController.isSelectMode = false;
            RestoreSavedArrowDrawDirection();
            RefreshToolingStatus();
        }

        private void HandleTwoHeadMode()
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.TwoHead;
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.currentBrush = CellType.ArrowBodyVertical;
            inputController.isSelectMode = false;
            RefreshToolingStatus();
        }

        private void HandleLinkMode()
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.Link;
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.currentBrush = CellType.ArrowBodyVertical;
            inputController.isSelectMode = false;
            _linkAnchorArrowId = string.IsNullOrWhiteSpace(inputController.currentArrowID)
                ? string.Empty
                : inputController.currentArrowID;
            RefreshToolingStatus();
        }

        private void HandleCancelArrowMechanicMode()
        {
            HandleArrowMode();
        }

        private void HandleArrowTwoHeadClicked(string arrowId)
        {
            PushUndoState();
            SelectArrow(arrowId, preserveMechanicMode: true);
            if (LevelMakerManager.Instance.GridSystem.ToggleTwoHead(arrowId))
            {
                RefreshSelectedArrowFromGrid();
                LevelMakerManager.Instance.GridView.PlayArrowBounce(arrowId);
            }
        }

        private void HandleArrowLinkClicked(string arrowId, bool isCtrlPressed)
        {
            if (!isCtrlPressed)
            {
                _linkAnchorArrowId = arrowId;
                SelectArrow(arrowId, preserveMechanicMode: true);
                return;
            }

            if (string.IsNullOrWhiteSpace(_linkAnchorArrowId))
            {
                _linkAnchorArrowId = arrowId;
                SelectArrow(arrowId, preserveMechanicMode: true);
                return;
            }

            if (_linkAnchorArrowId == arrowId)
            {
                SelectArrow(arrowId, preserveMechanicMode: true);
                return;
            }

            EditorArrowMetadataData anchorMetadata =
                LevelMakerManager.Instance.GridSystem.GetArrowMetadata(_linkAnchorArrowId);
            if (anchorMetadata == null)
            {
                _linkAnchorArrowId = arrowId;
                SelectArrow(arrowId, preserveMechanicMode: true);
                return;
            }

            string groupId = string.IsNullOrWhiteSpace(anchorMetadata.LinkGroupId)
                ? CreateNewLinkGroupId()
                : anchorMetadata.LinkGroupId;

            PushUndoState();
            if (string.IsNullOrWhiteSpace(anchorMetadata.LinkGroupId))
            {
                LevelMakerManager.Instance.GridSystem.SetLinkGroup(_linkAnchorArrowId, groupId);
            }

            EditorArrowMetadataData targetMetadata = LevelMakerManager.Instance.GridSystem.GetArrowMetadata(arrowId);
            if (targetMetadata == null) return;

            if (string.Equals(targetMetadata.LinkGroupId, groupId, StringComparison.Ordinal))
            {
                LevelMakerManager.Instance.GridSystem.ClearLinkGroup(arrowId);
            }
            else
            {
                LevelMakerManager.Instance.GridSystem.SetLinkGroup(arrowId, groupId);
            }

            RefreshToolingStatus();
            LevelMakerManager.Instance.GridView.PlayArrowBounce(_linkAnchorArrowId);
            LevelMakerManager.Instance.GridView.PlayArrowBounce(arrowId);
        }

        private void HandleToggleSelectedTwoHead()
        {
            if (string.IsNullOrWhiteSpace(inputController.currentArrowID)) return;
            PushUndoState();
            if (LevelMakerManager.Instance.GridSystem.ToggleTwoHead(inputController.currentArrowID))
            {
                RefreshSelectedArrowFromGrid();
                LevelMakerManager.Instance.GridView.PlayArrowBounce(inputController.currentArrowID);
            }
        }

        private void HandleCreateLinkGroup()
        {
            if (string.IsNullOrWhiteSpace(inputController.currentArrowID)) return;
            PushUndoState();
            string newGroupId = CreateNewLinkGroupId();
            LevelMakerManager.Instance.GridSystem.SetLinkGroup(inputController.currentArrowID, newGroupId);
            _linkAnchorArrowId = inputController.currentArrowID;
            RefreshToolingStatus();
        }

        private void HandleClearLinkGroup()
        {
            if (string.IsNullOrWhiteSpace(inputController.currentArrowID)) return;
            PushUndoState();
            LevelMakerManager.Instance.GridSystem.ClearLinkGroup(inputController.currentArrowID);
            RefreshToolingStatus();
        }

        private void HandlePortalBrush()
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            inputController.brushMode = EditorBrushMode.Special;
            inputController.currentSpecialType = BoardSpecialType.Portal;
            inputController.isSelectMode = false;
            AutoSelectPortalId();
            RefreshToolingStatus();
        }

        private void HandleRedirectBrush()
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            inputController.brushMode = EditorBrushMode.Special;
            inputController.currentSpecialType = BoardSpecialType.Redirect;
            inputController.isSelectMode = false;
            AutoSelectRedirectId();
            RefreshToolingStatus();
        }

        private void HandleCounterBlockBrush()
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            inputController.brushMode = EditorBrushMode.Special;
            inputController.currentSpecialType = BoardSpecialType.CounterBlock;
            inputController.isSelectMode = false;
            if (inputController.currentCounterBlockValue <= 0)
            {
                inputController.currentCounterBlockValue = 1;
            }

            RefreshToolingStatus();
        }

        private void HandleRotateSpecialDirection()
        {
            Direction4 nextDirection = inputController.currentSpecialDirection switch
            {
                Direction4.Up => Direction4.Right,
                Direction4.Right => Direction4.Down,
                Direction4.Down => Direction4.Left,
                _ => Direction4.Up
            };

            HandleSpecialDirectionChanged(nextDirection);
        }

        private void HandleSpecialDirectionChanged(Direction4 direction)
        {
            inputController.currentSpecialDirection = direction;
            RefreshToolingStatus();
        }

        private void HandleCyclePortalId()
        {
            string portalId = string.IsNullOrEmpty(inputController.currentPortalId)
                ? "A"
                : inputController.currentPortalId.Trim().ToUpperInvariant();
            char next = portalId[0] >= 'A' && portalId[0] < 'Z' ? (char)(portalId[0] + 1) : 'A';
            HandlePortalIdChanged(next.ToString());
        }

        private void HandlePortalIdChanged(string portalId)
        {
            inputController.currentPortalId = string.IsNullOrWhiteSpace(portalId)
                ? "A"
                : portalId.Trim().ToUpperInvariant();
            RefreshToolingStatus();
        }

        private void HandleCounterChanged(int counter)
        {
            inputController.currentCounterBlockValue = Mathf.Max(1, counter);
            RefreshToolingStatus();
        }

        private void HandleSpecialSelected(SpecialCellSaveData data)
        {
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            inputController.brushMode = EditorBrushMode.Special;
            inputController.currentSpecialType = data.Type;
            inputController.currentSpecialDirection = data.Type == BoardSpecialType.Portal
                ? data.PortalDirection
                : data.ExitDirection;
            if (data.Type == BoardSpecialType.CounterBlock)
            {
                inputController.currentCounterBlockValue = Mathf.Max(1, data.Counter);
            }
            else
            {
                inputController.currentPortalId = data.PortalId;
            }

            inputController.isSelectMode = false;
            RefreshToolingStatus();
        }

        private void HandleMechanicSelected(string idOrPos)
        {
            if (inputController.arrowMechanicMode != EditorArrowMechanicMode.None &&
                LevelMakerManager.Instance.GridSystem.GetAllArrowIDs().Contains(idOrPos))
            {
                if (inputController.arrowMechanicMode == EditorArrowMechanicMode.TwoHead)
                {
                    HandleArrowTwoHeadClicked(idOrPos);
                }
                else
                {
                    _linkAnchorArrowId = idOrPos;
                    SelectArrow(idOrPos, preserveMechanicMode: true);
                }

                return;
            }

            List<SpecialCellSaveData> specialCells = LevelMakerManager.Instance.GridSystem.GetSpecialSaveData();
            BoardSpecialType currentType = inputController.currentSpecialType;

            foreach (SpecialCellSaveData cell in specialCells)
            {
                string posStr = $"{cell.Position.x},{cell.Position.y}";
                bool isMatch = currentType == BoardSpecialType.CounterBlock
                    ? cell.Id == idOrPos || posStr == idOrPos
                    : cell.PortalId == idOrPos || posStr == idOrPos;

                if (!isMatch || cell.Type != currentType) continue;

                HandleSpecialSelected(cell);
                LevelMakerManager.Instance.GridView.PlaySpecialCellBounce(cell.Position);
                return;
            }
        }

        private void HandleSpecialCellPlaced()
        {
            if (inputController.currentSpecialType == BoardSpecialType.Portal)
            {
                AutoSelectPortalId();
            }
            else if (inputController.currentSpecialType == BoardSpecialType.Redirect)
            {
                AutoSelectRedirectId();
            }

            RefreshToolingStatus();
        }

        private void AutoSelectPortalId()
        {
            List<SpecialCellSaveData> specialCells = LevelMakerManager.Instance.GridSystem.GetSpecialSaveData();
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (SpecialCellSaveData cell in specialCells)
            {
                if (cell.Type == BoardSpecialType.Portal && !string.IsNullOrEmpty(cell.PortalId))
                {
                    counts[cell.PortalId] = counts.ContainsKey(cell.PortalId) ? counts[cell.PortalId] + 1 : 1;
                }
            }

            foreach (KeyValuePair<string, int> kvp in counts)
            {
                if (kvp.Value == 1)
                {
                    inputController.currentPortalId = kvp.Key;
                    return;
                }
            }

            for (char c = 'A'; c <= 'Z'; c++)
            {
                string id = c.ToString();
                if (!counts.ContainsKey(id))
                {
                    inputController.currentPortalId = id;
                    return;
                }
            }
        }

        private void AutoSelectRedirectId()
        {
            List<SpecialCellSaveData> specialCells = LevelMakerManager.Instance.GridSystem.GetSpecialSaveData();
            HashSet<int> usedIds = new HashSet<int>();
            foreach (SpecialCellSaveData cell in specialCells)
            {
                if (cell.Type == BoardSpecialType.Redirect && int.TryParse(cell.PortalId, out int id))
                {
                    usedIds.Add(id);
                }
            }

            int nextId = 1;
            while (usedIds.Contains(nextId)) nextId++;
            inputController.currentPortalId = nextId.ToString();
        }

        private void HandleResetMap()
        {
            if (!CheckUnsavedChanges()) return;

            PushUndoState();
            LevelMakerManager.Instance.ClearMap();
            LevelMakerManager.Instance.IsDirty = false;
            drawingToolPanel.RefreshArrowList();

            _currentArrowId = 1;
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            SelectArrow("1", preserveMechanicMode: false);
        }

        private void HandleSaveMap()
        {
            string levelID = LevelMakerManager.Instance.currentLevelID;

#if UNITY_EDITOR
            bool confirm = UnityEditor.EditorUtility.DisplayDialog("Xác nhận lưu Map", $"Lưu Level: \"{levelID}\"?",
                "Lưu ngay!", "Hủy");
            if (!confirm) return;
#endif
            if (LevelMakerManager.Instance.ExportLevel())
            {
                searchPanel.ScanSavedLevels();
            }
        }

        private void HandleDataPreview(LevelSaveData previewData)
        {
            mapSettingsPanel.RefreshFromPreview(previewData);
        }

        private void HandleLoadLevel()
        {
            if (!CheckUnsavedChanges()) return;

            string levelID = searchPanel.CurrentSearchText;
            if (string.IsNullOrEmpty(levelID)) return;

            PushUndoState();
            LevelMakerManager.Instance.LoadOrCreateLevel(levelID, mapSettingsPanel.Width, mapSettingsPanel.Height);
            LevelMakerManager.Instance.IsDirty = false;

            mapSettingsPanel.Refresh();
            drawingToolPanel.RefreshArrowList();
            drawingToolPanel.AutoSelectLastArrow();
            searchPanel.ScanSavedLevels();
            inputController.arrowMechanicMode = EditorArrowMechanicMode.None;
            _linkAnchorArrowId = string.Empty;
            RefreshToolingStatus();
        }

        private void HandleMapResized()
        {
            if (LevelMakerManager.Instance.GridSystem.Width == mapSettingsPanel.Width &&
                LevelMakerManager.Instance.GridSystem.Height == mapSettingsPanel.Height)
            {
                return;
            }

            PushUndoState();
            LevelMakerManager.Instance.ResizeGrid(mapSettingsPanel.Width, mapSettingsPanel.Height);
        }

        private bool CheckUnsavedChanges()
        {
            if (!LevelMakerManager.Instance.IsDirty) return true;
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.DisplayDialog("Cảnh báo", "Map chưa lưu. Bạn có muốn bỏ không?", "Bỏ",
                "Hủy");
#else
            return true;
#endif
        }

        private string GetNextAvailableArrowID()
        {
            List<string> ids = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();

            HashSet<int> usedIds = new HashSet<int>();
            foreach (string idStr in ids)
            {
                if (int.TryParse(idStr, out int id))
                {
                    usedIds.Add(id);
                }
            }

            int nextId = 1;
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            return nextId.ToString();
        }

        private void CycleArrowSelection(int step)
        {
            List<string> ids = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();
            if (ids.Count == 0) return;

            ids.Sort(CompareArrowIds);
            int currentIndex = ids.IndexOf(inputController.currentArrowID);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = (currentIndex + step + ids.Count) % ids.Count;
            SelectArrow(ids[nextIndex], preserveMechanicMode: inputController.arrowMechanicMode != EditorArrowMechanicMode.None);
        }

        private void RefreshSelectedArrowFromGrid()
        {
            SetArrowDrawDirection(LevelMakerManager.Instance.GridSystem.IsHeadFirst(inputController.currentArrowID),
                savePreference: false);
            RefreshToolingStatus();
        }

        private void SetArrowDrawDirection(bool drawHeadFirst, bool savePreference = true)
        {
            inputController.drawHeadFirst = drawHeadFirst;
            if (savePreference)
            {
                _savedArrowDrawHeadFirst = drawHeadFirst;
            }
        }

        private void RestoreSavedArrowDrawDirection()
        {
            inputController.drawHeadFirst = _savedArrowDrawHeadFirst;
        }

        private string CreateNewLinkGroupId()
        {
            HashSet<string> existing = new HashSet<string>(LevelMakerManager.Instance.GridSystem.GetAllLinkGroupIds(),
                StringComparer.Ordinal);

            int index = 1;
            while (existing.Contains($"Link_{index}"))
            {
                index++;
            }

            return $"Link_{index}";
        }

        private void RefreshToolingStatus()
        {
            string mode = GetCurrentModeLabel();
            string stateValue = inputController.currentSpecialType == BoardSpecialType.CounterBlock
                ? inputController.currentCounterBlockValue.ToString()
                : inputController.currentPortalId;

            if (inputController.arrowMechanicMode != EditorArrowMechanicMode.None)
            {
                EditorArrowMetadataData metadata =
                    LevelMakerManager.Instance.GridSystem.GetArrowMetadata(inputController.currentArrowID);
                List<Vector2Int> path = LevelMakerManager.Instance.GridSystem.GetArrowPath(inputController.currentArrowID);
                int pathLength = path != null ? path.Count : 0;
                int linkedCount = !string.IsNullOrWhiteSpace(metadata?.LinkGroupId)
                    ? LevelMakerManager.Instance.GridSystem.GetLinkedArrowIds(metadata.LinkGroupId).Count
                    : 0;

                mechanicDrawerPanel?.RefreshArrowMechanicState(mode, inputController.currentArrowID, pathLength,
                    metadata != null && metadata.PrimaryEndpointPathIndex == 0 ? "Start" : "End",
                    metadata != null && metadata.HasSecondaryEndpoint, metadata?.LinkGroupId ?? string.Empty, linkedCount);
            }
            else
            {
                mechanicDrawerPanel?.RefreshState(mode, inputController.currentSpecialDirection, stateValue);
            }

            string drawDirection = inputController.drawHeadFirst ? "ĐẦU→ĐUÔI" : "ĐUÔI→ĐẦU";
            Debug.Log(
                $"<color=cyan>[Status] Mode={mode} | Mũi tên [{inputController.currentArrowID}] | {drawDirection} | Exit={inputController.currentSpecialDirection.ToGlyph()} | State={stateValue}</color>");
        }

        private string GetCurrentModeLabel()
        {
            return inputController.arrowMechanicMode switch
            {
                EditorArrowMechanicMode.TwoHead => "TWO_HEAD",
                EditorArrowMechanicMode.Link => "LINK_ARROW",
                _ => GetLegacyModeLabel()
            };
        }

        private string GetLegacyModeLabel()
        {
            if (inputController.isSelectMode) return "SELECT";
            if (inputController.brushMode == EditorBrushMode.Special)
            {
                if (inputController.currentSpecialType == BoardSpecialType.Portal) return "PORTAL";
                if (inputController.currentSpecialType == BoardSpecialType.Redirect) return "REDIRECT";
                if (inputController.currentSpecialType == BoardSpecialType.CounterBlock) return "COUNTER_BLOCK";
            }

            return inputController.currentBrush == CellType.EmptyDot ? "ERASE" : "ARROW";
        }

        private void AutoDiscoverPanels()
        {
            if (leftPanelRoot != null && rightPanelRoot != null) return;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            if (leftPanelRoot == null)
            {
                Transform t = canvas.transform.Find("LeftPanel");
                if (t != null) leftPanelRoot = t.gameObject;
            }

            if (rightPanelRoot == null)
            {
                Transform t = canvas.transform.Find("RightPanel");
                if (t != null) rightPanelRoot = t.gameObject;
            }
        }

        private void HandleToggleLeftPanel()
        {
            if (_leftRect == null) return;
            _isLeftOpen = !_isLeftOpen;
            float targetX = _isLeftOpen ? _leftStartPos.x : _leftStartPos.x - _leftRect.rect.width;
            _leftRect.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.OutCubic);
        }

        private void HandleToggleRightPanel()
        {
            if (_rightRect == null) return;
            _isRightOpen = !_isRightOpen;
            float targetX = _isRightOpen ? _rightStartPos.x : _rightStartPos.x + _rightRect.rect.width;
            _rightRect.DOAnchorPosX(targetX, 0.3f).SetEase(Ease.OutCubic);
        }

        private void InitializePanelPositions()
        {
            if (leftPanelRoot != null)
            {
                _leftRect = leftPanelRoot.GetComponent<RectTransform>();
                if (_leftRect != null) _leftStartPos = _leftRect.anchoredPosition;
            }

            if (rightPanelRoot != null)
            {
                _rightRect = rightPanelRoot.GetComponent<RectTransform>();
                if (_rightRect != null) _rightStartPos = _rightRect.anchoredPosition;
            }
        }

        private static int CompareArrowIds(string left, string right)
        {
            bool leftIsNumber = int.TryParse(left, out int leftValue);
            bool rightIsNumber = int.TryParse(right, out int rightValue);
            if (leftIsNumber && rightIsNumber) return leftValue.CompareTo(rightValue);
            if (leftIsNumber) return -1;
            if (rightIsNumber) return 1;
            return string.CompareOrdinal(left, right);
        }
    }
}
