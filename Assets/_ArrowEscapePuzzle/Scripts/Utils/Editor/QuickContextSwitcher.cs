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
        private const string GAME_SCENE_PATH = "Assets/_ArrowEscapePuzzle/Scenes/Gameplay.unity";
        private const string EDITOR_SCENE_PATH = "Assets/_EditorTool/Scenes/EditorScene.unity";
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
                SwitchContext(GAME_SCENE_PATH, false);
            }

            EditorGUILayout.Space(5);

            // Nút về Editor
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.9f);
            if (GUILayout.Button("🛠 VỀ EDITOR SCENE (STOP)", GUILayout.Height(40)))
            {
                SwitchContext(EDITOR_SCENE_PATH, false);
            }

            GUI.backgroundColor = Color.white;
        }
        
        [MenuItem("Tools/ Switcher _F12")]
        public static void ToggleScene()
        {
            if (string.IsNullOrEmpty(GAME_SCENE_PATH) || string.IsNullOrEmpty(EDITOR_SCENE_PATH))
            {
                Debug.LogError(
                    "[Scene Switcher] Đường dẫn Scene đang trống. Vui lòng mở file script 'SceneSwitcherWindow.cs' để cấu hình lại.");
                return;
            }

            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            string targetPath = (currentScenePath == GAME_SCENE_PATH) ? EDITOR_SCENE_PATH : GAME_SCENE_PATH;

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath);
            if (sceneAsset == null)
            {
                Debug.LogError(
                    $"[Scene Switcher] KHÔNG tìm thấy Scene tại đường dẫn: \"{targetPath}\". Bạn hãy kiểm tra lại chính tả hoặc Copy Path lại vào code nhé!");
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(targetPath);
            }
        }
        
        private void SwitchContext(string scenePath, bool autoPlay)
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("[Switcher] Đang tắt Play Mode. Xin hãy click lại nút sau khi Editor dừng hoàn toàn.");
                return;
            }
            
            if (!File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Lỗi Đường Dẫn", $"Không tìm thấy Scene tại:\n{scenePath}", "OK");
                return;
            }
            
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(scenePath);

            if (autoPlay)
            {
                EditorApplication.isPlaying = true;
            }
        }
    }
}
#endif