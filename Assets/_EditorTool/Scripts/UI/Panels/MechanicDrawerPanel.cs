using System;
using System.Collections.Generic;
using DG.Tweening;
using EditorTool.Scripts.Data;
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
        [SerializeField] private Button _twoHeadModeBtn;
        [SerializeField] private Button _linkModeBtn;

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

        public Action OnArrowMode;
        public Action OnPortalBrush;
        public Action OnRedirectBrush;
        public Action OnCounterBlockBrush;
        public Action OnTwoHeadMode;
        public Action OnLinkMode;
        public Action<Direction4> OnDirectionChanged;
        public Action<string> OnPortalIdChanged;
        public Action<int> OnCounterChanged;
        public Action<string> OnMechanicSelectedFromList;

        private readonly List<UIMechanicListItem> _activeListItems = new List<UIMechanicListItem>();
        private Vector2 _shownPosition;
        private Vector2 _hiddenPosition;
        private bool _isOpen;
        private bool _isInitialized;
        private string _currentMode = "ARROW";
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_portalWidget != null)
            {
                _portalWidget.Initialize();
                _portalWidget.OnPortalIdChanged += HandlePortalWidgetChanged;
                _portalWidget.OnDirectionChanged += HandleDirectionChanged;
            }

            if (_redirectWidget != null)
            {
                _redirectWidget.Initialize();
                _redirectWidget.OnIdChanged += HandlePortalWidgetChanged;
                _redirectWidget.OnDirectionChanged += HandleDirectionChanged;
            }

            if (_counterBlockWidget != null)
            {
                _counterBlockWidget.Initialize();
                _counterBlockWidget.OnCounterChanged += HandleCounterChanged;
            }

            _toggleButton.onClick.AddListener(ToggleDrawer);
            _arrowModeBtn.onClick.AddListener(() => OnArrowMode?.Invoke());
            _portalModeBtn.onClick.AddListener(() => OnPortalBrush?.Invoke());
            _redirectModeBtn.onClick.AddListener(() => OnRedirectBrush?.Invoke());
            _counterBlockModeBtn.onClick.AddListener(() => OnCounterBlockBrush?.Invoke());
            if (_twoHeadModeBtn != null) _twoHeadModeBtn.onClick.AddListener(() => OnTwoHeadMode?.Invoke());
            if (_linkModeBtn != null) _linkModeBtn.onClick.AddListener(() => OnLinkMode?.Invoke());

            if (LevelMakerManager.Instance?.GridSystem != null)
            {
                LevelMakerManager.Instance.GridSystem.OnCellChanged += HandleGridChanged;
                LevelMakerManager.Instance.GridSystem.OnArrowMetadataChanged += HandleArrowMetadataChanged;
            }

            RecalculatePositions();
            SetOpen(false, true);
            RefreshState("ARROW", Direction4.Up, "A");

            _isInitialized = true;
        }

        public void RefreshState(string mode, Direction4 direction, string specialId)
        {
            if (!_isInitialized) return;

            _currentMode = mode.Trim().ToUpperInvariant();

            bool isPortal = _currentMode.Contains("PORTAL");
            bool isRedirect = _currentMode.Contains("REDIRECT");
            bool isCounterBlock = _currentMode.Contains("COUNTER_BLOCK");
            bool isArrow = _currentMode.Contains("ARROW") || _currentMode.Contains("SELECT");

            SetButtonVisual(_arrowModeBtn, isArrow && !IsArrowMechanicMode());
            SetButtonVisual(_portalModeBtn, isPortal);
            SetButtonVisual(_redirectModeBtn, isRedirect);
            SetButtonVisual(_counterBlockModeBtn, isCounterBlock);
            SetButtonVisual(_twoHeadModeBtn, false);
            SetButtonVisual(_linkModeBtn, false);

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
                _counterBlockWidget.Refresh(int.TryParse(specialId, out int counter) ? counter : 1);
            }
            else
            {
                _portalWidget.Hide();
                _redirectWidget.Hide();
                _counterBlockWidget.Hide();
            }

            UpdateSpecialStatusText(specialId, direction, isPortal, isRedirect, isCounterBlock);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_drawerRect);
            RefreshMechanicList();
        }

        public void RefreshArrowMechanicState(string mode, string selectedArrowId, int pathLength, string primaryEndpointInfo,
            bool hasSecondaryHead, string linkGroupId, int linkedCount)
        {
            if (!_isInitialized) return;

            _currentMode = mode.Trim().ToUpperInvariant();
            SetButtonVisual(_arrowModeBtn, false);
            SetButtonVisual(_portalModeBtn, false);
            SetButtonVisual(_redirectModeBtn, false);
            SetButtonVisual(_counterBlockModeBtn, false);
            SetButtonVisual(_twoHeadModeBtn, _currentMode.Contains("TWO_HEAD"));
            SetButtonVisual(_linkModeBtn, _currentMode.Contains("LINK_ARROW"));

            _portalWidget.Hide();
            _redirectWidget.Hide();
            _counterBlockWidget.Hide();

            string displayArrow = string.IsNullOrWhiteSpace(selectedArrowId) ? "-" : selectedArrowId;
            string displayLink = string.IsNullOrWhiteSpace(linkGroupId) ? "None" : linkGroupId;
            string modeGuide = _currentMode.Contains("TWO_HEAD")
                ? "Hotkey 5: click an arrow to toggle 2-head. T also toggles the selected arrow."
                : "Hotkey 6: click to choose base arrow, Ctrl-click another arrow to add/remove the link.";

            string linkWarning = linkedCount == 1 && !string.IsNullOrWhiteSpace(linkGroupId)
                ? "\n<i><color=#F2CC60>Warning: current link group only has 1 arrow.</color></i>"
                : string.Empty;

            _statusText.text = $"<b>MODE:</b> <color=#58A6FF>{_currentMode}</color>\n" +
                               $"<b>SELECTED:</b> Arrow {displayArrow}\n" +
                               $"<b>PATH:</b> {pathLength} cells\n" +
                               $"<b>PRIMARY:</b> {primaryEndpointInfo}\n" +
                               $"<b>2-HEAD:</b> {(hasSecondaryHead ? "ON" : "OFF")}\n" +
                               $"<b>LINK GROUP:</b> {displayLink}\n" +
                               $"<b>GROUP SIZE:</b> {linkedCount}\n\n" +
                               $"<i><color=#8B949E>{modeGuide}</color></i>{linkWarning}";

            LayoutRebuilder.ForceRebuildLayoutImmediate(_drawerRect);
            RefreshMechanicList();
        }

        public void ToggleDrawer()
        {
            if (_isInitialized)
            {
                SetOpen(!_isOpen);
            }
        }

        private void UpdateSpecialStatusText(string id, Direction4 dir, bool isPortal, bool isRedirect,
            bool isCounterBlock)
        {
            if (_statusText == null) return;

            string guideText;
            string detailLabel = "LINK ID";
            if (isPortal)
            {
                guideText = $"Tip: ID '{id}' is auto-selected. PortalDirection is {dir.ToGlyph()}: arrows must move {dir.ToGlyph()} when entering, and will continue {dir.ToGlyph()} after exiting.";
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
                guideText = "Tip: Use 5 for Two-Head mode and 6 for Link mode.";
            }

            string modeLabel = _currentMode.Contains("SELECT") ? "SELECT (CHOOSE ARROW)" : _currentMode;
            string directionLabel = isPortal
                ? $"<b>PORTAL DIR:</b> {dir.ToGlyph()} ({dir})\n<b>TRAVEL IN/OUT:</b> {dir.ToGlyph()} ({dir})"
                : $"<b>EXIT DIR:</b> {dir.ToGlyph()} ({dir})";
            _statusText.text = $"<b>MODE:</b> <color=#58A6FF>{modeLabel}</color>\n" +
                               $"<b>{detailLabel}:</b> {id}\n" +
                               $"{directionLabel}\n\n" +
                               $"<i><color=#8B949E>{guideText}</color></i>";
        }

        private void RefreshMechanicList()
        {
            if (LevelMakerManager.Instance?.GridSystem == null) return;

            foreach (UIMechanicListItem item in _activeListItems)
            {
                if (item != null && item.gameObject != null)
                {
                    PoolingManager.Instance.Despawn(item.gameObject);
                }
            }

            _activeListItems.Clear();

            if (IsArrowMechanicMode())
            {
                RefreshArrowMechanicList();
                return;
            }

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
            _listTitleText.text = isPortal ? "PORTAL LIST" : isRedirect ? "REDIRECT LIST" : "COUNTER LIST";

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

                UIMechanicListItem activeItem = PoolingManager.Instance.Spawn(_listItemPrefab, Vector3.zero,
                    Quaternion.identity, _listContentParent);
                if (activeItem == null) continue;

                activeItem.transform.localScale = Vector3.zero;
                activeItem.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);

                Color itemColor = new Color(0.6f, 0.2f, 0.8f);
                if (isRedirect) itemColor = new Color(0.9f, 0.5f, 0.1f);
                else if (isCounterBlock) itemColor = new Color(0.18f, 0.76f, 0.65f, 1f);

                string prefix = isRedirect ? "Redirect" : isCounterBlock ? "Counter" : "Portal";
                string dirGlyph = cellData.Type == BoardSpecialType.Portal
                    ? cellData.PortalDirection.ToGlyph()
                    : cellData.ExitDirection.ToGlyph();
                string label = isCounterBlock ? $"{payload}  x{cellData.Counter}" : $"{prefix} {payload} {dirGlyph}";

                activeItem.Setup(label, payload, itemColor, OnMechanicSelectedFromList);
                _activeListItems.Add(activeItem);
                delay += 0.05f;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_drawerRect);
        }

        private void RefreshArrowMechanicList()
        {
            List<string> arrowIds = LevelMakerManager.Instance.GridSystem.GetAllArrowIDs();
            if (arrowIds.Count == 0)
            {
                _mechanicListGroup.SetActive(false);
                return;
            }

            arrowIds.Sort(StringComparer.Ordinal);
            _mechanicListGroup.SetActive(true);
            _listTitleText.text = _currentMode.Contains("TWO_HEAD") ? "ARROW TWO-HEAD LIST" : "ARROW LINK LIST";

            float delay = 0f;
            foreach (string arrowId in arrowIds)
            {
                EditorArrowMetadataData metadata = LevelMakerManager.Instance.GridSystem.GetArrowMetadata(arrowId);
                List<Vector2Int> path = LevelMakerManager.Instance.GridSystem.GetArrowPath(arrowId);
                int pathLength = path != null ? path.Count : 0;
                string badge = _currentMode.Contains("TWO_HEAD")
                    ? (metadata != null && metadata.HasSecondaryEndpoint ? "2H" : "1H")
                    : (!string.IsNullOrWhiteSpace(metadata?.LinkGroupId) ? metadata.LinkGroupId : "No Link");

                UIMechanicListItem activeItem = PoolingManager.Instance.Spawn(_listItemPrefab, Vector3.zero,
                    Quaternion.identity, _listContentParent);
                if (activeItem == null) continue;

                activeItem.transform.localScale = Vector3.zero;
                activeItem.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetDelay(delay);

                string label = $"Arrow {arrowId}  •  {badge}  •  {pathLength} cells";
                activeItem.Setup(label, arrowId, EditorConstants.GetArrowColor(arrowId), OnMechanicSelectedFromList);
                _activeListItems.Add(activeItem);
                delay += 0.05f;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_drawerRect);
        }

        private bool IsArrowMechanicMode()
        {
            return _currentMode.Contains("TWO_HEAD") || _currentMode.Contains("LINK_ARROW");
        }

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
                    .OnComplete(() =>
                    {
                        if (open) RefreshMechanicList();
                    });
            }
        }

        private void RecalculatePositions()
        {
            if (_drawerRect == null) return;
            _shownPosition = Vector2.zero;
            _hiddenPosition = new Vector2(-_drawerRect.rect.width - _hiddenPadding, 0f);
        }

        private static void SetButtonVisual(Button btn, bool isSelected)
        {
            if (btn != null && btn.TryGetComponent(out Image img))
            {
                img.color = isSelected ? new Color(0.22f, 0.66f, 0.95f, 1f) : new Color(0.16f, 0.2f, 0.27f, 1f);
            }
        }

        private void HandleGridChanged(int x, int y, CellData data)
        {
            RefreshMechanicList();
        }

        private void HandleArrowMetadataChanged(string arrowId)
        {
            RefreshMechanicList();
        }

        private void HandlePortalWidgetChanged(string value)
        {
            OnPortalIdChanged?.Invoke(value);
        }

        private void HandleDirectionChanged(Direction4 direction)
        {
            OnDirectionChanged?.Invoke(direction);
        }

        private void HandleCounterChanged(int counter)
        {
            OnCounterChanged?.Invoke(counter);
        }

        private void OnDestroy()
        {
            _drawerRect?.DOKill();

            if (_portalWidget != null)
            {
                _portalWidget.OnPortalIdChanged -= HandlePortalWidgetChanged;
                _portalWidget.OnDirectionChanged -= HandleDirectionChanged;
            }

            if (_redirectWidget != null)
            {
                _redirectWidget.OnIdChanged -= HandlePortalWidgetChanged;
                _redirectWidget.OnDirectionChanged -= HandleDirectionChanged;
            }

            if (_counterBlockWidget != null)
            {
                _counterBlockWidget.OnCounterChanged -= HandleCounterChanged;
            }

            if (LevelMakerManager.Instance?.GridSystem != null)
            {
                LevelMakerManager.Instance.GridSystem.OnCellChanged -= HandleGridChanged;
                LevelMakerManager.Instance.GridSystem.OnArrowMetadataChanged -= HandleArrowMetadataChanged;
            }
        }
    }
}
