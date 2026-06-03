#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArrowGame.UI.Popups;

namespace ArrowGame.Utils.Editor
{
    [InitializeOnLoad]
    public static class TutorialOverlayCreator
    {
        static TutorialOverlayCreator()
        {
            string prefabPath = "Assets/Resources/UI/Popups/TutorialOverlay.prefab";
            if (!System.IO.File.Exists(prefabPath))
            {
                EditorApplication.delayCall += CreatePrefab;
            }
        }

        [MenuItem("Tools/Create Tutorial Overlay Prefab")]
        public static void CreatePrefab()
        {
            string prefabPath = "Assets/Resources/UI/Popups/TutorialOverlay.prefab";
            if (System.IO.File.Exists(prefabPath))
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            // 1. Khởi tạo root GameObject
            GameObject root = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(TutorialOverlayUI));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;
            rootRt.anchoredPosition = Vector2.zero;

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            TutorialOverlayUI ui = root.GetComponent<TutorialOverlayUI>();

            // 2. Tạo Dimmer Image phủ màn hình sử dụng Shader đục lỗ vòng tròn
            GameObject dim = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(root.transform, false);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.sizeDelta = Vector2.zero;
            dimRt.anchoredPosition = Vector2.zero;
            Image dimImg = dim.GetComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.6f);
            dimImg.raycastTarget = true;

            // Thiết lập shader và tạo material
            string dirPath = "Assets/Resources/UI/Popups";
            if (!System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.CreateDirectory(dirPath);
            }
            Shader cutoutShader = Shader.Find("UI/CircleCutout");
            if (cutoutShader != null)
            {
                Material mat = new Material(cutoutShader);
                string matPath = dirPath + "/UI-CircleCutout.mat";
                AssetDatabase.CreateAsset(mat, matPath);
                dimImg.material = mat;
            }
            else
            {
                Debug.LogWarning("[TutorialOverlayCreator] Không tìm thấy Shader 'UI/CircleCutout'!");
            }

            // 3. Tạo Dialog Container chứa lời thoại
            GameObject dialog = new GameObject("DialogContainer", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(root.transform, false);
            RectTransform dialogRt = dialog.GetComponent<RectTransform>();
            dialogRt.anchorMin = new Vector2(0f, 0f);
            dialogRt.anchorMax = new Vector2(1f, 0.25f); // 25% chiều cao màn hình ở đáy
            dialogRt.sizeDelta = new Vector2(-40f, -40f); // Margin
            dialogRt.anchoredPosition = new Vector2(0f, 20f);
            Image dialogImg = dialog.GetComponent<Image>();
            dialogImg.color = new Color(0f, 0f, 0f, 0.8f);

            // 4. Tạo Tooltip Text (TextMeshPro)
            GameObject textObj = new GameObject("TooltipText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(dialog.transform, false);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = new Vector2(-20f, -20f);
            textRt.anchoredPosition = Vector2.zero;
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = "Lời dẫn hướng dẫn...";

            // 5. Tạo Ghost Hand hiển thị bàn tay chỉ dẫn
            GameObject hand = new GameObject("GhostHand", typeof(RectTransform), typeof(Image));
            hand.transform.SetParent(root.transform, false);
            RectTransform handRt = hand.GetComponent<RectTransform>();
            handRt.sizeDelta = new Vector2(100f, 100f);
            Image handImg = hand.GetComponent<Image>();
            handImg.raycastTarget = false;

            // Liên kết các reference vào các trường Serialized của script
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("dimImage").objectReferenceValue = dimImg;
            so.FindProperty("dialogContainer").objectReferenceValue = dialogRt;
            so.FindProperty("panelContainer").objectReferenceValue = dialogRt;
            so.FindProperty("txtTooltip").objectReferenceValue = tmp;
            so.FindProperty("handTransform").objectReferenceValue = handRt;
            so.FindProperty("handImage").objectReferenceValue = handImg;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            
            // 1 slot cho hình ảnh bàn tay để trống cho người chơi tự kéo sprite
            so.FindProperty("handSprite").objectReferenceValue = null;
            
            so.ApplyModifiedProperties();

            // 6. Lưu thành file Prefab
            string fullPrefabPath = dirPath + "/TutorialOverlay.prefab";
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, fullPrefabPath);

            // Hủy đối tượng tạm ở scene
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>[TutorialOverlayCreator] Đã tự động sinh Prefab TutorialOverlay thành công tại: {fullPrefabPath}</color>");
        }

        private static GameObject CreateDimPanel(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Image img = obj.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            img.raycastTarget = true;
            return obj;
        }

        [MenuItem("Tools/Debug/Reset Level Progress to 1")]
        public static void ResetLevelProgress()
        {
            var profile = GameCore.Data.SaveSystem.Load<ArrowGame.Data.UserProfile>("PlayerData");
            if (profile == null)
            {
                profile = new ArrowGame.Data.UserProfile();
            }
            profile.CurrentLevelIndex = 1;
            if (profile.CompletedTutorials != null)
            {
                profile.CompletedTutorials.Clear();
            }
            GameCore.Data.SaveSystem.Save("PlayerData", profile);

            if (Application.isPlaying && ArrowGame.Gameplay.Managers.DataManager.Instance != null)
            {
                ArrowGame.Gameplay.Managers.DataManager.Instance.ForceReloadData();
            }
            
            Debug.Log("<color=green>[TutorialOverlayCreator] Đã reset tiến trình game về Level 1 thành công!</color>");
        }
    }
}
#endif
