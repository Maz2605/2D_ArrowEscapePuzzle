using System;
using ArrowGame.Gameplay.Visual;
using DG.Tweening;
using GameCore.Input;
using UnityEngine;

namespace ArrowGame.Gameplay.Controllers
{
    public class InputController : MonoBehaviour
    {
        public event Action<Vector2Int> OnGridCellClicked;
        public event Action<Vector2> OnCameraPanStart;
        public event Action<Vector2> OnCameraPanProcess;
        public event Action OnCameraResetZoom; 

        [SerializeField] private GridView gridView;

        [Header("Input Configurations")]
        [SerializeField] private float holdTimeToScale = 0.5f; 
        [SerializeField] private float sameCellCooldown = 0.5f; 
        [SerializeField] private float dragThreshold = 10f; 

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
            _isFingerDown = true; 
            _startScreenPos = screenPos; 

            if (InputManager.Instance.GetTouchCount() >= 2) return;
            
            _originGridPos = gridView.WorldToGridPos(InputManager.Instance.GetWorldPosition());
            _selectedArrow = gridView.GetArrowViewAt(_originGridPos);

            bool isSpamming = (_originGridPos == _lastClickedPos && (Time.time - _lastClickTime < sameCellCooldown));

            if (_selectedArrow != null && !isSpamming)
            {
                _isPanning = false; 
                _holdTween?.Kill(); 
                _holdTween = DOVirtual.DelayedCall(holdTimeToScale, () =>
                {
                    if (_selectedArrow != null) _selectedArrow.PlayHoldEffect(true);
                }, ignoreTimeScale: false); 
            }
            else
            {
                _selectedArrow = null; 
                _isPanning = false; 
                CancelHoldState();
            }
        }

        private void HandleTouchMove(Vector2 currentScreenPos)
        {
            if (!_isFingerDown || InputManager.Instance == null || InputManager.Instance.GetTouchCount() >= 2)
            {
                _isPanning = false;
                return;
            }

            if (!_isPanning)
            {
                bool isDraggedFarEnough = Vector2.Distance(currentScreenPos, _startScreenPos) > dragThreshold;

                if (isDraggedFarEnough)
                {
                    _isPanning = true; 
                    CancelHoldState(); 
                    OnCameraPanStart?.Invoke(currentScreenPos); 
                }
            }

            if (_isPanning)
            {
                OnCameraPanProcess?.Invoke(currentScreenPos); 
            }
        }

        private void HandleTouchEnd(Vector2 screenPos)
        {
            _isFingerDown = false; 

            if (!_isPanning)
            {
                if (_selectedArrow != null)
                {
                    _lastClickedPos = _originGridPos;
                    _lastClickTime = Time.time;
                    _holdTween?.Kill();
                    _selectedArrow.PlayHoldEffect(false); 
                    OnGridCellClicked?.Invoke(_originGridPos); 
                }
                else
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

            _isPanning = false; 
            _selectedArrow = null;
            _originGridPos = new Vector2Int(-1, -1);
        }

        private void CancelHoldState()
        {
            _holdTween?.Kill(); 
            if (_selectedArrow != null) _selectedArrow.PlayHoldEffect(false);
            _selectedArrow = null; 
        }
    }
}