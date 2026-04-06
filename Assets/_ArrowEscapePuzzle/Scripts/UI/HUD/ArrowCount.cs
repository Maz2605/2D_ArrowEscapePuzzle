using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Gameplay.Managers;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArrowGame.UI.HUD
{
    public class ArrowCount : MonoBehaviour, IPointerClickHandler
    {
        public enum DisplayState
        {
            Hidden,
            TemporaryShow,
            Locked
        }

        [Header("References")]
        [SerializeField] private TextMeshProUGUI txtArrowCount;
        [SerializeField] private RectTransform panelRect;

        [Header("Movement Settings")]
        [SerializeField] private Vector2 hiddenPos;
        [SerializeField] private Vector2 visiblePos;
        [SerializeField] private float moveDuration = 0.4f;
        [SerializeField] private float displayDuration = 5f;

        [Header("Animation Settings")]
        [SerializeField] private float punchScaleAmount = 0.6f;
        [SerializeField] private float animDuration = 0.15f;
        [SerializeField] private float countUpDuration = 0.5f;

        private DisplayState _currentState = DisplayState.Hidden;
        private Tween _moveTween;
        private Tween _punchTween;
        private Tween _countTween;
        private Tween _delayHideTween; 
        
        private bool _hasDoneInitialCountUp = false;
        private readonly Vector3 _baseScale = Vector3.one;
        private Vector3 _punchVector;

        private void Awake()
        {
            if (txtArrowCount == null) txtArrowCount = GetComponentInChildren<TextMeshProUGUI>();
            if (panelRect == null) panelRect = GetComponent<RectTransform>();

            _punchVector = _baseScale * punchScaleAmount;
            panelRect.anchoredPosition = hiddenPos;
        }

        private void OnEnable()
        {
            _hasDoneInitialCountUp = false;
            EventManager<LogicGameEventID>.AddListener<int>(LogicGameEventID.ArrowCountChanged, HandleArrowCountChanged);
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
        }

        private void OnDisable()
        {
            EventManager<LogicGameEventID>.RemoveListener<int>(LogicGameEventID.ArrowCountChanged, HandleArrowCountChanged);
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            
            _moveTween?.Kill();
            _punchTween?.Kill();
            _countTween?.Kill();
            _delayHideTween?.Kill();
        }

        private void OnDestroy()
        {
            _moveTween?.Kill();
            _punchTween?.Kill();
            _countTween?.Kill();
            _delayHideTween?.Kill();
            
            if (txtArrowCount != null) txtArrowCount.transform.DOKill();
            if (panelRect != null) panelRect.DOKill();
        }

        private void HandleInGameStateChanged(InGameState newState)
        {
            if (newState == InGameState.Intro)
            {
                _hasDoneInitialCountUp = false;
                ChangeState(DisplayState.Hidden);
            }
            else if (newState == InGameState.Playing)
            {
                if (_currentState != DisplayState.Locked)
                {
                    ChangeState(DisplayState.TemporaryShow);
                }
            }
        }

        private void HandleArrowCountChanged(int value)
        {
            if (_currentState != DisplayState.Locked)
            {
                ChangeState(DisplayState.TemporaryShow);
            }

            if (!_hasDoneInitialCountUp)
            {
                _hasDoneInitialCountUp = true;
                AnimateCountUpText(value); 
            }
            else
            {
                AnimateScoreText(value);   
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.clickCount == 2)
            {
                ChangeState(DisplayState.Locked);
            }
            else if (eventData.clickCount == 1)
            {
                if (_currentState == DisplayState.Locked)
                {
                    ChangeState(DisplayState.Hidden);
                }
                else if (_currentState == DisplayState.Hidden)
                {
                    ChangeState(DisplayState.TemporaryShow);
                }
            }
        }

        private void ChangeState(DisplayState newState)
        {
            _currentState = newState;
            _delayHideTween?.Kill(); 

            switch (newState)
            {
                case DisplayState.Hidden:
                    MovePanel(hiddenPos);
                    break;

                case DisplayState.TemporaryShow:
                    MovePanel(visiblePos);
                    _delayHideTween = DOVirtual.DelayedCall(displayDuration, () => 
                    {
                        if (_currentState == DisplayState.TemporaryShow)
                            ChangeState(DisplayState.Hidden);
                    }).SetLink(gameObject);
                    break;

                case DisplayState.Locked:
                    MovePanel(visiblePos);
                    break;
            }
        }

        private void MovePanel(Vector2 targetPos)
        {
            _moveTween?.Kill();
            _moveTween = panelRect.DOAnchorPos(targetPos, moveDuration)
                .SetEase(Ease.OutBack, 1.5f) 
                .SetLink(panelRect.gameObject);
        }

        private void AnimateScoreText(int score)
        {
            if (txtArrowCount == null) return;

            txtArrowCount.SetText("{0}", score);
            
            _punchTween?.Kill();
            txtArrowCount.transform.localScale = _baseScale;

            _punchTween = txtArrowCount.transform
                .DOPunchScale(_punchVector, animDuration, vibrato: 5, elasticity: 0.5f)
                .SetLink(txtArrowCount.gameObject);
        }

        private void AnimateCountUpText(int targetScore)
        {
            if (txtArrowCount == null) return;

            _countTween?.Kill();
            _punchTween?.Kill(); 

            int currentDisplayValue = 0;
            txtArrowCount.SetText("0");

            txtArrowCount.transform.localScale = _baseScale * 0.5f;
            _punchTween = txtArrowCount.transform.DOScale(_baseScale * 1.5f, countUpDuration * 0.5f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => 
                {
                    txtArrowCount.transform.DOScale(_baseScale, countUpDuration * 0.5f)
                        .SetEase(Ease.OutBounce)
                        .SetLink(txtArrowCount.gameObject); 
                })
                .SetLink(txtArrowCount.gameObject);

            _countTween = DOTween.To(() => currentDisplayValue, x => 
            {
                currentDisplayValue = x;
                txtArrowCount.SetText("{0}", currentDisplayValue);
            }, targetScore, countUpDuration)
            .SetEase(Ease.OutExpo) 
            .SetLink(txtArrowCount.gameObject);
        }
    }
}