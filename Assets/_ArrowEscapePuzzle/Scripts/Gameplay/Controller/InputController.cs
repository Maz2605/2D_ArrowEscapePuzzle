using System;
using ArrowGame.Gameplay.Visual;
using GameCore.Input;
using UnityEngine;

namespace ArrowGame.Gameplay.Controller
{
    public class InputController : MonoBehaviour
    {
        public event Action<Vector2Int> OnGridCellClicked;
        [SerializeField] private GridView gridView;

        private Vector2Int _originGridPos = new Vector2Int(-1, -1);
        private ArrowLineView _selectedArrow;

        private void OnEnable()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnTouchStart += HandleTouchStart;
            InputManager.Instance.OnTouchMove += HandleTouchMove; // Bắt sự kiện di chuyển
            InputManager.Instance.OnTouchEnd += HandleTouchEnd;
        }

        private void OnDisable()
        {
            if (InputManager.Instance == null) return;
            InputManager.Instance.OnTouchStart -= HandleTouchStart;
            InputManager.Instance.OnTouchMove -= HandleTouchMove;
            InputManager.Instance.OnTouchEnd -= HandleTouchEnd;
        }

        private void HandleTouchStart(Vector2 screenPos)
        {
            _originGridPos = gridView.WorldToGridPos(InputManager.Instance.GetWorldPosition());
            _selectedArrow = gridView.GetArrowViewAt(_originGridPos);

            if (_selectedArrow != null)
            {
                _selectedArrow.PlayHoldEffect(true);
            }
        }

        private void HandleTouchMove(Vector2 screenPos)
        {
            if (_selectedArrow == null) return;

            // Kiểm tra xem ngón tay còn nằm trong ô đó không
            Vector2Int currentPos = gridView.WorldToGridPos(InputManager.Instance.GetWorldPosition());

            if (currentPos != _originGridPos)
            {
                // Nếu rê tay ra khỏi ô ban đầu -> Hủy hiệu ứng, coi như người chơi đổi ý
                _selectedArrow.PlayHoldEffect(false);
                _selectedArrow = null; 
                _originGridPos = new Vector2Int(-1, -1);
            }
        }

        private void HandleTouchEnd(Vector2 screenPos)
        {
            // Chỉ thực hiện gameplay nếu đến lúc nhả tay ra, _selectedArrow vẫn hợp lệ
            if (_selectedArrow != null)
            {
                _selectedArrow.PlayHoldEffect(false); // Thu nhỏ lại
                OnGridCellClicked?.Invoke(_originGridPos); // Bắn sự kiện chạy logic game
            }

            // Reset state
            _selectedArrow = null;
            _originGridPos = new Vector2Int(-1, -1);
        }
    }
}