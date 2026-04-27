using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArrowGame.Interface
{
    [DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        [Header("Scene Config")]
        [SerializeField] private string initSceneName = "Loading";
        [SerializeField] private string mainSceneName = "Gameplay";
        [SerializeField] private float minLoadingTime = 1f;

        [Header("Core Services")]
        [SerializeField] private List<MonoBehaviour> coreServices = new();

        private static AppBootstrap _instance;
        private static bool _servicesInitialized;

        private Coroutine _bootstrapRoutine;

        private void Awake()
        {
            if (!TryBecomePrimaryInstance())
            {
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (IsInitSceneActive())
            {
                RunInitFlow(isEditorAutoInject: false);
            }
        }

        public void RunInitFlow(bool isEditorAutoInject)
        {
            if (_bootstrapRoutine != null)
            {
                return;
            }

            _bootstrapRoutine = StartCoroutine(RunBootstrapRoutine(isEditorAutoInject));
        }

        private IEnumerator RunBootstrapRoutine(bool isEditorAutoInject)
        {
            InitializeServicesOnce();
            yield return null;

            if (!ShouldLoadMainScene(isEditorAutoInject))
            {
                _bootstrapRoutine = null;
                yield break;
            }

            yield return LoadMainSceneRoutine();

            _bootstrapRoutine = null;
        }

        private bool TryBecomePrimaryInstance()
        {
            if (_instance == null)
            {
                _instance = this;
                return true;
            }

            if (_instance == this)
            {
                return true;
            }

            Destroy(gameObject);
            return false;
        }

        private void InitializeServicesOnce()
        {
            if (_servicesInitialized)
            {
                return;
            }

            HashSet<MonoBehaviour> initializedTargets = new();

            foreach (MonoBehaviour target in coreServices)
            {
                if (target == null || target == this || !initializedTargets.Add(target))
                {
                    continue;
                }

                if (target is IAppService service)
                {
                    service.Init();
                    continue;
                }

                Debug.LogWarning($"[AppBootstrap] {target.name} does not implement {nameof(IAppService)}.", target);
            }

            _servicesInitialized = true;
        }

        private bool ShouldLoadMainScene(bool isEditorAutoInject)
        {
            string activeSceneName = SceneManager.GetActiveScene().name;

            if (activeSceneName == mainSceneName)
            {
                return false;
            }

            if (activeSceneName == initSceneName)
            {
                return true;
            }

            return !isEditorAutoInject;
        }

        private IEnumerator LoadMainSceneRoutine()
        {
            Time.timeScale = 1f;

            yield return Resources.UnloadUnusedAssets();

            float startTime = Time.unscaledTime;
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mainSceneName);

            if (loadOperation == null)
            {
                Debug.LogError($"[AppBootstrap] Failed to load scene '{mainSceneName}'.");
                yield break;
            }

            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < 0.9f)
            {
                yield return null;
            }

            float elapsedTime = Time.unscaledTime - startTime;
            if (elapsedTime < minLoadingTime)
            {
                yield return new WaitForSecondsRealtime(minLoadingTime - elapsedTime);
            }

            loadOperation.allowSceneActivation = true;

            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }

        private bool IsInitSceneActive()
        {
            return SceneManager.GetActiveScene().name == initSceneName;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
