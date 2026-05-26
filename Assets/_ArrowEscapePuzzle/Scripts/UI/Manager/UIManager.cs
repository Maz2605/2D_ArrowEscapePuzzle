using System;
using System.Collections.Generic;
using ArrowGame.Interface;
using ArrowGame.UI.Base;
using ArrowGame.UI.Popups;
using ArrowGame.UI.Screens;
using ArrowGame.UI.TopLevels;
using UnityEngine;
using GameCore.Utils.DesignPattern.Singleton; 
using DG.Tweening;

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
        public Transform TopRoot => topRoot;

        [Header("--- Screen Configs (Layer 1) ---")]
        [SerializeField] private List<ScreenConfig> screenConfigs = new List<ScreenConfig>();

        [Header("--- Popup Configs (Layer 2) ---")]
        [SerializeField] private List<PopupConfig> popupConfigs = new List<PopupConfig>();
        
        [Header("--- Top UI Prefabs (Layer 3) ---")]
        [SerializeField] private ToastNotification toastPrefab;
        [SerializeField] private LoadingScreen loadingScreenPrefab;
        [SerializeField] private GameObject tapAuraPrefab;
        [SerializeField] private ArrowGame.Gameplay.Visual.ScreenFlashVFX screenFlashPrefab;

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
        private ArrowGame.Gameplay.Visual.ScreenFlashVFX _screenFlashInstance;
        public bool IsLoadingVisible => _loadingInstance != null && _loadingInstance.gameObject.activeInHierarchy;

        private void OnEnable()
        {
            if (ArrowGame.Gameplay.Managers.InputManager.Instance != null)
            {
                ArrowGame.Gameplay.Managers.InputManager.Instance.OnDiscreteTap += HandleDiscreteTap;
            }
            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.AddListener<ArrowGame.Data.VFX.TapVFXPayload>(ArrowGame.Data.Events.VisualEventID.PlayTapAuraVFX, HandleTapAuraVFX);
            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.AddListener<Vector3>(ArrowGame.Data.Events.VisualEventID.ArrowWrongImpact, PlayBlockedFlash);
        }

        private void OnDisable()
        {
            if (ArrowGame.Gameplay.Managers.InputManager.Instance != null)
            {
                ArrowGame.Gameplay.Managers.InputManager.Instance.OnDiscreteTap -= HandleDiscreteTap;
            }
            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.RemoveListener<ArrowGame.Data.VFX.TapVFXPayload>(ArrowGame.Data.Events.VisualEventID.PlayTapAuraVFX, HandleTapAuraVFX);
            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.RemoveListener<Vector3>(ArrowGame.Data.Events.VisualEventID.ArrowWrongImpact, PlayBlockedFlash);
        }

        private void HandleDiscreteTap(Vector2 screenPos)
        {
            HandleTapAuraVFX(new ArrowGame.Data.VFX.TapVFXPayload { ScreenPosition = screenPos });
        }

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

            if (screenFlashPrefab != null && _screenFlashInstance == null)
            {
                _screenFlashInstance = Instantiate(screenFlashPrefab, topRoot);
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
        public void HideLoading(Action onHidden = null) => _loadingInstance?.HideLoading(onHidden);

        private void HandleTapAuraVFX(ArrowGame.Data.VFX.TapVFXPayload payload)
        {
            if (tapAuraPrefab == null || topRoot == null) return;

            Vector2 localPoint;
            
            Canvas canvas = topRoot.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                cam = canvas.worldCamera;
                if (cam == null) cam = Camera.main;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                topRoot.GetComponent<RectTransform>(), 
                payload.ScreenPosition, 
                cam, 
                out localPoint);

            GameObject aura = GameCore.Utils.DesignPattern.ObjectPooling.PoolingManager.Instance.Spawn(tapAuraPrefab, Vector3.zero, Quaternion.identity, topRoot);
            RectTransform rt = aura.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
            
            // Su dung Despawn cua PoolingManager sau khi hieu ung ket thuc
            DOVirtual.DelayedCall(1.5f, () => {
                if (aura != null) GameCore.Utils.DesignPattern.ObjectPooling.PoolingManager.Instance.Despawn(aura);
            }).SetUpdate(true);
        }

        private void PlayBlockedFlash(Vector3 impactPosition)
        {
            if (_screenFlashInstance != null)
            {
                _screenFlashInstance.PlayImpact();
            }
        }
    }
}
