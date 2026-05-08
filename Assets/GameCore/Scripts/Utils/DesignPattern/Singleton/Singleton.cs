using UnityEngine;

namespace GameCore.Utils.DesignPattern.Singleton
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object Lock = new object();
        
        private static bool _applicationIsQuitting = false;

        public static bool HasInstance => _instance != null;
        public static bool DontDestroyOnLoadEnabled { get; set; } = true;

         public static T Instance
        {
            get
            {
                // Tránh tạo "Ghost Object" khi Editor đang tắt
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                lock (Lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)Object.FindAnyObjectByType(typeof(T));

                        if (Object.FindObjectsByType(typeof(T), FindObjectsSortMode.None).Length > 1)
                        {
                            Debug.LogError($"[Singleton] Something went really wrong - there are two instances of {typeof(T)}");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "[Singleton] " + typeof(T);

                            if (DontDestroyOnLoadEnabled)
                                DontDestroyOnLoad(singleton);
                        }
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_applicationIsQuitting) return;

            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = this as T;
                    if (DontDestroyOnLoadEnabled && transform.parent == null)
                    {
                        DontDestroyOnLoad(gameObject);
                    }
                }
                else if (_instance != this)
                {
                    Destroy(gameObject);
                }
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        public virtual void KeepAlive(bool alive)
        {
            DontDestroyOnLoadEnabled = alive;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _applicationIsQuitting = true; 
                lock (Lock)
                {
                    _instance = null;
                }
            }
        }
    }
}