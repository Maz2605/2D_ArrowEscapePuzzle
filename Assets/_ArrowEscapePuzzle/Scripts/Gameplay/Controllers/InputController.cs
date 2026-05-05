using System;
using ArrowGame.Data.Events;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Visual;
using DG.Tweening;
using GameCore.Input;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.Gameplay.Controllers
{
    public class InputController : MonoBehaviour
    {
        public event Action<Vector2Int> OnGridCellClicked;
        public event Action<Vector2> OnCameraPanStart;
        public event Action<Vector2> OnCameraPanProcess;
        public event Action OnCameraPanEnd;
        public event Action OnCameraResetZoom;

        [SerializeField] private GridView gridView;

        [Header("Input Configurations")]
        [SerializeField] private float holdTimeToScale = 0.5f;
        [SerializeField] private float sameCellCooldown = 0.5f;
        [SerializeField] private float dragThreshold = 10f;

        public bool IsLocked { get; set; }

        private Vector2Int _originGridPos = new Vector2Int(-1, -1);
        private ArrowLineView _selectedArrow;

        private Vector2Int _lastClickedPos = new Vector2Int(-1, -1);
        private float _lastClickTime;
        private Tween _holdTween;

        private bool _isFingerDown;
        private bool _isPanning;
        private Vector2 _startScreenPos;

        private float _lastEmptyTapTime = 0f;
        private const float DoubleTapThreshold = 0.3f;

        private void OnEnable()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnTouchStart += HandleTouchStart;
            InputManager.Instance.OnTouchEnd += HandleTouchEnd;
            InputManager.Instance.OnTouchMove += HandleTouchMove;
        }

        private void OnDisable()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnTouchStart -= HandleTouchStart;
            InputManager.Instance.OnTouchEnd -= HandleTouchEnd;
            InputManager.Instance.OnTouchMove -= HandleTouchMove;
            _holdTween?.Kill();
        }

        private void HandleTouchStart(Vector2 screenPos)
        {
            if (IsLocked) return;

            _isFingerDown = true;
            _startScreenPos = screenPos;
            
            if (InputManager.Instance.GetTouchCount() >= 2) return;

            _originGridPos = gridView.WorldToGridPos(InputManager.Instance.GetWorldPosition());
            _selectedArrow = gridView.GetArrowViewAt(_originGridPos);

            bool isSpamming = (_originGridPos == _lastClickedPos && (Time.time - _lastClickTime < sameCellCooldown));

            if (_selectedArrow != null && !isSpamming)
            {
                // Phan hoi xuc giac (Haptic) nhe khi cham vao mui ten
                ArrowGame.Haptic.HapticManager.Instance?.LightVibrateImpact();

                // Gui event de trigger Camera Micro-Shake va Grid Impact bounce
                EventManager<VisualEventID>.Post(VisualEventID.TapArrowHit);

                _isPanning = false;
                _holdTween?.Kill();
                _holdTween = DOVirtual.DelayedCall(holdTimeToScale, () =>
                {
                    if (_selectedArrow != null) _selectedArrow.PlayHoldEffect(true);
                });
            }
            else
            {
                _isPanning = false;
                CancelHoldState();
            }
        }

        private void HandleTouchMove(Vector2 currentScreenPos)
        {
            if (IsLocked) return;

            if (!_isFingerDown || InputManager.Instance == null || InputManager.Instance.GetTouchCount() >= 2)
            {
                _isPanning = false;
                return;
            }

            if (!_isPanning && Vector2.Distance(currentScreenPos, _startScreenPos) > dragThreshold)
            {
                _isPanning = true;
                CancelHoldState();
                OnCameraPanStart?.Invoke(currentScreenPos);
            }

            if (_isPanning) OnCameraPanProcess?.Invoke(currentScreenPos);
        }

        private void HandleTouchEnd(Vector2 screenPos)
        {
            if (IsLocked) return;

            _isFingerDown = false;
            bool wasPanning = _isPanning;

            if (!wasPanning && _originGridPos.x != -1 && _originGridPos.y != -1)
            {
                _lastClickedPos = _originGridPos;
                _lastClickTime = Time.time;
                CancelHoldState();

                // 1. Chi can la len la co nguoi click vao luoi. (De GameController tu lo lieu)
                OnGridCellClicked?.Invoke(_originGridPos);

                // 2. Logic Double Tap vao khoang trong (Reset Zoom)
                if (_selectedArrow == null)
                {
                    if (Time.time - _lastEmptyTapTime < DoubleTapThreshold)
                    {
                        OnCameraResetZoom?.Invoke();
                        _lastEmptyTapTime = 0f;
                    }
                    else
                    {
                        _lastEmptyTapTime = Time.time;
                    }
                }
            }

            if (wasPanning) OnCameraPanEnd?.Invoke();

            _isPanning = false;
            _selectedArrow = null;
            _originGridPos = new Vector2Int(-1, -1);
        }

        private void CancelHoldState()
        {
            _holdTween?.Kill();
            _holdTween = null;
            if (_selectedArrow != null) _selectedArrow.PlayHoldEffect(false);
            _selectedArrow = null;
        }
    }
}
