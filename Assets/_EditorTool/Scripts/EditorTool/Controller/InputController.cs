using System;
using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.System;
using ShareCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EditorTool.Scripts.EditorTool.Controller
{
    public class InputController : MonoBehaviour
    {
        [Header("Brush State — được set bởi EditorController")]
        public CellType currentBrush  = CellType.ArrowBodyVertical;
        public string currentArrowID  = "1";
        public bool drawHeadFirst     = false;
        public bool isSelectMode      = false;

        [Header("Hotkeys — có thể đổi trên Inspector")]
        public Key hotkeyNewArrow = Key.A;
        public Key hotkeySelect   = Key.V;
        public Key hotkeyErase    = Key.E;
        public Key hotkeySwap     = Key.S;

        // === Callbacks ===
        public Action OnNewArrowHotkey;
        public Action OnSwapHotkey;
        public Action OnEraseHotkey;
        public Action OnSelectHotkey;
        public Action<string> OnArrowSelectedFromMap;

        private Camera _mainCam;
        
        // --- THÊM BIẾN NÀY ĐỂ GHI NHỚ VỊ TRÍ CHUỘT Ở FRAME TRƯỚC ---
        private Vector2Int? _lastPaintedPos = null;

        private void Start() => _mainCam = Camera.main;

        private void Update()
        {
            if (LevelMakerManager.Instance.CurrentPhase != MakerPhase.BaseMap) return;
            if (Keyboard.current == null || Mouse.current == null) return;

            if (!IsTypingInInputField())
                FireHotkeyCallbacks();

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (isSelectMode)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    HandleMapSelection();
                return;
            }

            // --- RESET MEMORY KHI NHẢ CHUỘT ---
            // Tránh việc nhả chuột ra, đưa đi chỗ khác bấm vẽ tiếp bị sinh ra một đường nối dài
            if (Mouse.current.leftButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame)
            {
                _lastPaintedPos = null;
            }

            if (Mouse.current.leftButton.isPressed)       PaintCell(currentBrush, currentArrowID);
            else if (Mouse.current.rightButton.isPressed) PaintCell(CellType.EmptyDot, string.Empty);
        }

        private void FireHotkeyCallbacks()
        {
            if (Keyboard.current[hotkeyNewArrow].wasPressedThisFrame) OnNewArrowHotkey?.Invoke();
            if (Keyboard.current[hotkeySelect].wasPressedThisFrame)   OnSelectHotkey?.Invoke();
            if (Keyboard.current[hotkeyErase].wasPressedThisFrame)    OnEraseHotkey?.Invoke();
            if (Keyboard.current[hotkeySwap].wasPressedThisFrame)     OnSwapHotkey?.Invoke();
        }

        private void HandleMapSelection()
        {
            Vector2Int gridPos = GetMouseGridPosition();
            var cell = LevelMakerManager.Instance.GridSystem.GetCell(gridPos.x, gridPos.y);

            if (cell != null && !string.IsNullOrEmpty(cell.arrowID))
            {
                isSelectMode = false;
                OnArrowSelectedFromMap?.Invoke(cell.arrowID);
            }
        }

        private void PaintCell(CellType type, string id)
        {
            Vector2Int gridPos = GetMouseGridPosition();

            if (type == CellType.EmptyDot) // Chế độ cục tẩy (Chuột phải)
            {
                LevelMakerManager.Instance.GridSystem.RemoveArrowPathFrom(gridPos.x, gridPos.y);
                _lastPaintedPos = gridPos; 
            }
            else // Chế độ vẽ (Chuột trái)
            {
                // THUẬT TOÁN ĐIỀN VÀO CHỖ TRỐNG KHI VẨY CHUỘT
                if (_lastPaintedPos.HasValue && _lastPaintedPos.Value != gridPos)
                {
                    // Lấy danh sách các ô bị trượt mất
                    var points = GetManhattanLine(_lastPaintedPos.Value, gridPos);
                    
                    for (int i = 1; i < points.Count; i++) // i=1 vì bỏ qua điểm đầu (đã vẽ ở frame trước)
                    {
                        LevelMakerManager.Instance.GridSystem.ExtendArrowPath(points[i].x, points[i].y, id, drawHeadFirst);
                    }
                }
                else
                {
                    // Vẽ bình thường khi chuột đi chậm từng ô một
                    LevelMakerManager.Instance.GridSystem.ExtendArrowPath(gridPos.x, gridPos.y, id, drawHeadFirst);
                }

                _lastPaintedPos = gridPos; // Cập nhật lại memory
            }
        }

        // === THUẬT TOÁN TẠO CÁC BƯỚC ĐI "ZIC ZẮC" NỐI LIỀN 2 ĐIỂM BỊ TRƯỢT ===
        private List<Vector2Int> GetManhattanLine(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            result.Add(start);

            int currentX = start.x;
            int currentY = start.y;

            while (currentX != end.x || currentY != end.y)
            {
                // Ưu tiên đi theo trục có khoảng cách xa hơn để tạo bậc thang
                if (Mathf.Abs(end.x - currentX) > Mathf.Abs(end.y - currentY))
                {
                    currentX += (int)Mathf.Sign(end.x - currentX);
                }
                else
                {
                    currentY += (int)Mathf.Sign(end.y - currentY);
                }
                    
                result.Add(new Vector2Int(currentX, currentY));
            }
            
            return result;
        }

        private Vector2Int GetMouseGridPosition()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPos  = _mainCam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
            return new Vector2Int(Mathf.RoundToInt(mouseWorldPos.x), Mathf.RoundToInt(mouseWorldPos.y));
        }

        private bool IsTypingInInputField()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && selected.GetComponent<TMP_InputField>() != null;
        }
    }
}