using ArrowGame.Data.Events;
using ArrowGame.Gameplay.Managers;
using ArrowGame.UI.Manager;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;

namespace ArrowGame.UI.Components
{
    public class StreakWidget : MonoBehaviour
    {
        [Header("--- UI References ---")]
        [SerializeField] private TextMeshProUGUI streakText;
        [Tooltip("Kéo object Text clone vào đây để làm buffer cho animation trượt, thay vì Instantiate runtime")]
        [SerializeField] private TextMeshProUGUI streakTextBuffer; 
        [SerializeField] private RectTransform numberRoot;
        [SerializeField] private RectTransform stateIconRoot;
        [SerializeField] private GameObject noStreakFace;
        [SerializeField] private GameObject activeStreakFace;
        [SerializeField] private CanvasGroup visibilityCanvasGroup;

        [Header("--- Display Settings ---")]
        [SerializeField] private bool hideWhenZero;
        [SerializeField] private bool animateOnEnable = true;

        [Header("--- Number Animation ---")]
        [SerializeField] private float countDuration = 0.55f;
        [SerializeField] private float numberSlideDuration = 0.2f;
        [SerializeField] private float numberSlideOffset = 46f;
        [SerializeField] private float numberIncomingScale = 0.82f;
        [SerializeField] private float numberOutgoingScale = 1.08f;
        [SerializeField] private float numberPunchScale = 0.12f;
        [SerializeField] private Ease countEase = Ease.OutQuad;
        [SerializeField] private Ease numberIncomingEase = Ease.OutCubic;
        [SerializeField] private Ease numberOutgoingEase = Ease.InCubic;
        [SerializeField] private Ease numberBounceEase = Ease.OutBack;

        [Header("--- Icon Animation ---")]
        [SerializeField] private float iconFlipDuration = 0.18f;
        [SerializeField] private float iconFlipDelay = 0.02f;
        [SerializeField] private float iconPunchScale = 0.08f;
        [SerializeField] private Ease iconFlipEase = Ease.OutCubic;
        [SerializeField] private Ease iconReturnEase = Ease.OutBack;

        [Header("--- Visibility Sync ---")]
        [SerializeField] private bool deferRefreshUntilVisible = true;
        [SerializeField] private float visibleRefreshPollInterval = 0.05f;

        private int _displayedStreak;
        private bool _hasInitializedFromData;
        private bool _isWaitingForVisibleRefresh;
        private Vector2 _baseNumberAnchoredPosition;
        private Color _baseTextColor = Color.white;
        
        private Tween _countTween;
        private Tween _pendingVisibleRefreshTween;
        private Sequence _numberTransitionSequence;
        private Sequence _iconFlipSequence;
        
        // Cache để tránh GC Allocation trong quá trình polling
        private CanvasGroup[] _cachedCanvasGroups; 

        private void Awake()
        {
            if (streakText != null)
            {
                _baseNumberAnchoredPosition = streakText.rectTransform.anchoredPosition;
                _baseTextColor = streakText.color;
            }

            if (streakTextBuffer != null)
            {
                streakTextBuffer.gameObject.SetActive(false);
            }

            // Cache hierarchy 1 lần duy nhất để tối ưu
            _cachedCanvasGroups = GetComponentsInParent<CanvasGroup>(includeInactive: true);
        }

        private void OnEnable()
        {
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.StreakChanged, OnStreakChanged);

            if (ShouldDeferRefreshUntilVisible())
            {
                _isWaitingForVisibleRefresh = true;
                ScheduleVisibleRefreshPoll();
                return;
            }

