using System.Collections;
using System.Collections.Generic;
using ArrowGame.Utils; // Namespace chứa LoadingSceneVisual
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArrowGame.Interface
{
    public class AppBootstrap : MonoBehaviour
    {
        [Header("Scene Config")]
        [SerializeField] private string nameInitScene = "LoadingScene";
        [SerializeField] private string nameMainScene = "GameplayScene";
        [SerializeField] private float minLoadingTime = 5f; 
        
        [Header("Visual Reference")]
        [SerializeField] private LoadingSceneVisual loadingVisual; // Kéo object trong scene vào đây

        [Header("Core Services")]
        [SerializeField] private List<MonoBehaviour> coreServices;

        private bool _isFlowRunning = false; 

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == nameInitScene)
            {
                RunInitFlow(isEditorAutoInject: false);
            }
        }

        public void RunInitFlow(bool isEditorAutoInject)
        {
            if (_isFlowRunning) return;
            StartCoroutine(RunInitFlowRoutine(isEditorAutoInject));
        }

        private IEnumerator RunInitFlowRoutine(bool isEditorAutoInject)
        {
            _isFlowRunning = true;
            
            // AppBootstrap cần tồn tại xuyên suốt quá trình load
            DontDestroyOnLoad(gameObject);
    
            float startTime = Time.realtimeSinceStartup;

            // 1. KHỞI TẠO SERVICES (Data, Audio, Settings...)
            foreach (var mono in coreServices)
            {
                if (mono is IAppService service) service.Init();
            }

            yield return null; 

            // 2. XỬ LÝ LOAD SCENE VÀ THỜI GIAN CHỜ
            bool isCurrentlyInInitScene = SceneManager.GetActiveScene().name == nameInitScene;

            if (isCurrentlyInInitScene)
            {
                // Load Scene Gameplay ngầm
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nameMainScene);
                asyncLoad.allowSceneActivation = false;

                // Vòng lặp đợi Scene load xong 90%
                while (asyncLoad.progress < 0.9f) yield return null;

                // Đợi cho đủ thời gian tối thiểu (ví dụ 5s để diễn hết anim chữ nảy)
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed < minLoadingTime)
                {
                    yield return new WaitForSecondsRealtime(minLoadingTime - elapsed);
                }

                // Chuyển Scene
                asyncLoad.allowSceneActivation = true;
                yield return new WaitUntil(() => asyncLoad.isDone);
            }
            else if (isEditorAutoInject)
            {
                Debug.Log("<color=green>[Bootstrap] Editor Mode: Khởi tạo hoàn tất!</color>");
            }
    
            // 3. KẾT THÚC: FADE OUT VÀ TỰ HỦY LOADING VISUAL
            if (loadingVisual != null)
            {
                bool isFadeDone = false;
                // Gọi hàm FadeOut từ LoadingSceneVisual (duration 0.5s chẳng hạn)
                loadingVisual.FadeOutAndDestroy(0.5f, () => {
                    isFadeDone = true;
                });

                // Đợi hiệu ứng mờ dần xong mới kết thúc flow
                yield return new WaitUntil(() => isFadeDone);
            }

            _isFlowRunning = false;
            
            // Sau khi xong hết, AppBootstrap có thể tự hủy hoặc giữ lại tùy kiến trúc của ông
            // Destroy(gameObject); 
        }
    }
}