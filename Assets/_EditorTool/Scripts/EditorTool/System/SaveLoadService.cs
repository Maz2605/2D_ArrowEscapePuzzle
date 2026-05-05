using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.System
{
    public static class SaveLoadService 
    {
        // Hàm nội bộ để lấy đường dẫn chuẩn, tự tạo thư mục nếu chưa có
        private static string GetFolderPath()
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Levels");
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

        // Lưu data xuống file JSON (chạy ngầm trên Background Thread, không càn đơ màn hình)
        public static async void SaveLevelEditor(string levelID, LevelSaveData data)
        {
#if UNITY_EDITOR
            string fullPath = Path.Combine(GetFolderPath(), $"{levelID}.json");

            // Serialize toàn bộ data sang JSON trên Background Thread — không chặn main thread
            // Dùng Formatting.None để file nhỏ gọn hơn so với Indented (tiết kiệm 30-40%)
            string json = await Task.Run(() => JsonConvert.SerializeObject(data, Formatting.None));

            string tempPath = fullPath + ".tmp";
            await Task.Run(() =>
            {
                File.WriteAllText(tempPath, json);
                if (File.Exists(fullPath)) File.Delete(fullPath);
                File.Move(tempPath, fullPath);
            });

            // Chỉ nhiệm vụ cập nhật đúng file JSON vừa lưu (nhanh gấp ~100 lần so với Refresh() toàn bộ)
            string relativePath = "Assets" + fullPath.Replace(Application.dataPath, "").Replace("\\", "/");
            UnityEditor.AssetDatabase.ImportAsset(relativePath, UnityEditor.ImportAssetOptions.ForceUpdate);

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