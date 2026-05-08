using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.System;
using EditorTool.Scripts.EditorTool.Visual;
using EditorTool.Scripts.UI.Panels;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;
using DG.Tweening;

namespace EditorTool.Scripts.EditorTool.Controller
{
    [DefaultExecutionOrder(-50)]
    public class EditorController : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private InputController inputController;
        // [SerializeField] private ReferenceImageController referenceImageController;

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

        private void Start()
        {
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
            inputController.OnRotateDirectionHotkey = HandleRotateSpecialDirection;
            inputController.OnCyclePortalIdHotkey = HandleCyclePortalId;
            inputController.OnDirectionHotkey = HandleSpecialDirectionChanged;
            inputController.OnToggleLeftPanelHotkey = HandleToggleLeftPanel;
            inputController.OnToggleRightPanelHotkey = HandleToggleRightPanel;
            inputController.OnSpecialSelectedFromMap = HandleSpecialSelected;
            inputController.OnSpecialCellPlaced = HandleSpecialCellPlaced;
            inputController.OnSpecialCellRemoved = RefreshToolingStatus;
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

            // if (referenceImageController != null)
            // {
            //     mapSettingsPanel.OnLoadReferenceImage = () => referenceImageController.LoadReferenceImage();
            //     mapSettingsPanel.OnReferenceOpacityChanged = val => referenceImageController.SetOpacity(val);
            //     mapSettingsPanel.OnReferenceScaleChanged = val => referenceImageController.SetScale(val);
            //     mapSettingsPanel.OnReferencePosXChanged = val => referenceImageController.SetPositionX(val);
            //     mapSettingsPanel.OnReferencePosYChanged = val => referenceImageController.SetPositionY(val);
            // }
        }

