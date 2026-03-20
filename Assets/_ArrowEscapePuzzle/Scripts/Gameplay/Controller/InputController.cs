using System;
using ArrowGame.Gameplay.Visual;
using GameCore.Input;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.Gameplay.Controller
{
    public class InputController : MonoBehaviour
    {
        public event Action<Vector2Int> OnGridCellClicked;
        [SerializeField] private GridView gridView;

        [Header("Input Configurations")]
        [Tooltip("Thời gian giữ (giây) để kích hoạt hiệu ứng phình to")]
        [SerializeField] private float holdTimeToScale = 0.5f; 
        [Tooltip("Thời gian chờ nếu spam liên tục vào CÙNG 1 mũi tên (chặn spam rung lắc)")]
        [SerializeField] private float sameCellCooldown = 0.5f; 

        private Vector2Int _originGridPos = new Vector2Int(-1, -1);
        private ArrowLineView _selectedArrow;
        
        // Tracking state chống spam cục bộ
        private Vector2Int _lastClickedPos = new Vector2Int(-1, -1);
        private float _lastClickTime;
        
        private Tween _holdTween; 

        private void OnEnable()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnTouchStart += HandleTouchStart;
            InputManager.Instance.OnTouchMove += HandleTouchMove;
            InputManager.Instance.OnTouchEnd += HandleTouchEnd;
        }

        private void OnDisable()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnTouchStart -= HandleTouchStart;
            InputManager.Instance.OnTouchMove -= HandleTouchMove;
            InputManager.Instance.OnTouchEnd -= HandleTouchEnd;
            
            _holdTween?.Kill(); 
        }

        private void HandleTouchStart(Vector2 screenPos)
        {
            Vector2Int currentGridPos = gridView.WorldToGridPos(InputManager.Instance.GetWorldPosition());

            // CHỐNG SPAM: Nếu bấm lại ĐÚNG cái ô vừa bấm khi chưa hết cooldown -> Bỏ qua hoàn toàn
            // (Nhưng nếu bấm sang ô khác thì vẫn cho qua thoải mái)
            if (currentGridPos == _lastClickedPos && (Time.time - _lastClickTime < sameCellCooldown))
            {
                return;
            }

            _originGridPos = currentGridPos;
            _selectedArrow = gridView.GetArrowViewAt(_originGridPos);

            if (_selectedArrow != null)
            {
                _holdTween?.Kill(); 
                
                _holdTween = DOVirtual.DelayedCall(holdTimeToScale, () =>
                {
                    if (_selectedArrow != null)
                    {
                        _selectedArrow.PlayHoldEffect(true);
                    }
                }, ignoreTimeScale: false); 
            }
        }

        private void HandleTouchMove(Vector2 screenPos)
        {
            if (_selectedArrow == null) return;

            Vector2Int currentPos = gridView.WorldToGridPos(InputManager.Instance.GetWorldPosition());

            if (currentPos != _originGridPos)
            {
                CancelHoldState();
            }
        }

        private void HandleTouchEnd(Vector2 screenPos)
        {
            if (_selectedArrow != null)
            {
                // Ghi nhận lại vị trí và thời gian click để chặn spam cho lần chạm tiếp theo vào CHÍNH ô này
                _lastClickedPos = _originGridPos;
                _lastClickTime = Time.time;

                _holdTween?.Kill();
                _selectedArrow.PlayHoldEffect(false); 

                OnGridCellClicked?.Invoke(_originGridPos); 
            }

            // Reset state của touch hiện tại
            _selectedArrow = null;
            _originGridPos = new Vector2Int(-1, -1);
        }

        private void CancelHoldState()
        {
            _holdTween?.Kill(); 
            
            if (_selectedArrow != null)
            {
                _selectedArrow.PlayHoldEffect(false);
            }
            
            _selectedArrow = null; 
            _originGridPos = new Vector2Int(-1, -1);
        }
    }
}