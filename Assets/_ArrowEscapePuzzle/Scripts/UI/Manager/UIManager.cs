using System;
using System.Collections.Generic;
using ArrowGame.Interface;
using ArrowGame.UI.Base;
using ArrowGame.UI.Popups;
using ArrowGame.UI.Screens;
using ArrowGame.UI.TopLevels;
using UnityEngine;
using GameCore.Utils.DesignPattern.Singleton; 

namespace ArrowGame.UI.Manager
{
    [Serializable]
    public struct ScreenConfig
    {
        public ScreenID id;
        public BaseScreen prefab; 
    }
    [Serializable]
    public struct PopupConfig
    {
        public PopupID id;
        public BasePopup prefab;
    }

   public class UIManager : Singleton<UIManager>
    {
        [Header("--- UI Roots ---")]
        [SerializeField] private Transform screenRoot; 
        [SerializeField] private Transform popupRoot;  
        [SerializeField] private Transform topRoot;    

        [Header("--- Screen Configs (Layer 1) ---")]
        [SerializeField] private List<ScreenConfig> screenConfigs = new List<ScreenConfig>();

        [Header("--- Popup Configs (Layer 2) ---")]
        [SerializeField] private List<PopupConfig> popupConfigs = new List<PopupConfig>();
        
        [Header("--- Top UI Prefabs (Layer 3) ---")]
        [SerializeField] private ToastNotification toastPrefab;
        [SerializeField] private LoadingScreen loadingScreenPrefab;

        // --- Caches ---
        private Dictionary<ScreenID, BaseScreen> _screenPrefabDict = new Dictionary<ScreenID, BaseScreen>();
        private Dictionary<PopupID, BasePopup> _popupPrefabDict = new Dictionary<PopupID, BasePopup>();
        
        private Dictionary<ScreenID, BaseScreen> _screenCache = new Dictionary<ScreenID, BaseScreen>();
        private Dictionary<PopupID, BasePopup> _popupCache = new Dictionary<PopupID, BasePopup>();
        
        // --- Flow State ---
        private Stack<BasePopup> _popupStack = new Stack<BasePopup>();
        private BaseScreen _currentScreen; 

        private ToastNotification _toastInstance;
        private LoadingScreen _loadingInstance;

        // protected override void Awake()
        // {
        //     base.Awake();
        //     InitPrefabDictionaries();
        // }

        public void Init()
        {
            InitPrefabDictionaries();
            InitTopUI();
        }

        // private void Start()
        // {
        //     InitTopUI();
        // }

        private void InitPrefabDictionaries()
        {
            foreach (var config in popupConfigs)
            {
                if (config.prefab != null && !_popupPrefabDict.ContainsKey(config.id))
                    _popupPrefabDict.Add(config.id, config.prefab);
            }

            foreach (var config in screenConfigs)
            {
                if (config.prefab != null && !_screenPrefabDict.ContainsKey(config.id))
                    _screenPrefabDict.Add(config.id, config.prefab);
            }
        }
        
        private void InitTopUI()
        {
            if (loadingScreenPrefab != null && _loadingInstance == null)
            {
                _loadingInstance = Instantiate(loadingScreenPrefab, topRoot);
                _loadingInstance.gameObject.SetActive(false);
            }

            if (toastPrefab != null && _toastInstance == null)
            {
                _toastInstance = Instantiate(toastPrefab, topRoot);
                _toastInstance.gameObject.SetActive(false);
            }
        }

        
        public T ShowScreen<T>(ScreenID id, Action onOpened = null) where T : BaseScreen
        {
            if (_currentScreen != null && _currentScreen.gameObject.activeInHierarchy)
            {
                _currentScreen.Hide();
            }

            if (!_screenCache.TryGetValue(id, out BaseScreen instance) || instance == null)
            {
                if (!_screenPrefabDict.TryGetValue(id, out BaseScreen prefab))
                {
                    string resourcePath = $"UI/Screens/{id}";
                    prefab = Resources.Load<BaseScreen>(resourcePath);
                    
                    if (prefab == null)
                    {
                        Debug.LogError($"[UIManager] Lỗi: Không tìm thấy Screen Prefab cho ID '{id}'!");
                        return null;
                    }
                    _screenPrefabDict[id] = prefab;
                }

                instance = Instantiate(prefab, screenRoot);
                _screenCache[id] = instance;
            }

            instance.transform.SetAsLastSibling();
            instance.Show(onOpened);
            _currentScreen = instance;

            return instance as T;
        }

        public T ShowPopup<T>(PopupID id, Action onOpened = null) where T : BasePopup
        {
            if (!_popupCache.TryGetValue(id, out BasePopup instance) || instance == null)
            {
                if (!_popupPrefabDict.TryGetValue(id, out BasePopup prefab))
                {
                    string resourcePath = $"UI/Popups/{id}";
                    prefab = Resources.Load<BasePopup>(resourcePath);
                    
                    if (prefab == null)
                    {
                        Debug.LogError($"[UIManager] Lỗi: Không tìm thấy Popup Prefab cho ID '{id}'!");
                        return null;
                    }
                    _popupPrefabDict[id] = prefab;
                }

                instance = Instantiate(prefab, popupRoot);
                _popupCache[id] = instance;
            }

            instance.transform.SetAsLastSibling(); 

            if (_popupStack.Count == 0 || _popupStack.Peek() != instance)
            {
                _popupStack.Push(instance);
            }
            
            instance.Show(onOpened);
            return instance as T;
        }

        public void CloseTopPopup()
        {
            if (_popupStack.Count > 0)
            {
                BasePopup topPopup = _popupStack.Pop();
                if (topPopup != null && topPopup.gameObject.activeInHierarchy)
                {
                    topPopup.Hide(); 
                }
            }
        }

        public void ClearAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                BasePopup popup = _popupStack.Pop();
                if (popup != null) popup.Hide();
            }
        }
        
        public void HideCurrentScreen()
        {
            if (_currentScreen != null && _currentScreen.gameObject.activeInHierarchy)
            {
                _currentScreen.Hide();
                _currentScreen = null;
            }
        }

        public void ShowToast(string message, float duration = -1f)
        {
            if (_toastInstance)
            {
                _toastInstance.transform.SetAsLastSibling();
                _toastInstance.ShowToast(message, duration);
            }
        }

        public void ShowLoading(Action onCovered = null) => _loadingInstance?.ShowLoading(onCovered);
        public void HideLoading() => _loadingInstance?.HideLoading();
    }
}