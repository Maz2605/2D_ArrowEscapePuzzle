using System;
using System.Collections.Generic;
using DG.Tweening;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.Logic;
using EditorTool.Scripts.EditorTool.System;
using EditorTool.Scripts.EditorTool.Visual;
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
        [SerializeField] private Button _btnHighlightErrors;
        [SerializeField] private TextMeshProUGUI _validationStatusText;
        [SerializeField] private Image _validationStatusIcon;

        private MapValidationResult _lastBaseValidation;
        private DeadlockAnalysisResult _lastDeadlockResult;
        private bool _lastIsValid = true;

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

            if (_btnCheckLevel != null)
            {
                BindButton(_btnCheckLevel, () => OnCheckLevel?.Invoke());

                if (_btnHighlightErrors == null)
                {
                    var parent = _btnCheckLevel.transform.parent;
                    var found = parent.Find("BtnHighlightErrors");
                    if (found != null)
                    {
                        _btnHighlightErrors = found.GetComponent<Button>();
                    }
                    else
                    {
                        GameObject go = Instantiate(_btnCheckLevel.gameObject, parent);
                        go.name = "BtnHighlightErrors";
                        _btnHighlightErrors = go.GetComponent<Button>();

                        RectTransform rect = go.GetComponent<RectTransform>();
                        if (rect != null)
                        {
                            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - 45f);
                        }

                        var textMesh = go.GetComponentInChildren<TextMeshProUGUI>();
                        if (textMesh != null)
                        {
                            textMesh.text = "Highlight Errors";
                        }
                    }
                }
            }

            if (_btnHighlightErrors != null)
            {
                BindButton(_btnHighlightErrors, HighlightErrorPoints);
            }

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

            SetValidationIdle("ready");
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
            SetValidationIdle("analyzing");
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
                _lastIsValid = false;
                _lastBaseValidation = null;
                _lastDeadlockResult = null;
                SetValidationError("error");
                UpdateHighlightButtonVisibility();
                return;
            }

            GridSystem grid = LevelMakerManager.Instance.GridSystem;

            // ── Bước 1: Validate cấu trúc cơ bản ────────────────────────────
            _lastBaseValidation = MapValidator.ValidateBaseMap(grid);
            if (!_lastBaseValidation.isValid)
            {
                _lastIsValid = false;
                _lastDeadlockResult = null;
                SetValidationError("error");
                Debug.LogError($"[Validation Error] {_lastBaseValidation.errorMsg}");
                UpdateHighlightButtonVisibility();
                HighlightErrorPoints();
                return;
            }

            // ── Bước 2: Phân tích deadlock ────────────────────────────────────
            var deadlockCheck = MapValidator.CheckDeadlock(grid);
            _lastDeadlockResult = deadlockCheck.result;
            if (!deadlockCheck.isSolvable)
            {
                _lastIsValid = false;
                SetValidationError("error");
                Debug.LogError($"[Validation Error] {FormatDeadlockMessage(_lastDeadlockResult)}");
                UpdateHighlightButtonVisibility();
                HighlightErrorPoints();
                return;
            }

            // ── Thành công ────────────────────────────────────────────────────
            _lastIsValid = true;
            SetValidationOk("accept");
            Debug.Log($"[Validation Success] {FormatSolvableMessage(_lastDeadlockResult)}");
            UpdateHighlightButtonVisibility();
        }

        private void UpdateHighlightButtonVisibility()
        {
            if (_btnHighlightErrors != null)
            {
                _btnHighlightErrors.gameObject.SetActive(!_lastIsValid);
            }
        }

        public void HighlightErrorPoints()
        {
            if (LevelMakerManager.Instance == null || LevelMakerManager.Instance.GridView == null) return;

            GridView gridView = LevelMakerManager.Instance.GridView;
            Color errorColor = new Color(0.95f, 0.25f, 0.25f); // Red error color

            // 1. Highlight base validation errors
            if (_lastBaseValidation != null && !_lastBaseValidation.isValid)
            {
                if (_lastBaseValidation.errorArrowIds != null)
                {
                    foreach (string id in _lastBaseValidation.errorArrowIds)
                    {
                        gridView.PlayArrowBounce(id);
                        gridView.PlayArrowFlash(id, errorColor, 1.2f);
                    }
                }

                if (_lastBaseValidation.errorCellPositions != null)
                {
                    foreach (Vector2Int pos in _lastBaseValidation.errorCellPositions)
                    {
                        gridView.PlaySpecialCellBounce(pos);
                        gridView.PlaySpecialCellFlash(pos, errorColor, 1.2f);
                    }
                }
            }

            // 2. Highlight deadlock groups
            if (_lastDeadlockResult != null && !_lastDeadlockResult.IsSolvable)
            {
                if (_lastDeadlockResult.DeadlockGroups != null)
                {
                    foreach (var group in _lastDeadlockResult.DeadlockGroups)
                    {
                        if (group.ArrowIds != null)
                        {
                            foreach (string id in group.ArrowIds)
                            {
                                gridView.PlayArrowBounce(id);
                                gridView.PlayArrowFlash(id, errorColor, 1.2f);
                            }
                        }
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Hiển thị status
        // ─────────────────────────────────────────────────────────────────────

        private void SetValidationOk(string msg)     => SetStatus(msg, ColorOk,      "✅");
        private void SetValidationError(string msg)  => SetStatus(msg, ColorDead,    "❌");
        private void SetValidationWarning(string msg)=> SetStatus(msg, ColorWarning, "⚠");
        private void SetValidationIdle(string msg)
        {
            _lastIsValid = true;
            UpdateHighlightButtonVisibility();
            SetStatus(msg, ColorIdle, "○");
        }

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