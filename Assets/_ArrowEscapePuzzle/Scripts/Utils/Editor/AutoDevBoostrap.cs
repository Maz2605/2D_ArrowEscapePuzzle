#if UNITY_EDITOR
using ArrowGame.Interface;
using UnityEngine;

namespace ArrowGame.Utils.Editor
{
    public static class AutoDevBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InjectCoreOnPlay()
        {
            if (Object.FindAnyObjectByType<AppBootstrap>() != null) return;

            Debug.Log("<color=yellow>⚙️ [AutoDevBootstrap] Đang tự động tiêm Core Prefab vào Scene hiện tại...</color>");

            GameObject prefab = Resources.Load<GameObject>("Core/AppBootstrap");
            
            if (prefab == null)
            {
                Debug.LogError("❌ KHÔNG TÌM THẤY PREFAB BOOTSTRAP! Kiểm tra lại thư mục Resources/Core/");
                return;
            }

            GameObject instance = Object.Instantiate(prefab);
            instance.name = "[AUTO_BOOTSTRAP_INJECTED]";

            instance.GetComponent<AppBootstrap>().RunInitFlow(isEditorAutoInject: true);
        }
    }
}
#endif