using System;
using System.Collections.Generic;
using DG.Tweening;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.System;
using GameCore.Utils.DesignPattern.ObjectPooling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI.Panels
{
    public class DrawingToolPanel : MonoBehaviour
    {
        [Header("UI References - Drawing Tools")]
        [SerializeField] private Button _btnNewArrow;
        [SerializeField] private Button _btnSwap;
        [SerializeField] private Button _btnErase;
        [SerializeField] private Button _btnSelect;
        [SerializeField] private Button _btnResetMap;
        [SerializeField] private Button _btnSave;

        [Header("UI References - Arrow List")]
        [SerializeField] private TextMeshProUGUI _arrowCountText;
        [SerializeField] private Transform _listContentParent;
        [SerializeField] private UIArrowListItem _listItemPrefab;

        public Action OnNewArrow;
        public Action OnSwap;
        public Action OnErase;
        public Action OnSelect;
        public Action OnResetMap;
        public Action OnSaveMap;
        public Action<string> OnArrowSelectedFromList;

        private readonly List<UIArrowListItem> _activeUIItems = new List<UIArrowListItem>();

        public void Initialize()
        {
            BindButton(_btnNewArrow, OnNewArrow);
            BindButton(_btnSwap, OnSwap);
            BindButton(_btnErase, OnErase);
            BindButton(_btnSelect, OnSelect);
            BindButton(_btnResetMap, OnResetMap);
            BindButton(_btnSave, OnSaveMap);

            LevelMakerManager.Instance.GridSystem.OnCellChanged += (x, y, data) => RefreshArrowList();
        }

        private void BindButton(Button btn, Action action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => action?.Invoke());
        }

        public void RefreshArrowList()
        {
            if (LevelMakerManager.Instance == null || LevelMakerManager.Instance.GridSystem == null) return;
            List<string> activeIDs = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();

            if (_arrowCountText != null)
                _arrowCountText.text = $"Tổng số mũi tên: {activeIDs.Count}";

            foreach (UIArrowListItem item in _activeUIItems)
            {
                if (item != null && item.gameObject != null) PoolingManager.Instance.Despawn(item.gameObject);
            }
            _activeUIItems.Clear();

            if (_listItemPrefab == null) return;

            // Stagger animation bằng DOTween
            float delay = 0f;
            foreach (string id in activeIDs)
            {
                List<Vector2Int> path = LevelMakerManager.Instance.GridSystem.GetArrowPath(id);
                if (path == null || path.Count == 0) continue;

                UIArrowListItem activeItem = PoolingManager.Instance.Spawn(_listItemPrefab, Vector3.zero, Quaternion.identity, _listContentParent);
                if (activeItem == null) continue;
                
                // Animation Pop-in (Scale từ 0 lên 1)
                activeItem.transform.localScale = Vector3.zero;
                activeItem.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);
                
                _activeUIItems.Add(activeItem);
                activeItem.Setup(id, EditorConstants.GetArrowColor(id), path.Count, OnArrowSelectedFromList);
                
                delay += 0.05f; // Item sau nảy lên sau item trước 0.05s
            }
        }

        public void AutoSelectLastArrow()
        {
            List<string> activeIDs = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();
            int maxId = 0;
            foreach (string idStr in activeIDs)
            {
                if (int.TryParse(idStr, out int id) && id > maxId) maxId = id;
            }

            OnArrowSelectedFromList?.Invoke((maxId == 0 ? 1 : maxId).ToString());
        }
    }
}