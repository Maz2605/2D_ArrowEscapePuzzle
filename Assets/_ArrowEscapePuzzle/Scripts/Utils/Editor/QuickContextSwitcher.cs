#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArrowGame.Utils.Editor
{
    public class QuickContextSwitcher : EditorWindow
    {
        // ====================================================================
        // 1. CẤU HÌNH ĐƯỜNG DẪN TẠI ĐÂY (Sửa nếu bạn đổi tên thư mục)
        // ====================================================================
        private const string GAME_SCENE_PATH   = "Assets/_ArrowEscapePuzzle/Scenes/Gameplay.unity";
        private const string GAME_LAYOUT_PATH  = "Assets/_EditorTool/Layouts/Vertical.wlt";

        private const string EDITOR_SCENE_PATH = "Assets/_EditorTool/Scenes/EditorScene.unity";
        private const string EDITOR_LAYOUT_PATH= "Assets/_EditorTool/Layouts/EditorLayout.wlt";
        // ====================================================================

        [MenuItem("Tools/🚀 Quick Context Switcher")]
        public static void ShowWindow()
        {
            var window = GetWindow<QuickContextSwitcher>("Switcher");
            window.minSize = new Vector2(280, 150);
            window.maxSize = new Vector2(400, 200);
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("QUY TRÌNH KIỂM THỬ NHANH", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // Nút sang Game
            GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
            if (GUILayout.Button("▶ SANG GAME SCENE (PLAY)", GUILayout.Height(40)))
            {
                SwitchContext(GAME_SCENE_PATH, GAME_LAYOUT_PATH, false);
            }

            EditorGUILayout.Space(5);

            // Nút về Editor
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
            if (GUILayout.Button("🛠 VỀ EDITOR SCENE (STOP)", GUILayout.Height(40)))
            {
                SwitchContext(EDITOR_SCENE_PATH, EDITOR_LAYOUT_PATH, false);
            }
            
            GUI.backgroundColor = Color.white;
        }

        private void SwitchContext(string scenePath, string layoutPath, bool autoPlay)
        {
            // 1. Xử lý an toàn: Nếu đang Play, phải tắt Play Mode trước khi chuyển
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("[Switcher] Đang tắt Play Mode. Xin hãy click lại nút sau khi Editor dừng hoàn toàn.");
                return; 
            }

            // 2. Validate: Kiểm tra file Scene có tồn tại không
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Lỗi Đường Dẫn", $"Không tìm thấy Scene tại:\n{scenePath}", "OK");
                return;
            }

            // 3. Hỏi lưu map đang làm dở
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 4. Mở Scene mới
            EditorSceneManager.OpenScene(scenePath);
            
            // 5. Nạp Layout (Nếu có)
            if (File.Exists(layoutPath))
            {
                LoadLayoutSafe(layoutPath);
            }
            else
            {
                Debug.LogWarning($"[Switcher] Bỏ qua đổi Layout vì không tìm thấy file tại: {layoutPath}");
            }

            // 6. Tự động Play
            if (autoPlay)
            {
                EditorApplication.isPlaying = true;
            }
        }

        private void LoadLayoutSafe(string path)
        {
            try
            {
                var assembly = typeof(EditorApplication).Assembly;
                var windowLayoutType = assembly.GetType("UnityEditor.WindowLayout");    

                if (windowLayoutType != null)
                {
                    // FIX TRIỆT ĐỂ AmbiguousMatchException: Định nghĩa rõ tham số (string, bool)
                    var method = windowLayoutType.GetMethod("LoadWindowLayout", 
                        BindingFlags.Public | BindingFlags.Static, 
                        null, 
                        new System.Type[] { typeof(string), typeof(bool) }, 
                        null);

                    if (method != null)
                    {
                        method.Invoke(null, new object[] { path, false });
                    }
                    else
                    {
                        Debug.LogError("[Switcher] Không tìm thấy hàm LoadWindowLayout tương thích.");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Switcher] Lỗi khi nạp Layout: {e.Message}");
            }
        }
    }
}
#endif