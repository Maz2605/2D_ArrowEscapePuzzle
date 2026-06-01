using System;
using System.Collections.Generic;
using DG.Tweening;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.Logic;
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

        [Header("UI References - Level Validation")]
        [SerializeField] private Button _btnCheckLevel;
        [SerializeField] private TextMeshProUGUI _validationStatusText;
        [SerializeField] private Image _validationStatusIcon;

        // ── Colors dùng cho status label ─────────────────────────────────────
        private static readonly Color ColorOk       = new Color(0.2f, 0.85f, 0.4f);   // xanh lá
        private static readonly Color ColorDead     = new Color(0.95f, 0.25f, 0.25f); // đỏ
        private static readonly Color ColorWarning  = new Color(0.95f, 0.75f, 0.1f);  // vàng
        private static readonly Color ColorIdle     = new Color(0.65f, 0.65f, 0.65f); // xám

        // ── Trạng thái debounce auto-check ───────────────────────────────────
        private bool _pendingValidation;
        private float _validationDelay = 1.2f; // giây, sau khi dừng vẽ mới chạy lại
        private float _validationTimer;

        public Action OnNewArrow;
        public Action OnSwap;
        public Action OnErase;
        public Action OnSelect;
        public Action OnResetMap;
        public Action OnSaveMap;
        public Action OnCheckLevel;
        public Action<string> OnArrowSelectedFromList;

        private readonly List<UIArrowListItem> _activeUIItems = new List<UIArrowListItem>();

        // ─────────────────────────────────────────────────────────────────────

        public void Initialize()
        {
            BindButton(_btnNewArrow, OnNewArrow);
            BindButton(_btnSwap, OnSwap);
            BindButton(_btnErase, OnErase);
            BindButton(_btnSelect, OnSelect);
            BindButton(_btnResetMap, OnResetMap);
            BindButton(_btnSave, OnSaveMap);
            BindButton(_btnCheckLevel, () => OnCheckLevel?.Invoke());

            // Auto-check validation sau khi map thay đổi (debounced)
            LevelMakerManager.Instance.GridSystem.OnCellChanged += (x, y, data) =>
            {
                RefreshArrowList();
                ScheduleValidation();
            };

            LevelMakerManager.Instance.GridSystem.OnGridRebuilt += () =>
            {
                RefreshArrowList();
                ScheduleValidation();
            };

            SetValidationIdle("Nhấn 'Kiểm Tra' hoặc vẽ xong để phân tích.");
        }

        private void Update()
        {
            if (!_pendingValidation) return;

            _validationTimer -= Time.deltaTime;
            if (_validationTimer > 0f) return;

            _pendingValidation = false;
            RunValidation();
        }

        // ─────────────────────────────────────────────────────────────────────

        private void ScheduleValidation()
        {
            _pendingValidation = true;
            _validationTimer = _validationDelay;
            SetValidationIdle("Đang phân tích...");
        }

        /// <summary>
        /// Chạy toàn bộ validation (cấu trúc + deadlock) và hiển thị kết quả.
        /// Có thể gọi trực tiếp từ nút "Kiểm Tra Level".
        /// </summary>
        public void RunValidation()
        {
            _pendingValidation = false;

            if (LevelMakerManager.Instance == null || LevelMakerManager.Instance.GridSystem == null)
            {
                SetValidationWarning("Không tìm thấy GridSystem.");
                return;
            }

            GridSystem grid = LevelMakerManager.Instance.GridSystem;

            // ── Bước 1: Validate cấu trúc cơ bản ────────────────────────────
            var baseValidation = MapValidator.ValidateBaseMap(grid);
            if (!baseValidation.isValid)
            {
                SetValidationError($"⚠ Cấu trúc lỗi:\n{baseValidation.errorMsg}");
                return;
            }

            // ── Bước 2: Phân tích deadlock ────────────────────────────────────
            var deadlockCheck = MapValidator.CheckDeadlock(grid);
            if (!deadlockCheck.isSolvable)
            {
                SetValidationError(FormatDeadlockMessage(deadlockCheck.result));
                return;
            }

            // ── Thành công ────────────────────────────────────────────────────
            bool hasSpecialCells = deadlockCheck.result != null &&
                                   deadlockCheck.message.Contains("Special Cells");
            if (hasSpecialCells)
            {
                SetValidationWarning(FormatSolvableMessage(deadlockCheck.result));
            }
            else
            {
                SetValidationOk(FormatSolvableMessage(deadlockCheck.result));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Hiển thị status
        // ─────────────────────────────────────────────────────────────────────

        private void SetValidationOk(string msg)     => SetStatus(msg, ColorOk,      "✅");
        private void SetValidationError(string msg)  => SetStatus(msg, ColorDead,    "❌");
        private void SetValidationWarning(string msg)=> SetStatus(msg, ColorWarning, "⚠");
        private void SetValidationIdle(string msg)   => SetStatus(msg, ColorIdle,    "○");

        private void SetStatus(string msg, Color color, string iconText)
        {
            if (_validationStatusText != null)
            {
                _validationStatusText.text = msg;
                _validationStatusText.color = color;

                // Nhấp nháy nhẹ khi có kết quả mới
                _validationStatusText.transform.DOKill();
                _validationStatusText.transform.localScale = Vector3.one;
                _validationStatusText.transform
                    .DOPunchScale(Vector3.one * 0.06f, 0.25f, 6, 0.5f);
            }

            if (_validationStatusIcon != null)
            {
                _validationStatusIcon.color = color;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Format message
        // ─────────────────────────────────────────────────────────────────────

        private static string FormatSolvableMessage(DeadlockAnalysisResult result)
        {
            if (result == null) return "✅ Map hợp lệ!";

            int freeCount = result.InitiallyFreeArrows?.Count ?? 0;
            string order = result.InitiallyFreeArrows != null && result.InitiallyFreeArrows.Count > 0
                ? string.Join(" → ", result.InitiallyFreeArrows)
                : "N/A";

            string note = result.Summary != null && result.Summary.Contains("Special Cells")
                ? "\n⚠ Có Special Cells — kiểm tra thêm trong Play Mode."
                : string.Empty;

            return $"✅ Giải được!\nThứ tự thoát gợi ý:\n[{order}]{note}";
        }

        private static string FormatDeadlockMessage(DeadlockAnalysisResult result)
        {
            if (result == null) return "❌ Phát hiện DEADLOCK!";

            int stuck = 0;
            foreach (var g in result.DeadlockGroups)
                stuck += g.ArrowIds?.Count ?? 0;

            string groups = string.Empty;
            for (int i = 0; i < result.DeadlockGroups.Count; i++)
            {
                var g = result.DeadlockGroups[i];
                string ids = g.ArrowIds != null ? string.Join(", ", g.ArrowIds) : "?";
                groups += $"\n🔒 Nhóm {i + 1}: [{ids}]";
            }

            return $"❌ LEVEL CHẾT! ({stuck} mũi tên kẹt){groups}";
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Arrow list
        // ─────────────────────────────────────────────────────────────────────

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

            float delay = 0f;
            foreach (string id in activeIDs)
            {
                List<Vector2Int> path = LevelMakerManager.Instance.GridSystem.GetArrowPath(id);
                if (path == null || path.Count == 0) continue;

                UIArrowListItem activeItem = PoolingManager.Instance.Spawn(_listItemPrefab, Vector3.zero, Quaternion.identity, _listContentParent);
                if (activeItem == null) continue;

                activeItem.transform.localScale = Vector3.zero;
                activeItem.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);

                _activeUIItems.Add(activeItem);
                activeItem.Setup(id, EditorConstants.GetArrowColor(id), path.Count, OnArrowSelectedFromList);

                delay += 0.05f;
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