            _isWaitingForVisibleRefresh = false;
            RefreshFromData(animateOnEnable);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.StreakChanged, OnStreakChanged);
            KillTweens();
            ResetAnimatedTransforms();
        }

        public void RefreshFromData(bool animate)
        {
            _isWaitingForVisibleRefresh = false;
            _pendingVisibleRefreshTween?.Kill();
            _pendingVisibleRefreshTween = null;

            int liveValue = DataManager.HasInstance && DataManager.Instance != null
                ? DataManager.Instance.CurrentWinStreak
                : 0;
            bool shouldAnimate = animate && gameObject.activeInHierarchy;

            if (!_hasInitializedFromData)
            {
                if (shouldAnimate)
                {
                    _displayedStreak = 0;
                    ApplyStateVisibility(_displayedStreak);
                    ApplyStateFace(false);
                    UpdateText(_displayedStreak);
                    ApplyAnimatedValue(liveValue, true);
                }
                else
                {
                    SetImmediate(liveValue);
                }

                _hasInitializedFromData = true;
                return;
            }

            if (shouldAnimate)
            {
                ApplyAnimatedValue(liveValue, false);
            }
            else
            {
                SetImmediate(liveValue);
            }
        }

        public void SetImmediate(int streakValue)
        {
            KillTweens();

            _displayedStreak = Mathf.Max(0, streakValue);
            bool hasStreak = _displayedStreak > 0;

            ApplyStateFace(hasStreak);
            ApplyStateVisibility(_displayedStreak);
            UpdateText(_displayedStreak);
            ResetAnimatedTransforms();
            _hasInitializedFromData = true;
        }

        private void OnStreakChanged(int newStreakValue)
        {
            if (_isWaitingForVisibleRefresh)
            {
                return;
            }

            ApplyAnimatedValue(newStreakValue, false);
        }

        private void ApplyAnimatedValue(int targetStreakValue, bool forceNumberAnimation)
        {
            int clampedTarget = Mathf.Max(0, targetStreakValue);
            bool previousHasStreak = _displayedStreak > 0;
            bool nextHasStreak = clampedTarget > 0;

            ApplyStateVisibility(clampedTarget);

            if (previousHasStreak != nextHasStreak)
            {
                PlayIconFlip(nextHasStreak);
            }
            else
            {
                ApplyStateFace(nextHasStreak);
            }

            if (!forceNumberAnimation && clampedTarget == _displayedStreak)
            {
                return;
            }

            PlayCountAnimation(_displayedStreak, clampedTarget);
        }

        private void PlayCountAnimation(int startValue, int endValue)
        {
            KillNumberTweens();

            int countingValue = startValue;
            int lastVisualValue = startValue;
            _displayedStreak = startValue;
            UpdateText(startValue);

            _countTween = DOTween.To(() => countingValue, value =>
                {
                    if (value == lastVisualValue)
                    {
                        return;
                    }

                    bool isCountingDown = value < lastVisualValue;
                    countingValue = value;
                    lastVisualValue = countingValue;
                    PlayNumberSlideTransition(isCountingDown);
                    _displayedStreak = countingValue;
                    UpdateText(countingValue);
                }, endValue, countDuration)
                .SetEase(countEase)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnStart(() =>
                {
                    ApplyStateVisibility(startValue);
                    UpdateText(startValue);
                })
                .OnComplete(() =>
                {
                    _displayedStreak = endValue;
                    ApplyStateVisibility(endValue);
                    UpdateText(endValue);
                    ResetNumberTransform();
                });
        }

        private void PlayNumberSlideTransition(bool isCountingDown)
        {
            if (streakText == null)
            {
                return;
            }

            _numberTransitionSequence?.Kill();
            ResetNumberTransform();

            // 1. Setup Text Buffer (Outgoing - Trượt đi)
            if (streakTextBuffer != null)
            {
                streakTextBuffer.gameObject.SetActive(true);
                streakTextBuffer.text = streakText.text;
                streakTextBuffer.rectTransform.anchoredPosition = _baseNumberAnchoredPosition;
                streakTextBuffer.rectTransform.localScale = Vector3.one * numberOutgoingScale;
                SetTextAlpha(streakTextBuffer, 1f);
            }

            // 2. Setup Main Text (Incoming - Trượt vào)
            RectTransform targetRect = streakText.rectTransform;
            float enterOffset = isCountingDown ? numberSlideOffset : -numberSlideOffset;
            float exitOffset = isCountingDown ? -numberSlideOffset : numberSlideOffset;

            targetRect.anchoredPosition = _baseNumberAnchoredPosition + new Vector2(0f, enterOffset);
            targetRect.localScale = new Vector3(1f, numberIncomingScale, 1f);
            SetTextAlpha(streakText, 0f);

            _numberTransitionSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            // Animate Main
            _numberTransitionSequence.Append(targetRect.DOAnchorPos(_baseNumberAnchoredPosition, numberSlideDuration)
                .SetEase(numberIncomingEase));
            _numberTransitionSequence.Join(targetRect.DOScale(Vector3.one, numberSlideDuration).SetEase(numberBounceEase));
            _numberTransitionSequence.Join(streakText.DOFade(1f, numberSlideDuration * 0.8f).SetEase(Ease.OutQuad));

            // Animate Buffer
            if (streakTextBuffer != null)
            {
                _numberTransitionSequence.Join(streakTextBuffer.rectTransform
                    .DOAnchorPos(_baseNumberAnchoredPosition + new Vector2(0f, exitOffset), numberSlideDuration)
                    .SetEase(numberOutgoingEase));
                _numberTransitionSequence.Join(streakTextBuffer.rectTransform
                    .DOScale(new Vector3(0.94f, numberIncomingScale, 1f), numberSlideDuration)
                    .SetEase(numberOutgoingEase));
                _numberTransitionSequence.Join(streakTextBuffer.DOFade(0f, numberSlideDuration * 0.75f).SetEase(Ease.OutQuad));
            }

            if (numberRoot != null)
            {
                _numberTransitionSequence.Join(numberRoot.DOPunchScale(Vector3.one * numberPunchScale,
                    numberSlideDuration * 1.15f, 2, 0.05f).SetEase(numberBounceEase));
            }

            _numberTransitionSequence.OnComplete(() =>
            {
                ResetNumberTransform();
            });
        }

        private void PlayIconFlip(bool showActiveFace)
        {
            if (stateIconRoot == null)
            {
                ApplyStateFace(showActiveFace);
                return;
            }

            _iconFlipSequence?.Kill();
            stateIconRoot.localEulerAngles = Vector3.zero;

            _iconFlipSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(stateIconRoot.gameObject, LinkBehaviour.KillOnDisable);

            _iconFlipSequence.AppendInterval(iconFlipDelay);
            if (!showActiveFace)
            {
                ApplyStateFace(false);
                _iconFlipSequence.Append(stateIconRoot.DOLocalRotate(new Vector3(0f, 90f, 0f), iconFlipDuration * 0.45f)
                    .SetEase(iconFlipEase));
                _iconFlipSequence.Append(stateIconRoot.DOLocalRotate(Vector3.zero, iconFlipDuration * 0.55f)
                    .SetEase(iconReturnEase));
                _iconFlipSequence.Join(stateIconRoot.DOPunchScale(Vector3.one * iconPunchScale,
                    iconFlipDuration, 2, 0.05f).SetEase(iconReturnEase));
            }
            else
            {
                _iconFlipSequence.Append(stateIconRoot.DOLocalRotate(new Vector3(0f, 90f, 0f), iconFlipDuration * 0.5f)
                    .SetEase(iconFlipEase));
                _iconFlipSequence.AppendCallback(() => ApplyStateFace(true));
                _iconFlipSequence.Append(stateIconRoot.DOLocalRotate(Vector3.zero, iconFlipDuration * 0.5f)
                    .SetEase(iconReturnEase));
                _iconFlipSequence.Join(stateIconRoot.DOPunchScale(Vector3.one * iconPunchScale,
                    iconFlipDuration, 2, 0.05f).SetEase(iconReturnEase));
            }
            _iconFlipSequence.OnComplete(() => stateIconRoot.localEulerAngles = Vector3.zero);
        }

        private void ApplyStateFace(bool showActiveFace)
        {
            if (noStreakFace != null)
            {
                noStreakFace.SetActive(!showActiveFace);
            }

            if (activeStreakFace != null)
            {
                activeStreakFace.SetActive(showActiveFace);
            }
        }

        private void ApplyStateVisibility(int streakValue)
        {
            if (visibilityCanvasGroup == null)
            {
                return;
            }

            bool shouldShow = !hideWhenZero || streakValue > 0;
            visibilityCanvasGroup.alpha = shouldShow ? 1f : 0f;
            visibilityCanvasGroup.interactable = shouldShow;
            visibilityCanvasGroup.blocksRaycasts = shouldShow;
        }

        private void UpdateText(int streakValue)
        {
            if (streakText != null)
            {
                streakText.text = streakValue.ToString();
            }
        }

        private void KillTweens()
        {
            KillNumberTweens();
            _pendingVisibleRefreshTween?.Kill();
            _pendingVisibleRefreshTween = null;
            _iconFlipSequence?.Kill();
            _iconFlipSequence = null;
        }

        private void KillNumberTweens()
        {
            _countTween?.Kill();
            _countTween = null;
            _numberTransitionSequence?.Kill();
            _numberTransitionSequence = null;
        }

        private void ResetAnimatedTransforms()
        {
            ResetNumberTransform();

            if (stateIconRoot != null)
            {
                stateIconRoot.localEulerAngles = Vector3.zero;
            }
        }

        private void ResetNumberTransform()
        {
            if (numberRoot != null)
            {
                numberRoot.localScale = Vector3.one;
                numberRoot.localEulerAngles = Vector3.zero;
            }
            
            if (streakText != null)
            {
                streakText.rectTransform.anchoredPosition = _baseNumberAnchoredPosition;
                streakText.rectTransform.localScale = Vector3.one;
                streakText.color = _baseTextColor;
            }

            if (streakTextBuffer != null)
            {
                streakTextBuffer.gameObject.SetActive(false);
            }
        }

        private bool ShouldDeferRefreshUntilVisible()
        {
            if (!deferRefreshUntilVisible) return false;

            // Sử dụng mảng cache để loại bỏ 100% GC Allocation mỗi 0.05s
            if (_cachedCanvasGroups != null)
            {
                for (int i = 0; i < _cachedCanvasGroups.Length; i++)
                {
                    CanvasGroup group = _cachedCanvasGroups[i];
                    if (group == null || group == visibilityCanvasGroup || group.gameObject == gameObject)
                    {
                        continue;
                    }

                    if (group.alpha < 0.99f)
                    {
                        return true;
                    }
                }
            }

            return UIManager.HasInstance && UIManager.Instance != null && UIManager.Instance.IsLoadingVisible;
        }

        private void ScheduleVisibleRefreshPoll()
        {
            _pendingVisibleRefreshTween?.Kill();
            _pendingVisibleRefreshTween = DOVirtual.DelayedCall(visibleRefreshPollInterval, TryRefreshWhenVisible)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void TryRefreshWhenVisible()
        {
            if (this == null || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (ShouldDeferRefreshUntilVisible())
            {
                ScheduleVisibleRefreshPoll();
                return;
            }

            _isWaitingForVisibleRefresh = false;
            RefreshFromData(animateOnEnable);
        }

        private void SetTextAlpha(TextMeshProUGUI textTarget, float alpha)
        {
            if (textTarget == null)
            {
                return;
            }

            Color color = textTarget.color;
            color.a = alpha;
            textTarget.color = color;
        }
    }
}