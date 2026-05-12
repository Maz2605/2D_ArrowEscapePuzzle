using System;
using System.Collections.Generic;
using DG.Tweening;
using EditorTool.Scripts.EditorTool.System;
using EditorTool.Scripts.UI.MechanicWidgets;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EditorTool.Scripts.UI.Panels
{
    public class MechanicDrawerPanel : MonoBehaviour
    {
        [Header("Panel Core")]
        [SerializeField] private RectTransform _drawerRect;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private TextMeshProUGUI _toggleButtonLabel;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Mode Selectors")]
        [SerializeField] private Button _arrowModeBtn;
        [SerializeField] private Button _portalModeBtn;
        [SerializeField] private Button _redirectModeBtn;
        [SerializeField] private Button _counterBlockModeBtn;

        [Header("Sub-Widgets (Prefabs)")]
        [SerializeField] private PortalSettingsWidget _portalWidget;
        [SerializeField] private RedirectSettingsWidget _redirectWidget;
        [SerializeField] private CounterBlockSettingsWidget _counterBlockWidget;
        
        [Header("Mechanic List")]
        [SerializeField] private GameObject _mechanicListGroup;
        [SerializeField] private TextMeshProUGUI _listTitleText;
        [SerializeField] private Transform _listContentParent;
        [SerializeField] private UIMechanicListItem _listItemPrefab;

        [Header("Animation")]
        [SerializeField] private float _slideDuration = 0.25f;
        [SerializeField] private float _hiddenPadding = 28f;

        // Giữ nguyên tên OnPortalIdChanged để tương thích ngược với EditorController hiện tại
        public Action OnArrowMode;
        public Action OnPortalBrush;
        public Action OnRedirectBrush;
        public Action OnCounterBlockBrush;
        public Action<Direction4> OnDirectionChanged;
        public Action<string> OnPortalIdChanged; 
        public Action<int> OnCounterChanged;
        public Action<string> OnMechanicSelectedFromList;

        private readonly List<UIMechanicListItem> _activeListItems = new List<UIMechanicListItem>();
        private Vector2 _shownPosition, _hiddenPosition;
        private bool _isOpen, _isInitialized;
        private string _currentMode = "ARROW";

        public void Initialize()
        {
            if (_isInitialized) return;

            // 1. Init Sub-Widgets & Wire Events
            if (_portalWidget != null)
            {
                _portalWidget.Initialize();
                _portalWidget.OnPortalIdChanged += id => OnPortalIdChanged?.Invoke(id);
                _portalWidget.OnDirectionChanged += dir => OnDirectionChanged?.Invoke(dir);
            }

            if (_redirectWidget != null)
            {
                _redirectWidget.Initialize();
                // Dùng chung event ném lên Controller để tiết kiệm biến state trên Controller
                _redirectWidget.OnIdChanged += id => OnPortalIdChanged?.Invoke(id); 
                _redirectWidget.OnDirectionChanged += dir => OnDirectionChanged?.Invoke(dir);
            }

            if (_counterBlockWidget != null)
            {
                _counterBlockWidget.Initialize();
                _counterBlockWidget.OnCounterChanged += count => OnCounterChanged?.Invoke(count);
            }

            // 2. Bind Local Buttons
            _toggleButton.onClick.AddListener(ToggleDrawer);
            _arrowModeBtn.onClick.AddListener(() => OnArrowMode?.Invoke());
            _portalModeBtn.onClick.AddListener(() => OnPortalBrush?.Invoke());
            _redirectModeBtn.onClick.AddListener(() => OnRedirectBrush?.Invoke());
            _counterBlockModeBtn.onClick.AddListener(() => OnCounterBlockBrush?.Invoke());

            // 3. Setup Grid System Listener
            if (LevelMakerManager.Instance?.GridSystem != null)
            {
                LevelMakerManager.Instance.GridSystem.OnCellChanged += (x, y, data) => RefreshMechanicList();
            }

            RecalculatePositions();
            SetOpen(false, true);
            RefreshState("ARROW", Direction4.Up, "A");

            _isInitialized = true;
        }

        public void RefreshState(string mode, Direction4 direction, string specialId)
        {
            if (!_isInitialized) return;
            
            // Format chuẩn lại tên mode để Status hiển thị đẹp nhất
            _currentMode = mode.Trim().ToUpperInvariant();

            bool isPortal = _currentMode.Contains("PORTAL");
            bool isRedirect = _currentMode.Contains("REDIRECT");
            bool isCounterBlock = _currentMode.Contains("COUNTER_BLOCK");
            bool isArrow = _currentMode.Contains("ARROW") || _currentMode.Contains("SELECT");

            SetButtonVisual(_arrowModeBtn, isArrow);
            SetButtonVisual(_portalModeBtn, isPortal);
            SetButtonVisual(_redirectModeBtn, isRedirect);
            SetButtonVisual(_counterBlockModeBtn, isCounterBlock);

            // Xử lý bật/tắt Widget
            if (isPortal)
            {
                _portalWidget.Show();
                _portalWidget.Refresh(specialId, direction);
                _redirectWidget.Hide();
                _counterBlockWidget.Hide();
            }
            else if (isRedirect)
            {
                _portalWidget.Hide();
                _redirectWidget.Show();
                _redirectWidget.Refresh(specialId, direction);
                _counterBlockWidget.Hide();
            }
            else if (isCounterBlock)
            {
                _portalWidget.Hide();
                _redirectWidget.Hide();
                _counterBlockWidget.Show();
                if (int.TryParse(specialId, out int counter))
                {
                    _counterBlockWidget.Refresh(counter);
                }
                else
                {
                    _counterBlockWidget.Refresh(1);
                }
            }
            else
            {
                _portalWidget.Hide();
                _redirectWidget.Hide();
                _counterBlockWidget.Hide();
            }

            UpdateStatusText(specialId, direction, isPortal, isRedirect, isCounterBlock);

            LayoutRebuilder.ForceRebuildLayoutImmediate(_drawerRect);
            RefreshMechanicList();
        }

        private void UpdateStatusText(string id, Direction4 dir, bool isPortal, bool isRedirect, bool isCounterBlock)
        {
            if (_statusText == null) return;

            string guideText = "";
            string detailLabel = "LINK ID";
            if (isPortal) 
            {
                guideText = $"Tip: ID '{id}' is auto-selected. Place 2 portals to link.";
            }
            else if (isRedirect) 
            {
                guideText = $"Tip: ID '{id}' is auto-assigned. Click to place, rotate with 'F'.";
            }
            else if (isCounterBlock)
            {
                detailLabel = "COUNTER";
                guideText = $"Tip: Set counter to '{id}'. Click to place.";
            }
            else 
            {
                guideText = "Tip: Select mode (V) works for both arrows and mechanics.";
            }

            string modeLabel = _currentMode;
            // Nếu là mode Select thì đổi label cho rõ ràng
            if (_currentMode.Contains("SELECT")) modeLabel = "SELECT (CHOOSE ARROW)";

            // Build chuỗi Status bằng tiếng Anh
            _statusText.text = $"<b>MODE:</b> <color=#58A6FF>{modeLabel}</color>\n" +
                               $"<b>{detailLabel}:</b> {id}\n" +
                               $"<b>EXIT DIR:</b> {dir.ToGlyph()} ({dir})\n\n" +
                               $"<i><color=#8B949E>{guideText}</color></i>";
        }

        private void RefreshMechanicList()
        {
            if (LevelMakerManager.Instance?.GridSystem == null) return;

            foreach (var item in _activeListItems)
            {
                if (item != null && item.gameObject != null) PoolingManager.Instance.Despawn(item.gameObject);
            }
            _activeListItems.Clear();

            bool isPortal = _currentMode.Contains("PORTAL");
            bool isRedirect = _currentMode.Contains("REDIRECT");
            bool isCounterBlock = _currentMode.Contains("COUNTER_BLOCK");

            if (!isPortal && !isRedirect && !isCounterBlock)
            {
                _mechanicListGroup.SetActive(false);
                return;
            }

            BoardSpecialType targetType = BoardSpecialType.Portal;
            if (isRedirect) targetType = BoardSpecialType.Redirect;
            else if (isCounterBlock) targetType = BoardSpecialType.CounterBlock;

            List<SpecialCellSaveData> activeCells = LevelMakerManager.Instance.GridSystem.GetSpecialSaveData();
            activeCells.RemoveAll(cell => cell == null || cell.Type != targetType);

            if (activeCells.Count == 0)
            {
                _mechanicListGroup.SetActive(false);
                return;
            }

            _mechanicListGroup.SetActive(true);
            
            if (isPortal) _listTitleText.text = "PORTAL LIST";
            else if (isRedirect) _listTitleText.text = "REDIRECT LIST";
            else if (isCounterBlock) _listTitleText.text = "COUNTER LIST";

            float delay = 0f;
            HashSet<string> seenPayloads = new HashSet<string>();
            foreach (SpecialCellSaveData cellData in activeCells)
            {
                string payload = isCounterBlock
                    ? (!string.IsNullOrEmpty(cellData.Id) ? cellData.Id : $"{cellData.Position.x},{cellData.Position.y}")
                    : (!string.IsNullOrEmpty(cellData.PortalId) ? cellData.PortalId : $"{cellData.Position.x},{cellData.Position.y}");

                if (!isCounterBlock && !seenPayloads.Add(payload))
                {
                    continue;
                }

                UIMechanicListItem activeItem = PoolingManager.Instance.Spawn(_listItemPrefab, Vector3.zero, Quaternion.identity, _listContentParent);
                if (activeItem == null) continue;

                activeItem.transform.localScale = Vector3.zero;
                activeItem.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);

                Color itemColor = new Color(0.6f, 0.2f, 0.8f);
                if (isRedirect) itemColor = new Color(0.9f, 0.5f, 0.1f);
                else if (isCounterBlock) itemColor = new Color(0.18f, 0.76f, 0.65f, 1f);

                string prefix = "Portal";
                if (isRedirect) prefix = "Redirect";
                else if (isCounterBlock) prefix = "Counter";

                string dirGlyph = cellData.ExitDirection.ToGlyph();
                string label = isCounterBlock
                    ? $"{payload}  x{cellData.Counter}"
                    : $"{prefix} {payload} {dirGlyph}";

                activeItem.Setup(label, payload, itemColor, OnMechanicSelectedFromList);
                _activeListItems.Add(activeItem);
                
                delay += 0.05f;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_drawerRect);
        }

        public void ToggleDrawer() { if (_isInitialized) SetOpen(!_isOpen); }

        private void SetOpen(bool open, bool immediate = false)
        {
            _isOpen = open;
            if (_toggleButtonLabel != null) _toggleButtonLabel.text = _isOpen ? "Close" : "Open";

            _drawerRect.DOKill();
            Vector2 target = open ? _shownPosition : _hiddenPosition;

            if (immediate)
            {
                _drawerRect.anchoredPosition = target;
                if (open) RefreshMechanicList();
            }
            else
            {
                _drawerRect.DOAnchorPos(target, _slideDuration).SetEase(Ease.OutCubic).SetUpdate(true)
                    .OnComplete(() => { if (open) RefreshMechanicList(); });
            }
        }

        private void RecalculatePositions()
        {
            if (_drawerRect != null)
            {
                _shownPosition = Vector2.zero;
                _hiddenPosition = new Vector2(-_drawerRect.rect.width - _hiddenPadding, 0f);
            }
        }

        private static void SetButtonVisual(Button btn, bool isSelected)
        {
            if (btn != null && btn.TryGetComponent(out Image img))
                img.color = isSelected ? new Color(0.22f, 0.66f, 0.95f, 1f) : new Color(0.16f, 0.2f, 0.27f, 1f);
        }

        private void OnDestroy()
        {
            _drawerRect?.DOKill();
            if (_portalWidget != null)
            {
                _portalWidget.OnPortalIdChanged -= OnPortalIdChanged; // Unsub dùng chung logic biến
                _portalWidget.OnDirectionChanged -= OnDirectionChanged;
            }
            if (_redirectWidget != null) 
            {
                _redirectWidget.OnIdChanged -= OnPortalIdChanged; 
                _redirectWidget.OnDirectionChanged -= OnDirectionChanged;
            }
        }
    }
}
