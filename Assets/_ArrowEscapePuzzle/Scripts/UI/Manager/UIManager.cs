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
        [Tooltip("Kéo thả Prefab vào đây. Nếu để trống, code sẽ tự tìm trong Resources/UI/Popups/Tên_Enum")]
        [SerializeField] private List<PopupConfig> popupConfigs = new List<PopupConfig>();
        
        [Header("--- Top UI Prefabs ---")]
        [SerializeField] private ToastNotification toastPrefab;
        [SerializeField] private LoadingScreen loadingScreenPrefab;

        // --- Cache & Flow ---
        private Dictionary<PopupID, BasePopup> _popupCache = new Dictionary<PopupID, BasePopup>();
        private Stack<BasePopup> _popupStack = new Stack<BasePopup>();

        private ToastNotification _toastInstance;
        private LoadingScreen _loadingInstance;

        private void Start()
        {
            InitTopUI();
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
                BasePopup prefab = null;

                int index = popupConfigs.FindIndex(x => x.id == id);
                if (index >= 0)
                {
                    prefab = popupConfigs[index].prefab;
                }

                if (prefab == null)
                {
                    string resourcePath = $"UI/Popups/{id.ToString()}";
                    T loadedPrefab = Resources.Load<T>(resourcePath);
                    
                    if (loadedPrefab == null)
                    {
                        Debug.LogError($"[UIManager] Lỗi: Không tìm thấy Prefab cho ID '{id}'! Hãy kiểm tra Inspector hoặc thư mục Resources/{resourcePath}");
                        return null;
                    }
                    prefab = loadedPrefab;
                }

                instance = Instantiate(prefab, popupRoot);
                _popupCache[id] = instance;
            }

            instance.transform.SetAsLastSibling(); 
            _popupStack.Push(instance);
            instance.Show(onOpened);

            return instance as T;
        }

        public void CloseTopPopup()
        {
            if (_popupStack.Count > 0)
            {
                BasePopup topPopup = _popupStack.Pop();
                topPopup.Hide();
            }
        }

        public void ClearAllPopups()
        {
            while (_popupStack.Count > 0)
            {
                BasePopup popup = _popupStack.Pop();
                popup.Hide();
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

        public void ShowLoading(Action onCovered = null)
        {
            if (_loadingInstance)
            {
                _loadingInstance.transform.SetAsLastSibling();
                _loadingInstance.ShowLoading(onCovered);
            }
        }

        public void HideLoading() => _loadingInstance?.HideLoading();
    }
}