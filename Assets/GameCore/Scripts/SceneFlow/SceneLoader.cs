using System;
using System.Collections;
using GameCore.UI.Manager;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.SceneFlow
{
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Header("Config")]
        [SerializeField] private float minLoadingTime = 1f;
        
        public void LoadScene(string sceneName, Action onSceneLoaded = null)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, onSceneLoaded));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, Action onSceneLoaded)
        {
            Time.timeScale = 1f;

            bool isCovered = false;
            UIManager.Instance.ShowLoading(() => isCovered = true);
            
            yield return new WaitUntil(() => isCovered);

            
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
            
            float startTime = Time.unscaledTime; 
            
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
            {
                yield return null;
            }
            
            float elapsedTime = Time.unscaledTime - startTime;
            
            if (elapsedTime < minLoadingTime)
            {
                yield return new WaitForSecondsRealtime(minLoadingTime - elapsedTime);
            }

            
            op.allowSceneActivation = true;

            while (!op.isDone) yield return null;

            onSceneLoaded?.Invoke();

            yield return null; 

            UIManager.Instance.HideLoading();
        }
    }
}