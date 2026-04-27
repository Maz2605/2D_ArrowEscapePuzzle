using System;
using GameCore.UI.Base;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;

namespace GameCore.UI.Manager
{
    public class UIManager : Singleton<UIManager>
    {
        [Header("--- Canvas Config ---")]
        [SerializeField] private Transform globalCanvasRoot; 

        [Header("--- Prefabs ---")]
        [SerializeField] private SettingBase settingsPrefab; 
        [SerializeField] private ConfirmationPopup confirmationPrefab;
        [SerializeField] private ToastNotification toastPrefab;
        [SerializeField] private LoadingScreen loadingScreenPrefab;

        // --- Instances (Biến lưu bản cached) ---
        private SettingBase _settingInstance;
        private ConfirmationPopup _confirmInstance;
        private ToastNotification _toastInstance;
        private LoadingScreen _loadingInstance;

        // Lưu trạng thái
        private float _savedTimeScale = 1f;
        private Action _onSettingsCloseCallback; 
        
        private void Start()
        {
            InitUI();
        }

        private void InitUI()
        {
            if (loadingScreenPrefab != null && _loadingInstance == null)
            {
                _loadingInstance = Instantiate(loadingScreenPrefab, globalCanvasRoot);
                _loadingInstance.transform.SetAsLastSibling();
                _loadingInstance.gameObject.SetActive(false);
            }
        }

        // SETTINGS SYSTEM 
        
        public void OpenSettings(Action onClose = null)
        {
            if (_settingInstance == null)
            {
                if (settingsPrefab == null)
                {
                    Debug.LogError("UIManager: Chưa gắn Settings Prefab!");
                    return;
                }
                _settingInstance = Instantiate(settingsPrefab, globalCanvasRoot);
            }

            _onSettingsCloseCallback = onClose;

            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            _settingInstance.OnClose.RemoveListener(OnSettingsClosed); 
            _settingInstance.OnClose.AddListener(OnSettingsClosed);

            _settingInstance.Show();
        }

        private void OnSettingsClosed()
        { 
            Time.timeScale = _savedTimeScale;
            _onSettingsCloseCallback?.Invoke();
            _onSettingsCloseCallback = null;
        }

        // CONFIRMATION SYSTEM
        public void ShowConfirmation(string title, string message, Action onConfirm, Action onCancel, string confirmLabel = null, string cancelLabel = null)
        {
            if (_confirmInstance == null)
            {
                if (confirmationPrefab == null) return;
                _confirmInstance = Instantiate(confirmationPrefab, globalCanvasRoot);
            }
            float pauseTime = Time.timeScale;
            Time.timeScale = 0f;
            
            Action wrapperConfirm = () =>
            {
                Time.timeScale = pauseTime;
                onConfirm?.Invoke();
            };
            Action wrapperCancel = () =>
            {
                Time.timeScale = pauseTime;
                onCancel?.Invoke();
            };
            
            _confirmInstance.Setup(title, message, wrapperConfirm, wrapperCancel, confirmLabel, cancelLabel);
            _confirmInstance.Show();
        }

        // TOAST SYSTEM
        public void ShowToast(string message, float duration = -1f)
        {
            if (_toastInstance == null)
            {
                if (toastPrefab == null) return;
                _toastInstance = Instantiate(toastPrefab, globalCanvasRoot);
                _toastInstance.transform.SetAsLastSibling(); 
            }

            _toastInstance.ShowToast(message, duration);
        }

        // LOADING SYSTEM
        public void ShowLoading(Action onCovered = null)
        {
            if (_loadingInstance == null) InitUI();
    
            if (_loadingInstance != null)
            {
                _loadingInstance.ShowLoading(onCovered);
                return;
            }

            onCovered?.Invoke();
        }

        public void HideLoading()
        {
            if (_loadingInstance != null)
            {
                _loadingInstance.HideLoading();
            }
        }
        
        public void ToggleLoading(bool isShow)
        {
            if (isShow) ShowLoading();
            else HideLoading();
        }
    }
}
