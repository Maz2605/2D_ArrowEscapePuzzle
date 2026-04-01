using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.Logic;
using EditorTool.Scripts.EditorTool.Visual;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using ShareCore.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.System
{
    [DefaultExecutionOrder(-100)]
    public class LevelMakerManager : Singleton<LevelMakerManager>
    {
        [Header("References")] 
        [SerializeField] private GridView gridView;

        [Header("Settings")] 
        [SerializeField] private int startWidth = 10;
        [SerializeField] private int startHeight = 10;
        
        public string currentLevelID = "Level_01";
        public LevelDifficulty currentDifficulty = LevelDifficulty.Normal;

        public GridSystem GridSystem { get; private set; }
        public MakerPhase CurrentPhase { get; private set; } = MakerPhase.BaseMap;

        // Cờ đánh dấu Map có thay đổi chưa được lưu
        public bool IsDirty { get; set; } = false;

        protected override void Awake()
        {
            base.Awake();
            GridSystem = new GridSystem();
            GridSystem.Initialize(startWidth, startHeight);
        }

        private void Start()
        {
            if (gridView != null) gridView.Initialize(GridSystem);
            
            // Fire event để Camera focus vào map ngay lúc khởi động
            EventManager<EditorEventType>.Post<(int, int)>(
                EditorEventType.MapLoadedOrCreated, (startWidth, startHeight));
            
            // Bất cứ khi nào Map bị vẽ/xóa, bật cờ IsDirty lên true
            GridSystem.OnCellChanged += (x, y, data) => IsDirty = true;
        }

        public void ExportLevel()
        {
            var validation = MapValidator.ValidateBaseMap(GridSystem);

            if (!validation.isValid)
            {
                Debug.LogError($"<color=red>[Validate Failed] {validation.errorMsg}</color>");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog("Cảnh báo lỗi Map!", validation.errorMsg, "Đã hiểu, tôi sẽ sửa");
#endif
                return;
            }

            LevelSaveData saveData = new LevelSaveData()
            {
                LevelID = this.currentLevelID,
                Width = GridSystem.Width,
                Height = GridSystem.Height,
                Difficulty = this.currentDifficulty,
                Cells = new List<CellData>()
            };

            for (int x = 0; x < GridSystem.Width; x++)
            {
                for (int y = 0; y < GridSystem.Height; y++)
                {
                    var cell = GridSystem.GetCell(x, y);
                    if (cell.type != CellType.EmptyDot) 
                    {
                        saveData.Cells.Add(new ShareCore.Data.CellData(x, y, cell.type, cell.arrowID));
                    }
                }
            }

            // Gọi qua SaveLoadService để lưu
            SaveLoadService.SaveLevelEditor(currentLevelID, saveData);
            
            // Save thành công thì tắt cờ IsDirty
            IsDirty = false;
        }
        
        public void ClearMap()
        {
            GridSystem.ClearAllPaths();
            IsDirty = false; // Dọn sạch map coi như là map trắng mới, tắt cờ đi
            Debug.Log("<color=yellow>[Manager] Đã dọn sạch Map!</color>");
        }

        public void ResizeGrid(int newWidth, int newHeight, bool forceRebuild = false)
        {
            if (!forceRebuild && GridSystem.Width == newWidth && GridSystem.Height == newHeight) return;
            startWidth = newWidth;
            startHeight = newHeight;
            GridSystem.Initialize(newWidth, newHeight);
            if (gridView != null) gridView.RebuildGrid();
        }

        public void LoadOrCreateLevel(string levelID, int fallbackWidth, int fallbackHeight)
        {
            currentLevelID = levelID;

            if (!SaveLoadService.DoesLevelExist(levelID))
            {
                Debug.Log($"<color=green>[Tạo mới] Khởi tạo Level mới: {levelID} ({fallbackWidth}x{fallbackHeight})</color>");
                ResizeGrid(fallbackWidth, fallbackHeight, true);
                ClearMap();
                
                // Broadcast event — Camera tự lắng nghe, không cần Find
                EventManager<EditorEventType>.Post<(int, int)>(EditorEventType.MapLoadedOrCreated, (fallbackWidth, fallbackHeight));
                
                IsDirty = false; // Map mới tạo, tắt cờ
                return;
            }

            var saveData = SaveLoadService.LoadLevelEditor(levelID);
            if (saveData != null)
            {
                currentDifficulty = saveData.Difficulty;
                ResizeGrid(saveData.Width, saveData.Height, true);
                ClearMap();

                foreach (var cell in saveData.Cells)
                {
                    if (cell.x < GridSystem.Width && cell.y < GridSystem.Height)
                    {
                        GridSystem.SetCell(cell.x, cell.y, cell.type, cell.arrowID);
                    }
                }

                GridSystem.ReconstructPathsFromGrid();
                GridSystem.ForceRefreshVisual();

                // Broadcast event — Camera tự lắng nghe, không cần Find
                EventManager<EditorEventType>.Post<(int, int)>(EditorEventType.MapLoadedOrCreated, (saveData.Width, saveData.Height));

                IsDirty = false; // Load thành công thì tắt cờ
                Debug.Log($"<color=#00FFFF>[Load] Đã tải thành công {levelID}.json!</color>");
            }
        }
    }
}