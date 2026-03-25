using System;
using System.Collections.Generic;
using ArrowGame.UI.Base;
using ArrowGame.UI.Popups;
using ArrowGame.UI.TopLevel;
using UnityEngine;
using GameCore.Utils.DesignPattern.Singleton; 

namespace ArrowGame.UI.Manager
{
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

        [Header("--- Popup Configs (Hybrid) ---")]
        [Tooltip("Kéo thả Prefab vào đây. Code sẽ parse ra Dictionary để lookup O(1)")]
        [SerializeField] private List<PopupConfig> popupConfigs = new List<PopupConfig>();
        
        [Header("--- Top UI Prefabs ---")]
        [SerializeField] private ToastNotification toastPrefab;
        [SerializeField] private LoadingScreen loadingScreenPrefab;

        // --- Cache & Flow ---
        private Dictionary<PopupID, BasePopup> _prefabDict = new Dictionary<PopupID, BasePopup>();
        private Dictionary<PopupID, BasePopup> _popupCache = new Dictionary<PopupID, BasePopup>();
        private Stack<BasePopup> _popupStack = new Stack<BasePopup>();

        private ToastNotification _toastInstance;
        private LoadingScreen _loadingInstance;

        protected override void Awake()
        {
            base.Awake();
            InitPrefabDictionary();
        }

        private void Start()
        {
            InitTopUI();
        }

        // Tối ưu mục 2: Chuyển List thành Dictionary để lookup O(1)
        private void InitPrefabDictionary()
        {
            foreach (var config in popupConfigs)
            {
                if (config.prefab != null && !_prefabDict.ContainsKey(config.id))
                {
                    _prefabDict.Add(config.id, config.prefab);
                }
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

        public T ShowPopup<T>(PopupID id, Action onOpened = null) where T : BasePopup
        {
            if (!_popupCache.TryGetValue(id, out BasePopup instance) || instance == null)
            {
                // Tìm prefab trong Dictionary đã cache O(1)
                if (!_prefabDict.TryGetValue(id, out BasePopup prefab))
                {
                    // Giữ nguyên logic Resources.Load theo ý bạn
                    string resourcePath = $"UI/Popups/{id.ToString()}";
                    T loadedPrefab = Resources.Load<T>(resourcePath);
                    
                    if (loadedPrefab == null)
                    {
                        Debug.LogError($"[UIManager] Lỗi: Không tìm thấy Prefab cho ID '{id}'!");
                        return null;
                    }
                    prefab = loadedPrefab;
                }

                instance = Instantiate(prefab, popupRoot);
                _popupCache[id] = instance;
            }

            instance.transform.SetAsLastSibling(); 

            // Tối ưu mục 4: Chặn Push đúp vào Stack nếu user spam click
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