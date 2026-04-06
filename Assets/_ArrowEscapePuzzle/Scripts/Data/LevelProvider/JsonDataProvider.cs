using System;
using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Interface;
using Newtonsoft.Json; 
using UnityEngine;

namespace ArrowGame.Data.LevelProvider
{
    public class JsonDataProvider : MonoBehaviour, ILevelDataProvider
    {
        [Header("Manual List (Testing & Prototype)")]
        [Tooltip("Kéo các file JSON vào đây. Tên file bắt buộc phải khớp với levelId (VD: Level_1)")]
        [SerializeField] private List<TextAsset> _jsonFiles;

        // Cache map chứa reference tới file TextAsset (không cache Data Object để tránh lỗi mutation)
        private Dictionary<string, TextAsset> _fileMap;

        private void Awake()
        {
            InitializeData();
        }

        private void InitializeData()
        {
            int capacity = _jsonFiles != null ? _jsonFiles.Count : 0;
            _fileMap = new Dictionary<string, TextAsset>(capacity);

            if (_jsonFiles == null) return;

            foreach (var file in _jsonFiles)
            {
                if (file != null && !_fileMap.ContainsKey(file.name))
                {
                    _fileMap[file.name] = file;
                }
            }
        }

        public LevelSaveData GetLevelData(string levelId)
        {
            // 1. Tìm file TextAsset trong list đã kéo vào Inspector
            if (_fileMap.TryGetValue(levelId, out TextAsset jsonAsset))
            {
                // 2. Parse thẳng từ JSON string mỗi lần gọi để tạo ra một instance MỚI.
                // Điều này đảm bảo khi gameplay làm thay đổi Data, lúc Replay lại map vẫn nguyên vẹn.
                LevelSaveData data = ParseLevelData(jsonAsset.text, levelId);
                if (data != null)
                {
                    return data;
                }
            }

            // Báo lỗi rõ ràng nếu không tìm thấy file trong Inspector hoặc quên kéo file vào.
            Debug.LogError($"[JsonDataProvider] LỖI: Không tìm thấy level '{levelId}' trong List _jsonFiles! \n" +
                           $"Vui lòng kiểm tra lại: Tên file JSON có đúng là '{levelId}' không và đã kéo vào Inspector chưa?");
            return null;
        }

        private LevelSaveData ParseLevelData(string jsonText, string levelId)
        {
            try
            {
                LevelSaveData data = JsonConvert.DeserializeObject<LevelSaveData>(jsonText);
                
                if (data != null)
                {
                    if (string.IsNullOrEmpty(data.LevelID))
                    {
                        data.LevelID = levelId;
                    }
                    
                    // Safety check cho Cells
                    if (data.Cells == null)
                    {
                        data.Cells = new List<CellData>();
                        Debug.LogWarning($"[JsonDataProvider] Level {levelId} có dữ liệu Cells bị null. Đã khởi tạo list rỗng.");
                    }
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonDataProvider] Lỗi parse JSON cho level {levelId}: {e.Message}\n" +
                               $"File JSON có thể bị sai format (thiếu ngoặc, sai dấu phẩy...).");
                return null;
            }
        }

        public void ClearMemoryCache()
        {
            // Vì dùng List Inspector (Hard Reference), TextAsset không bị GC thu hồi nên hàm này tạm thời không cần làm gì.
            // Sẽ cần thiết khi sau này bạn nâng cấp lên Addressables.
        }
    }
}