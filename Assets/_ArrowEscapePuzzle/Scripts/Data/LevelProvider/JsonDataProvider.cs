using System;
using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Interface;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace ArrowGame.Data.LevelProvider
{
    public class JsonDataProvider : MonoBehaviour, ILevelDataProvider
    {
        [Header("Level Database (JSON)")]
        [Tooltip("Kéo thả các file JSON (TextAsset) vào đây. Lưu ý: Tên file BẮT BUỘC trùng với Level ID.")]
        [SerializeField] private List<TextAsset> _jsonFiles;

        private Dictionary<string, TextAsset> _fileMap;
        
        private Dictionary<string, LevelSaveData> _parsedCache;

        private void Awake()
        {
            int capacity = _jsonFiles != null ? _jsonFiles.Count : 0;
            _fileMap = new Dictionary<string, TextAsset>(capacity);
            _parsedCache = new Dictionary<string, LevelSaveData>(capacity);

            if (_jsonFiles == null) return;

            foreach (var file in _jsonFiles)
            {
                if (file != null)
                {
                    _fileMap[file.name] = file;
                }
            }
        }

        public LevelSaveData GetLevelData(string levelId)
        {
            if (_parsedCache.TryGetValue(levelId, out LevelSaveData cachedData))
            {
                return cachedData;
            }

            if (_fileMap.TryGetValue(levelId, out TextAsset jsonAsset))
            {
                try
                {
                    LevelSaveData data = JsonConvert.DeserializeObject<LevelSaveData>(jsonAsset.text);
                    
                    if (data != null)
                    {
                        if (string.IsNullOrEmpty(data.LevelID))
                        {
                            data.LevelID = levelId;
                        }
                        
                        _parsedCache[levelId] = data; 
                        return data;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[JsonDataProvider] Lỗi parse JSON cho level {levelId}: {e.Message}");
                    return null;
                }
            }

            Debug.LogError($"[JsonDataProvider] LỖI: Không tìm thấy file JSON nào có tên là: {levelId}");
            return null;
        }

        /// <summary>
        /// Gọi hàm này khi game nhận Memory Warning từ OS hoặc khi chuyển Scene 
        /// để giải phóng RAM trên máy cấu hình yếu.
        /// </summary>
        public void ClearMemoryCache()
        {
            _parsedCache.Clear();
        }
    }
}