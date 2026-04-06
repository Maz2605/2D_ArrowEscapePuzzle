using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace GameCore.Data
{
    public class SaveSystem
    {
        private static bool USE_ENCRYPTION = true;

        public static void Save<T>(string gameId, T data)
        {
            try
            {
                string filePath = GetPath(gameId);
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                
                if(USE_ENCRYPTION) json = Encrypt(json);

                string tempPath = filePath + ".tmp";
                File.WriteAllText(tempPath, json);
                
                if(File.Exists(filePath))
                    File.Delete(filePath);
                
                File.Move(tempPath, filePath);
                
                Debug.Log($"[Save System] Saved: {gameId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Save ERROR: {e.Message}");
            }
        }
        
        public static T Load<T>(string gameId) where T : new()
        {
            string filePath = GetPath(gameId);
            if (!File.Exists(filePath)) return new T(); 

            try
            {
                string json = File.ReadAllText(filePath);
                if (USE_ENCRYPTION) json = Decrypt(json);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Load Error: {e.Message}");
                return new T(); 
            }
        }
        
        //Helpers

        private static string GetPath(string gameId)
        {
            return Path.Combine(Application.persistentDataPath,$"{gameId}.json");
        }

        private static string Encrypt(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(bytes);
        }

        private static string Decrypt(string data)
        {
            var bytes = Convert.FromBase64String(data);
            return Encoding.UTF8.GetString(bytes);
        }
        
        public static void DeleteFile(string gameId)
        {
            try
            {
                string filePath = GetPath(gameId);
                
                // Xóa file chính
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[SaveSystem] Deleted Save File: {filePath}");
                }
                
                // Xóa luôn file tạm (.tmp) nếu lỡ còn sót lại
                string tempPath = filePath + ".tmp";
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Delete Error: {e.Message}");
            }
        }
        
        public static void SaveToPath<T>(string fullPath, T data, bool useEncryption = false)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                if (useEncryption) json = Encrypt(json);

                string tempPath = fullPath + ".tmp";
                File.WriteAllText(tempPath, json);
        
                if (File.Exists(fullPath)) File.Delete(fullPath);
                File.Move(tempPath, fullPath);
        
                Debug.Log($"[Save System] Saved to Path: {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] SaveToPath ERROR: {e.Message}");
            }
        }

        public static T LoadFromPath<T>(string fullPath, bool useEncryption = false) where T : new()
        {
            if (!File.Exists(fullPath)) return new T(); 

            try
            {
                string json = File.ReadAllText(fullPath);
                if (useEncryption) json = Decrypt(json);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] LoadFromPath Error: {e.Message}");
                return new T(); 
            }
        }
    }
}
