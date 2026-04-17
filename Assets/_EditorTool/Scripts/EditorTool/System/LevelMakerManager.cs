using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.Logic;
using EditorTool.Scripts.EditorTool.Visual;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.Singleton;
using ShareCore.Data;
using ShareCore.Scripts.Data;
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
            EventManager<EditorEventType>.Post<(int, int)>(EditorEventType.MapLoadedOrCreated, (startWidth, startHeight));
            GridSystem.OnCellChanged += (x, y, data) => IsDirty = true;
        }

        // ĐÃ SỬA: Trả về bool để UI biết lưu có thành công không
        public bool ExportLevel()
        {
            GridSystem.SyncGridWithPaths();

            var validation = MapValidator.ValidateBaseMap(GridSystem);
            if (!validation.isValid)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog("Lỗi Map!", validation.errorMsg, "OK");
#endif
                return false; // Lưu thất bại
            }

            LevelSaveData saveData = new LevelSaveData()
            {
                LevelID = this.currentLevelID,
                Width = GridSystem.Width,
                Height = GridSystem.Height,
                Difficulty = this.currentDifficulty,
                Arrows = GridSystem.GetSaveData() 
            };

            SaveLoadService.SaveLevelEditor(currentLevelID, saveData);
            IsDirty = false;
            
            return true; // Lưu thành công
        }
        
        public void ClearMap()
        {
            GridSystem.ClearAllPaths();
            IsDirty = false; 
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
                ResizeGrid(fallbackWidth, fallbackHeight, true);
                ClearMap();
                EventManager<EditorEventType>.Post<(int, int)>(EditorEventType.MapLoadedOrCreated, (fallbackWidth, fallbackHeight));
                IsDirty = false;
                return;
            }

            var saveData = SaveLoadService.LoadLevelEditor(levelID);
            if (saveData != null)
            {
                currentDifficulty = saveData.Difficulty;
                ResizeGrid(saveData.Width, saveData.Height, true);
        
                GridSystem.LoadFromSaveData(saveData.Arrows);

                EventManager<EditorEventType>.Post<(int, int)>(EditorEventType.MapLoadedOrCreated, (saveData.Width, saveData.Height));
                IsDirty = false;
            }
        }
    }
}