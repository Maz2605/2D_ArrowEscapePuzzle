using UnityEngine;

namespace GameCore.Utils.DesignPattern.Singleton
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        
        private static bool _applicationIsQuitting = false;

        public static bool HasInstance => _instance != null;
        public static bool DontDestroyOnLoadEnabled { get; set; } = true;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Debug.LogError("[Singleton] There is more than one singleton of type " + typeof(T));
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            GameObject singleton = new GameObject();
                            _instance = singleton.AddComponent<T>();
                            singleton.name = "[Singleton] " + typeof(T);

                            if (DontDestroyOnLoadEnabled)
                                DontDestroyOnLoad(singleton);
                            
                            Debug.Log($"[Singleton] An instance of {typeof(T)} is needed in the scene, so '{singleton}' was created.");
                        }
                    }
                }

                return _instance;
            }
            set
            {
                lock (_lock)
                {
                    if (_instance != null) _instance = value;
                }
            }
        }

        public virtual void KeepAlive(bool alive)
        {
            DontDestroyOnLoadEnabled = alive;
        }

        protected virtual void Awake()
        {
            if (_applicationIsQuitting) return;

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = this as T;
                    if (DontDestroyOnLoadEnabled)
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
        
        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _applicationIsQuitting = true; 
                lock (_lock)
                {
                    _instance = null;
                }
            }
        }
    }
}