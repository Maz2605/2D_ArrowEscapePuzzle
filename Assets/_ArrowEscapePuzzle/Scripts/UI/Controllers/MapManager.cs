using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Components;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Controllers
{
    public class MapManager : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform contentPanel;
        
        [Header("--- Prefabs ---")]
        [SerializeField] private LevelNode levelNodePrefab;
        [SerializeField] private RectTransform linePrefab; 

        [Header("--- Layout Config ---")]
        [SerializeField] private int totalLevels = 50; 
        [SerializeField] private float spacingY = 250f;     
        [SerializeField] private float bottomPadding = 150f; 

        [Header("--- Level Visibility ---")]
        [Tooltip("Số level bị khóa tiếp theo được phép hiển thị trên map")]
        [SerializeField] private int maxPeekLevels = 5; 

        [Header("--- Auto Scroll ---")]
        [SerializeField] private float scrollDuration = 1f;

        private Dictionary<int, LevelNode> _activeNodes = new Dictionary<int, LevelNode>();
        private Dictionary<int, RectTransform> _activeLines = new Dictionary<int, RectTransform>();

        private void Start()
        {
            SetupContentPanel();
            
            // Cập nhật giới hạn khung kéo ngay từ đầu
            UpdateContentHeight();

            if (PoolingManager.HasInstance)
            {
                PoolingManager.Instance.Prewarm(levelNodePrefab.gameObject, 12);
                PoolingManager.Instance.Prewarm(linePrefab.gameObject, 12);
            }

            scrollView.onValueChanged.AddListener((vec) => UpdateMapCulling());

            Canvas.ForceUpdateCanvases();
        }

        private void OnEnable()
        {
            if (scrollView != null) scrollView.enabled = true;
        
            UpdateContentHeight();
            RefreshMapData();
            
            Canvas.ForceUpdateCanvases();
        }

        private void SetupContentPanel()
        {
            if (contentPanel == null) return;
            contentPanel.anchorMin = new Vector2(0.5f, 0);
            contentPanel.anchorMax = new Vector2(0.5f, 0);
            contentPanel.pivot = new Vector2(0.5f, 0);
            contentPanel.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Tính toán và giới hạn chiều cao của ContentPanel để khóa Scroll
        /// </summary>
        private void UpdateContentHeight()
        {
            if (contentPanel == null) return;

            int currentUnlockedLevel = DataManager.Instance != null ? DataManager.Instance.GetCurrentLevel() : 1;
            
            // Tìm level cao nhất được phép hiển thị
            int maxVisibleLevel = Mathf.Min(totalLevels, currentUnlockedLevel + maxPeekLevels);

            // Tính toán chiều cao dựa trên số level được phép xem
            float dynamicHeight = bottomPadding + (maxVisibleLevel * spacingY) + bottomPadding;

            contentPanel.sizeDelta = new Vector2(contentPanel.sizeDelta.x, dynamicHeight);
        }

        private Vector2 GetNodePosition(int levelIndex)
        {
            int zeroBased = levelIndex - 1;
            float posY = bottomPadding + (zeroBased * spacingY);

            // Cố định X = 0 để tạo thành bản đồ thẳng đứng
            return new Vector2(0, posY);
        }

        private void UpdateMapCulling()
        {
            float contentY = contentPanel.anchoredPosition.y; 
            float viewHeight = viewport.rect.height;
            float buffer = spacingY * 1.5f;
            
            float visibleLocalMinY = -contentY; 
            float visibleLocalMaxY = -contentY + viewHeight;

            float visibleMinY = visibleLocalMinY - buffer;
            float visibleMaxY = visibleLocalMaxY + buffer;

            int currentUnlockedLevel = DataManager.Instance != null ? DataManager.Instance.GetCurrentLevel() : 1;
            int maxVisibleLevel = Mathf.Min(totalLevels, currentUnlockedLevel + maxPeekLevels);

            int minIndex = Mathf.Max(1, Mathf.FloorToInt((visibleMinY - bottomPadding) / spacingY));
            int maxIndex = Mathf.Min(maxVisibleLevel, Mathf.CeilToInt((visibleMaxY - bottomPadding) / spacingY) + 1);

            // Thu hồi Node nằm ngoài vùng nhìn
            List<int> outOfBounds = new List<int>();
            foreach (var kvp in _activeNodes)
            {
                if (kvp.Key < minIndex || kvp.Key > maxIndex)
                {
                    PoolingManager.Instance.Despawn(kvp.Value.gameObject);
                    outOfBounds.Add(kvp.Key);
                }
            }
            foreach (var key in outOfBounds) _activeNodes.Remove(key);

            // Thu hồi Line nằm ngoài vùng nhìn
            List<int> linesOutOfBounds = new List<int>();
            foreach (var kvp in _activeLines)
            {
                if (kvp.Key < minIndex || kvp.Key > maxIndex)
                {
                    PoolingManager.Instance.Despawn(kvp.Value.gameObject);
                    linesOutOfBounds.Add(kvp.Key);
                }
            }
            foreach (var key in linesOutOfBounds) _activeLines.Remove(key);

            // Spawn Node & Line mới
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (!_activeNodes.ContainsKey(i))
                {
                    LevelNode node = PoolingManager.Instance.Spawn<LevelNode>(levelNodePrefab, Vector3.zero, Quaternion.identity, contentPanel);
                    node.transform.localScale = Vector3.one;
                    node.GetComponent<RectTransform>().anchoredPosition = GetNodePosition(i);

                    LevelNodeState state = (i < currentUnlockedLevel) ? LevelNodeState.Passed :
                                         (i == currentUnlockedLevel) ? LevelNodeState.Current : LevelNodeState.Locked;
                    int stars = DataManager.Instance != null ? DataManager.Instance.GetLevelStars(i) : 0;
                    
                    node.SetupNode(i, state, stars, OnLevelClicked);
                    node.transform.SetAsLastSibling(); 
                    _activeNodes.Add(i, node);
                }

                // Không spawn Line cho level cuối cùng được phép hiển thị
                if (i < maxVisibleLevel && !_activeLines.ContainsKey(i))
                {
                    RectTransform line = PoolingManager.Instance.Spawn<RectTransform>(linePrefab, Vector3.zero, Quaternion.identity, contentPanel);
                    line.localScale = Vector3.one;

                    Vector2 posA = GetNodePosition(i);
                    Vector2 posB = GetNodePosition(i + 1);
                    Vector2 dir = posB - posA;

                    line.anchoredPosition = posA;
                    // Độ dày Line mặc định là 20f, có thể chỉnh lại cho phù hợp với UI của bạn
                    line.sizeDelta = new Vector2(dir.magnitude, 20f); 
                    line.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                    line.SetAsFirstSibling(); 
                    
                    _activeLines.Add(i, line);
                }
            }
        }

        public void FocusOnCurrentLevel()
        {
            if (scrollView == null || contentPanel == null || viewport == null) return;

            int currentLevel = DataManager.Instance != null ? DataManager.Instance.GetCurrentLevel() : 1;
            float targetPosY = bottomPadding + (currentLevel - 1) * spacingY;

            float vpHeight = viewport.rect.height;
            float contentHeight = contentPanel.rect.height;
            float maxScrollY = Mathf.Max(0, contentHeight - vpHeight);

            float endY = (vpHeight / 2f) - targetPosY;
            endY = Mathf.Clamp(endY, -maxScrollY, 0);

            scrollView.enabled = false;
            contentPanel.DOAnchorPosY(endY, scrollDuration)
                .SetEase(Ease.OutCubic)
                .OnUpdate(() => UpdateMapCulling()) 
                .OnComplete(() => scrollView.enabled = true);
        }

        private void OnLevelClicked(int levelIndex)
        {
            Debug.Log($"[MapManager] Xác nhận chọn chơi màn {levelIndex}...");

            if (DataManager.Instance != null && !DataManager.Instance.CanStartLevel())
            {
                UIManager.Instance.ShowPopup<BasePopup>(PopupID.OutOfEnergyPopup);
                return;
            }

            if (DataManager.Instance != null) 
            {
                DataManager.Instance.SelectedLevelIndex = levelIndex; 
            }

            scrollView.enabled = false;

            if (UIManager.HasInstance)
            {
                UIManager.Instance.ShowLoading(onCovered: () => 
                {
                    EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel);
                });
            }
            else
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel);
            }
        }

        public void RefreshMapData()
        {
            if (_activeNodes == null || _activeNodes.Count == 0) return;

            UpdateContentHeight();

            int currentUnlockedLevel = DataManager.Instance != null ? DataManager.Instance.GetCurrentLevel() : 1;

            foreach (var kvp in _activeNodes)
            {
                int levelIndex = kvp.Key;
                LevelNode node = kvp.Value;

                LevelNodeState state = (levelIndex < currentUnlockedLevel) ? LevelNodeState.Passed :
                    (levelIndex == currentUnlockedLevel) ? LevelNodeState.Current : LevelNodeState.Locked;
                
                int stars = DataManager.Instance != null ? DataManager.Instance.GetLevelStars(levelIndex) : 0;
                
                node.SetupNode(levelIndex, state, stars, OnLevelClicked);
            }
        }
    }
}
