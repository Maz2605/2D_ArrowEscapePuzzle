using System.Collections.Generic;
using System.IO;
using ShareCore.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.System
{
    public static class SaveLoadService 
    {
        // Hàm nội bộ để lấy đường dẫn chuẩn, tự tạo thư mục nếu chưa có
        private static string GetFolderPath()
        {
            string path = Path.Combine(Application.dataPath, "_EditorTool", "Data", "Levels");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        // Lấy danh sách toàn bộ file map đang có (phục vụ cho hàm Scan Map của UI)
        public static List<string> GetAllSavedLevels()
        {
            List<string> levels = new List<string>();
#if UNITY_EDITOR
            var files = Directory.GetFiles(GetFolderPath(), "*.json");
            foreach (var f in files)
            {
                // Lấy tên file bỏ đuôi .json để nạp vào Dropdown
                levels.Add(Path.GetFileNameWithoutExtension(f));
            }
#endif
            return levels;
        }

        // Kiểm tra xem Map có tồn tại không (Phục vụ cho check Tạo mới hay Load đè)
        public static bool DoesLevelExist(string levelID)
        {
            string fullPath = Path.Combine(GetFolderPath(), $"{levelID}.json");
            return File.Exists(fullPath);
        }

        // Lưu data xuống file JSON
        public static void SaveLevelEditor(string levelID, LevelSaveData data)
        {
#if UNITY_EDITOR
            string fullPath = Path.Combine(GetFolderPath(), $"{levelID}.json");
            
            // Tái sử dụng SaveSystem có sẵn của GameCore
            GameCore.Data.SaveSystem.SaveToPath(fullPath, data, useEncryption: false);
            
            // Refresh lại thư mục Editor để file JSON hiện ra ngay lập tức
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"<color=#00FF00>[Thành công] Đã xuất {levelID}.json tại: {fullPath}</color>");
#endif
        }

        // Tải data từ file JSON lên để tái tạo Map
        public static LevelSaveData LoadLevelEditor(string levelID)
        {
#if UNITY_EDITOR
            string fullPath = Path.Combine(GetFolderPath(), $"{levelID}.json");
            if (File.Exists(fullPath))
            {
                return GameCore.Data.SaveSystem.LoadFromPath<LevelSaveData>(fullPath, false);
            }
#endif
            return null; // Trả về null nếu lỗi hoặc không tìm thấy
        }
    }
}