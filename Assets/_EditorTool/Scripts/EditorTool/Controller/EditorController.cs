using System.Collections.Generic;
using EditorTool.Scripts.EditorTool.System;
using EditorTool.Scripts.UI.Panels;
using EditorTool.Scripts.EditorTool.Visual;
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
        [SerializeField] private ReferenceImageController referenceImageController;

        [Header("UI Panels")]
        [SerializeField] private DrawingToolPanel drawingToolPanel;
        [SerializeField] private SearchPanel searchPanel;
        [SerializeField] private MapSettingsPanel mapSettingsPanel;

        private int _currentArrowId = 1;

        private void Start()
        {
            WireInputController();
            WireDrawingToolPanel();
            WireSearchPanel();
            WireMapSettingsPanel();

            drawingToolPanel.Initialize();
            searchPanel.Initialize();
            mapSettingsPanel.Initialize();
        }

        private void WireInputController()
        {
            inputController.OnNewArrowHotkey       = HandleNewArrow;
            inputController.OnSwapHotkey           = HandleSwap;
            inputController.OnEraseHotkey          = HandleErase;
            inputController.OnSelectHotkey         = HandleToggleSelect;
            inputController.OnArrowSelectedFromMap = HandleArrowSelected;
        }

        private void WireDrawingToolPanel()
        {
            drawingToolPanel.OnNewArrow              = HandleNewArrow;
            drawingToolPanel.OnSwap                  = HandleSwap;
            drawingToolPanel.OnErase                 = HandleErase;
            drawingToolPanel.OnSelect                = HandleToggleSelect;
            drawingToolPanel.OnResetMap              = HandleResetMap;
            drawingToolPanel.OnSaveMap               = HandleSaveMap;
            drawingToolPanel.OnArrowSelectedFromList = HandleArrowSelected;
        }

        private void WireSearchPanel()
        {
            searchPanel.OnCheckUnsavedChanges = CheckUnsavedChanges;
            searchPanel.OnDataPreviewLoaded   = HandleDataPreview;
            searchPanel.OnLoadButtonClicked   = HandleLoadLevel;
        }

        private void WireMapSettingsPanel()
        {
            mapSettingsPanel.OnMapResized = HandleMapResized;

            if (referenceImageController != null)
            {
                mapSettingsPanel.OnLoadReferenceImage      = () => referenceImageController.LoadReferenceImage();
                mapSettingsPanel.OnReferenceOpacityChanged = (val) => referenceImageController.SetOpacity(val);
                mapSettingsPanel.OnReferenceScaleChanged   = (val) => referenceImageController.SetScale(val);
                mapSettingsPanel.OnReferencePosXChanged    = (val) => referenceImageController.SetPositionX(val);
                mapSettingsPanel.OnReferencePosYChanged    = (val) => referenceImageController.SetPositionY(val);
            }
        }

        private void HandleArrowSelected(string arrowID)
        {
            inputController.currentArrowID = arrowID;
            inputController.currentBrush   = CellType.ArrowBodyVertical;
            inputController.isSelectMode   = false;

            // ÉP CHẾ ĐỘ: Lấy state của mũi tên được chọn gán cho biến cục bộ
            if (LevelMakerManager.Instance.GridSystem.GetAllArrowIDs().Contains(arrowID))
            {
                inputController.drawHeadFirst = LevelMakerManager.Instance.GridSystem.IsHeadFirst(arrowID);
            }

            if (int.TryParse(arrowID, out int parsed)) _currentArrowId = parsed;

            LogStatus();
        }

        private void HandleNewArrow()
        {
            string newID = GetNextAvailableArrowID();
            HandleArrowSelected(newID);
        }

        private void HandleSwap()
        {
            // 1. Đảo chế độ vẽ cục bộ
            inputController.drawHeadFirst = !inputController.drawHeadFirst;
            
            // 2. Yêu cầu GridSystem lật Hình Ảnh (Visual) của mũi tên hiện tại
            LevelMakerManager.Instance.GridSystem.FlipArrowPath(inputController.currentArrowID);
            
            LogStatus();
        }

        private void HandleErase()
        {
            inputController.currentBrush = CellType.EmptyDot;
            inputController.isSelectMode = false;
        }

        private void HandleToggleSelect()
        {
            inputController.isSelectMode = !inputController.isSelectMode;
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
            bool confirm = UnityEditor.EditorUtility.DisplayDialog("Xác nhận lưu Map", $"Lưu Level: \"{levelID}\"?", "Lưu ngay!", "Hủy");
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
        }

        private void HandleMapResized()
        {
            LevelMakerManager.Instance.ResizeGrid(mapSettingsPanel.Width, mapSettingsPanel.Height);
        }

        private bool CheckUnsavedChanges()
        {
            if (!LevelMakerManager.Instance.IsDirty) return true;
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.DisplayDialog("Cảnh báo", "Map chưa lưu. Bạn có muốn bỏ không?", "Bỏ", "Hủy");
#else
            return true;
#endif
        }

        private string GetNextAvailableArrowID()
        {
            var ids = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();
            
            // Đưa tất cả các ID hiện có vào một HashSet để tra cứu siêu tốc (O(1))
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

        private void LogStatus()
        {
            // Bỏ comment dòng dưới nếu bạn đã làm UI Text hiển thị hướng vẽ
            // drawingToolPanel.UpdateDrawingIndicator(inputController.currentArrowID, inputController.drawHeadFirst);

            string dir = inputController.drawHeadFirst ? "ĐẦU→ĐUÔI" : "ĐUÔI→ĐẦU";
            Debug.Log($"<color=cyan>[Status] Mũi tên [{inputController.currentArrowID}] | {dir}</color>");
        }
    }
}