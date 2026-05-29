using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Base;
using ArrowGame.UI.Manager;
using ArrowGame.UI.Popups;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArrowGame.Utils.Developer
{
    public class DevLevelTest : MonoBehaviour
    {
        [Header("Level Jump")]
        [SerializeField] private TMP_InputField levelInputField;
        [SerializeField] private bool clearInputAfterLoad;

        [Header("Panel Toggle")]
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private float hiddenOffset = 80f;
        [SerializeField] private float toggleDuration = 0.25f;
        [SerializeField] private Ease toggleEase = Ease.OutCubic;

        private Tween _toggleTween;
        private Vector2 _shownAnchoredPosition;
        private Vector2 _hiddenAnchoredPosition;
        private bool _hasCachedShownPosition;
        private bool _isHidden;

        private void Awake()
        {
            CacheShownPosition();
            UpdateHiddenPosition();
        }

        private void OnEnable()
        {
            if (levelInputField != null)
            {
                levelInputField.onEndEdit.AddListener(HandleLevelInputEndEdit);
            }
        }

        private void Start()
        {
            ApplyPanelState(instant: true);
        }

        private void OnDisable()
        {
            if (levelInputField != null)
            {
                levelInputField.onEndEdit.RemoveListener(HandleLevelInputEndEdit);
            }

            _toggleTween?.Kill();
        }

        private void OnDestroy()
        {
            _toggleTween?.Kill();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                TogglePanel();
            }
        }

        private void HandleLevelInputEndEdit(string rawValue)
        {
            if (!isActiveAndEnabled) return;

            ConfirmLevelInput(rawValue);
        }

        private void ConfirmLevelInput(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return;

            if (!int.TryParse(rawValue, out int targetLevel))
            {
                Debug.LogWarning($"[DevLevelTest] Level không hợp lệ: {rawValue}");
                RefreshInputText();
                return;
            }

            targetLevel = Mathf.Max(1, targetLevel);

            if (DataManager.Instance == null)
            {
                Debug.LogWarning("[DevLevelTest] Không tìm thấy DataManager để chuyển level.");
                return;
            }

            DataManager.Instance.SelectedLevelIndex = targetLevel;

            if (clearInputAfterLoad && levelInputField != null)
            {
                levelInputField.text = string.Empty;
            }
            else
            {
                RefreshInputText(targetLevel.ToString());
            }

            RequestLoadLevel();
        }

        private void RequestLoadLevel()
        {
            if (DataManager.Instance != null && !DataManager.Instance.CanStartLevel())
            {
                UIManager.Instance.ShowPopup<BasePopup>(PopupID.OutOfEnergyPopup);

                return;
            }

            EventManager<LogicGameEventID>.Post(LogicGameEventID.RequestLoadLevel);
        }

        private void TogglePanel()
        {
            if (panelRect == null)
            {
                Debug.LogWarning("[DevLevelTest] Chưa gán panelRect để toggle dev panel.");
                return;
            }

            if (!_isHidden)
            {
                CacheShownPosition();
            }

            UpdateHiddenPosition();
            _isHidden = !_isHidden;
            ApplyPanelState(instant: false);
        }

        private void ApplyPanelState(bool instant)
        {
            if (panelRect == null) return;

            UpdateHiddenPosition();

            Vector2 targetPosition = _isHidden ? _hiddenAnchoredPosition : _shownAnchoredPosition;

            _toggleTween?.Kill();

            if (instant)
            {
                panelRect.anchoredPosition = targetPosition;
                return;
            }

            _toggleTween = panelRect
                .DOAnchorPos(targetPosition, toggleDuration)
                .SetEase(toggleEase)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void CacheShownPosition()
        {
            if (panelRect == null) return;

            _shownAnchoredPosition = panelRect.anchoredPosition;
            _hasCachedShownPosition = true;
        }

        private void UpdateHiddenPosition()
        {
            if (panelRect == null) return;

            if (!_hasCachedShownPosition)
            {
                CacheShownPosition();
            }

            float panelWidth = GetPanelSlideDistance();
            _hiddenAnchoredPosition = new Vector2(_shownAnchoredPosition.x - panelWidth - hiddenOffset, _shownAnchoredPosition.y);
        }

        private void RefreshInputText(string overrideValue = null)
        {
            if (levelInputField == null) return;

            levelInputField.SetTextWithoutNotify(overrideValue ?? levelInputField.text);
        }

        private float GetPanelSlideDistance()
        {
            float panelWidth = panelRect.rect.width;

            if (panelRect.parent is RectTransform parentRect)
            {
                panelWidth = Mathf.Max(panelWidth, parentRect.rect.width);
            }

            Canvas rootCanvas = panelRect.GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas.rootCanvas != null)
            {
                RectTransform canvasRect = rootCanvas.rootCanvas.transform as RectTransform;
                if (canvasRect != null)
                {
                    panelWidth = Mathf.Max(panelWidth, canvasRect.rect.width);
                }
            }

            return panelWidth * Mathf.Max(panelRect.lossyScale.x, 0.0001f);
        }
    }
}
