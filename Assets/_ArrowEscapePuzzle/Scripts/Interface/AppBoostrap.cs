using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArrowGame.Interface
{
    public class AppBootstrap : MonoBehaviour
    {
        
        [SerializeField] private string nameInitScene = "LoadingScene";
        [SerializeField] private string nameMainScene = "GameplayScene";
        [SerializeField] private List<MonoBehaviour> coreServices;
        private void Start()
        {
            if (SceneManager.GetActiveScene().name == nameInitScene)
            {
                RunInitFlow(isEditorAutoInject: false);
            }
        }

        public void RunInitFlow(bool isEditorAutoInject)
        {
            DontDestroyOnLoad(gameObject);

            foreach (var mono in coreServices)
            {
                if (mono is IAppService service)
                {
                    service.Init();
                }
                else if (mono != null)
                {
                    Debug.LogWarning($"[Bootstrap] Thằng {mono.name} không có interface IAppService nên bị bỏ qua!");
                }
            }

            if (!isEditorAutoInject)
            {
                SceneManager.LoadSceneAsync(nameMainScene);
            }
        }
    }
}