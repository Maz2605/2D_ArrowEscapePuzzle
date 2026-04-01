using System;
using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.System;
using GameCore.Utils.DesignPattern.ObjectPooling;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI.Panels
{
    /// <summary>
    /// Panel UI thuần túy: hiển thị danh sách mũi tên và báo cáo hành động lên EditorController.
    /// Không chứa bất kỳ business logic nào.
    /// EditorController gán các callback trước khi gọi Initialize().
    /// </summary>
    public class DrawingToolPanel : MonoBehaviour
    {
        [Header("Drawing Tools")]
        [SerializeField] private Button btnNewArrow;
        [SerializeField] private Button btnSwap;
        [SerializeField] private Button btnErase;
        [SerializeField] private Button btnSelect;
        [SerializeField] private Button btnResetMap;
        [SerializeField] private Button btnSave;

        [Header("Arrow List")]
        [SerializeField] private TextMeshProUGUI arrowCountText;
        [SerializeField] private Transform listContentParent;
        [SerializeField] private UIArrowListItem listItemPrefab;

        // === Callbacks — được EditorController gán trước Initialize() ===
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
            // Mỗi button chỉ invoke callback tương ứng — không logic gì
            btnNewArrow.onClick.AddListener(() => OnNewArrow?.Invoke());
            btnSwap.onClick.AddListener(()     => OnSwap?.Invoke());
            btnErase.onClick.AddListener(()    => OnErase?.Invoke());
            btnSelect.onClick.AddListener(()   => OnSelect?.Invoke());
            btnResetMap.onClick.AddListener(() => OnResetMap?.Invoke());
            btnSave.onClick.AddListener(()     => OnSaveMap?.Invoke());

            LevelMakerManager.Instance.GridSystem.OnCellChanged += (x, y, data) => RefreshArrowList();
        }

        /// <summary>Rebuild danh sách UI từ GridSystem.</summary>
        public void RefreshArrowList()
        {
            var activeIDs = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();

            if (arrowCountText != null)
                arrowCountText.text = $"Tổng số mũi tên: {activeIDs.Count}";

            foreach (var item in _activeUIItems)
                if (item != null) PoolingManager.Instance.Despawn(item.gameObject);
            _activeUIItems.Clear();

            foreach (string id in activeIDs)
            {
                var path = LevelMakerManager.Instance.GridSystem.GetArrowPath(id);
                if (path == null || path.Count == 0) continue;

                UIArrowListItem activeItem = PoolingManager.Instance.Spawn(
                    listItemPrefab, Vector3.zero, Quaternion.identity, listContentParent);
                activeItem.transform.localScale = Vector3.one;
                _activeUIItems.Add(activeItem);

                // Truyền callback vào item — item không biết ai xử lý
                activeItem.Setup(id, EditorConstants.GetArrowColor(id), path.Count, OnArrowSelectedFromList);
            }
        }

        /// <summary>Báo cáo lên EditorController để chọn arrow có ID lớn nhất.</summary>
        public void AutoSelectLastArrow()
        {
            var activeIDs = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();
            int maxId = 0;
            foreach (var idStr in activeIDs)
                if (int.TryParse(idStr, out int id) && id > maxId) maxId = id;

            OnArrowSelectedFromList?.Invoke((maxId == 0 ? 1 : maxId).ToString());
        }
    }
}
