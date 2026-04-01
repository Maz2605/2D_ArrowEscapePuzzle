using EditorTool.Scripts.EditorTool.System;
using EditorTool.Scripts.UI.Panels;
using ShareCore.Data;
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

        private int _currentArrowId = 1;

        private void Start()
        {
            WireInputController();
            WireDrawingToolPanel();
            WireSearchPanel();
            WireMapSettingsPanel();

            // Khởi tạo panels sau khi đã gán đủ callbacks
            drawingToolPanel.Initialize();
            searchPanel.Initialize();
            mapSettingsPanel.Initialize();
        }

        // =====================================================================
        // WIRING — Gán callbacks từ EditorController vào từng component
        // =====================================================================

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
        }

        // =====================================================================
        // HANDLERS — Business logic tập trung, không rải rác
        // =====================================================================

        /// <summary>Arrow được chọn từ list UI hoặc từ click trên map.</summary>
        private void HandleArrowSelected(string arrowID)
        {
            inputController.currentArrowID = arrowID;
            inputController.currentBrush   = CellType.ArrowBodyVertical;
            inputController.isSelectMode   = false;

            if (int.TryParse(arrowID, out int parsed)) _currentArrowId = parsed;

            LogStatus();
        }

        /// <summary>[A / btnNewArrow] Tạo mũi tên mới với ID kế tiếp.</summary>
        private void HandleNewArrow()
        {
            string newID = GetNextAvailableArrowID();
            HandleArrowSelected(newID);
            Debug.Log($"<color=green>[New arrow] Số {newID}</color>");
        }

        /// <summary>[S / btnSwap] Đổi hướng mũi tên đang chọn.</summary>
        private void HandleSwap()
        {
            inputController.drawHeadFirst = !inputController.drawHeadFirst;
            LevelMakerManager.Instance.GridSystem.FlipArrowPath(inputController.currentArrowID);
            LogStatus();
        }

        /// <summary>[E / btnErase] Chuyển sang chế độ Tẩy.</summary>
        private void HandleErase()
        {
            inputController.currentBrush = CellType.EmptyDot;
            inputController.isSelectMode = false;
            Debug.Log("<color=orange>[Erase] Chế độ TẨY (→ EmptyDot)</color>");
        }

        /// <summary>[V / btnSelect] Toggle chế độ chọn arrow.</summary>
        private void HandleToggleSelect()
        {
            inputController.isSelectMode = !inputController.isSelectMode;
            Debug.Log($"<color=yellow>[Select] {(inputController.isSelectMode ? "BẬT" : "TẮT")}</color>");
        }

        /// <summary>[btnResetMap] Xóa toàn bộ map.</summary>
        private void HandleResetMap()
        {
            if (!CheckUnsavedChanges()) return;

            LevelMakerManager.Instance.ClearMap();
            LevelMakerManager.Instance.IsDirty = false;
            drawingToolPanel.RefreshArrowList();

            _currentArrowId = 1;
            HandleArrowSelected("1");

            Debug.Log("<color=#00FF00>[Reset] Đã dọn sạch Map!</color>");
        }

        /// <summary>[btnSave] Lưu map sau khi user xác nhận.</summary>
        private void HandleSaveMap()
        {
            string levelID = LevelMakerManager.Instance.currentLevelID;

#if UNITY_EDITOR
            bool confirm = UnityEditor.EditorUtility.DisplayDialog(
                "Xác nhận lưu Map",
                $"Bạn có chắc chắn muốn lưu Level:\n\n\"{levelID}\"\n\nkhông?",
                "Lưu ngay!", "Hủy");

            if (!confirm) return;
#endif
            LevelMakerManager.Instance.ExportLevel();
        }

        /// <summary>Khi dropdown SearchPanel chọn level — preview thông tin lên MapSettingsPanel.</summary>
        private void HandleDataPreview(LevelSaveData previewData)
        {
            mapSettingsPanel.RefreshFromPreview(previewData);
        }

        /// <summary>[btnLoad] Load hoặc tạo mới level.</summary>
        private void HandleLoadLevel()
        {
            if (!CheckUnsavedChanges()) return;

            string levelID = searchPanel.CurrentSearchText;
            if (string.IsNullOrEmpty(levelID))
            {
                Debug.LogWarning("[EditorController] Level ID không được để trống!");
                return;
            }

            LevelMakerManager.Instance.LoadOrCreateLevel(levelID, mapSettingsPanel.Width, mapSettingsPanel.Height);
            LevelMakerManager.Instance.IsDirty = false;

            mapSettingsPanel.Refresh();
            drawingToolPanel.RefreshArrowList();
            drawingToolPanel.AutoSelectLastArrow();
            searchPanel.ScanSavedLevels();
        }

        /// <summary>Khi width/height input thay đổi — resize grid.</summary>
        private void HandleMapResized()
        {
            LevelMakerManager.Instance.ResizeGrid(mapSettingsPanel.Width, mapSettingsPanel.Height);
        }

        // =====================================================================
        // SHARED UTILITIES
        // =====================================================================

        private bool CheckUnsavedChanges()
        {
            if (!LevelMakerManager.Instance.IsDirty) return true;
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.DisplayDialog(
                "Cảnh báo chưa lưu!",
                "Map có thay đổi chưa lưu.\n\nBạn có chắc chắn muốn bỏ không?",
                "Bỏ", "Để Save lại");
#else
            return true;
#endif
        }

        private string GetNextAvailableArrowID()
        {
            var ids = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();
            int maxId = 0;
            foreach (string idStr in ids)
                if (int.TryParse(idStr, out int id) && id > maxId) maxId = id;
            return (maxId + 1).ToString();
        }

        private void LogStatus()
        {
            string dir = inputController.drawHeadFirst ? "ĐẦU→ĐUÔI" : "ĐUÔI→ĐẦU";
            Debug.Log($"<color=cyan>[Status] Mũi tên [{inputController.currentArrowID}] | {dir}</color>");
        }
    }
}