        private void WireMechanicDrawerPanel()
        {
            if (mechanicDrawerPanel == null) return;

            mechanicDrawerPanel.OnArrowMode = HandleArrowMode;
            mechanicDrawerPanel.OnPortalBrush = HandlePortalBrush;
            mechanicDrawerPanel.OnRedirectBrush = HandleRedirectBrush;
            mechanicDrawerPanel.OnDirectionChanged = HandleSpecialDirectionChanged;
            mechanicDrawerPanel.OnPortalIdChanged = HandlePortalIdChanged;
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
                    mechanicDrawerPanel = leftPanel.gameObject.AddComponent<MechanicDrawerPanel>();
                return;
            }
        }

        private void HandleArrowSelected(string arrowID)
        {
            inputController.currentArrowID = arrowID;
            inputController.currentBrush = CellType.ArrowBodyVertical;
            inputController.isSelectMode = false;
            inputController.brushMode = EditorBrushMode.Arrow;

            if (LevelMakerManager.Instance.GridSystem.GetAllArrowIDs().Contains(arrowID))
            {
                inputController.drawHeadFirst = LevelMakerManager.Instance.GridSystem.IsHeadFirst(arrowID);
            }

            if (int.TryParse(arrowID, out int parsed)) _currentArrowId = parsed;
            RefreshToolingStatus();

            LevelMakerManager.Instance.GridView.PlayArrowBounce(arrowID);
        }

        private void HandleNewArrow()
        {
            string newID = GetNextAvailableArrowID();
            HandleArrowSelected(newID);
        }

        private void HandleSwap()
        {
            inputController.drawHeadFirst = !inputController.drawHeadFirst;
            LevelMakerManager.Instance.GridSystem.FlipArrowPath(inputController.currentArrowID);
            RefreshToolingStatus();
        }

        private void HandleErase()
        {
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.currentBrush = CellType.EmptyDot;
            inputController.isSelectMode = false;
            RefreshToolingStatus();
        }

        private void HandleToggleSelect()
        {
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.isSelectMode = !inputController.isSelectMode;
            RefreshToolingStatus();
        }

        private void HandleArrowMode()
        {
            inputController.brushMode = EditorBrushMode.Arrow;
            inputController.currentBrush = CellType.ArrowBodyVertical;
            inputController.isSelectMode = false;
            RefreshToolingStatus();
        }

        private void HandlePortalBrush()
        {
            inputController.brushMode = EditorBrushMode.Special;
            inputController.currentSpecialType = BoardSpecialType.Portal;
            inputController.isSelectMode = false;
            AutoSelectPortalId();
            RefreshToolingStatus();
        }

        private void HandleRedirectBrush()
        {
            inputController.brushMode = EditorBrushMode.Special;
            inputController.currentSpecialType = BoardSpecialType.Redirect;
            inputController.isSelectMode = false;
            AutoSelectRedirectId();
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

        private void HandleSpecialSelected(SpecialCellSaveData data)
        {
            inputController.brushMode = EditorBrushMode.Special;
            inputController.currentSpecialType = data.Type;
            inputController.currentSpecialDirection = data.ExitDirection;
            inputController.currentPortalId = data.PortalId;
            inputController.isSelectMode = false;
            RefreshToolingStatus();
        }

        private void HandleMechanicSelected(string idOrPos)
        {
            var specialCells = LevelMakerManager.Instance.GridSystem.GetSpecialSaveData();
            foreach (var cell in specialCells)
            {
                string posStr = $"{cell.Position.x},{cell.Position.y}";
                if (cell.PortalId == idOrPos || posStr == idOrPos)
                {
                    HandleSpecialSelected(cell);
                    LevelMakerManager.Instance.GridView.PlaySpecialCellBounce(cell.Position);
                    return;
                }
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
            var specialCells = LevelMakerManager.Instance.GridSystem.GetSpecialSaveData();
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (var cell in specialCells)
            {
                if (cell.Type == BoardSpecialType.Portal && !string.IsNullOrEmpty(cell.PortalId))
                {
                    counts[cell.PortalId] = counts.ContainsKey(cell.PortalId) ? counts[cell.PortalId] + 1 : 1;
                }
            }

            // 1. Find ID with exactly 1 portal (to pair it)
            foreach (var kvp in counts)
            {
                if (kvp.Value == 1)
                {
                    inputController.currentPortalId = kvp.Key;
                    return;
                }
            }

            // 2. Find first unused ID (A, B, C...)
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
            var specialCells = LevelMakerManager.Instance.GridSystem.GetSpecialSaveData();
            HashSet<int> usedIds = new HashSet<int>();
            foreach (var cell in specialCells)
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

            LevelMakerManager.Instance.ClearMap();
            LevelMakerManager.Instance.IsDirty = false;
            drawingToolPanel.RefreshArrowList();

            _currentArrowId = 1;
            HandleArrowSelected("1");
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

            LevelMakerManager.Instance.LoadOrCreateLevel(levelID, mapSettingsPanel.Width, mapSettingsPanel.Height);
            LevelMakerManager.Instance.IsDirty = false;

            mapSettingsPanel.Refresh();
            drawingToolPanel.RefreshArrowList();
            drawingToolPanel.AutoSelectLastArrow();
            searchPanel.ScanSavedLevels();
            RefreshToolingStatus();
        }

        private void HandleMapResized()
        {
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

        private void RefreshToolingStatus()
        {
            string mode = GetCurrentModeLabel();
            mechanicDrawerPanel?.RefreshState(mode, inputController.currentSpecialDirection,
                inputController.currentPortalId);

            string drawDirection = inputController.drawHeadFirst ? "ĐẦU→ĐUÔI" : "ĐUÔI→ĐẦU";
            Debug.Log(
                $"<color=cyan>[Status] Mode={mode} | Mũi tên [{inputController.currentArrowID}] | {drawDirection} | Exit={inputController.currentSpecialDirection.ToGlyph()} | Portal={inputController.currentPortalId}</color>");
        }

        private string GetCurrentModeLabel()
        {
            if (inputController.isSelectMode) return "SELECT";
            if (inputController.brushMode == EditorBrushMode.Special)
            {
                return inputController.currentSpecialType == BoardSpecialType.Portal ? "PORTAL" : "REDIRECT";
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
    }
}
