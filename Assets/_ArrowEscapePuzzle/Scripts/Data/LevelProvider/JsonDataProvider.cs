using System;
using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Interface;
using Newtonsoft.Json;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Data.LevelProvider
{
    public class JsonDataProvider : MonoBehaviour, ILevelDataProvider
    {
        [Header("Resources Settings")]
        [Tooltip("Tên thư mục con nằm trong thư mục Resources chứa các file JSON level.")]
        [SerializeField] private string levelFolder = "Levels";

        public LevelSaveData GetLevelData(string levelId)
        {
            string resourcePath = $"{levelFolder}/{levelId}";
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);

            if (jsonAsset != null)
            {
                LevelSaveData data = ParseLevelData(jsonAsset.text, levelId);
                Resources.UnloadAsset(jsonAsset);
                
                if (data != null) return data;
            }
            Debug.LogError($"[JsonDataProvider] KHÔNG tìm thấy level '{levelId}' tại đường dẫn Resources/{resourcePath}!");
            return null;
        }

        private LevelSaveData ParseLevelData(string jsonText, string levelId)
        {
            try
            {
                LevelSaveData data = JsonConvert.DeserializeObject<LevelSaveData>(jsonText);
                
                if (data != null)
                {
                    if (string.IsNullOrEmpty(data.LevelID)) data.LevelID = levelId;
                    
                    if (data.Arrows == null)
                    {
                        data.Arrows = new List<ArrowSaveData>();
                        Debug.LogWarning($"[JsonDataProvider] Level {levelId} thiếu dữ liệu Arrows (List-based).");
                    }
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[JsonDataProvider] Lỗi parse JSON cho level {levelId}: {e.Message}");
                return null;
            }
        }

        public void ClearMemoryCache()
        {
            // Khi dùng Resources.UnloadAsset() phía trên, memory đã được dọn sạch từng level.
            // Nếu bạn có cache hệ thống nào khác thì xử lý ở đây.
        }
    }
}