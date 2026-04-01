using System;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.System;
using ShareCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EditorTool.Scripts.EditorTool.Controller
{
    /// <summary>
    /// Chịu trách nhiệm: giữ brush state + xử lý input chuột/bàn phím.
    /// Hotkeys invoke Action callback — KHÔNG chứa business logic.
    /// EditorController gán các callback và quản lý state.
    /// </summary>
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

        // === Callbacks — được EditorController gán ===
        public Action OnNewArrowHotkey;
        public Action OnSwapHotkey;
        public Action OnEraseHotkey;
        public Action OnSelectHotkey;
        public Action<string> OnArrowSelectedFromMap;

        private Camera _mainCam;

        private void Start() => _mainCam = Camera.main;

        private void Update()
        {
            if (LevelMakerManager.Instance.CurrentPhase != MakerPhase.BaseMap) return;
            if (Keyboard.current == null || Mouse.current == null) return;

            // Hotkeys — chỉ invoke callback, không logic gì
            if (!IsTypingInInputField())
                FireHotkeyCallbacks();

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (isSelectMode)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                    HandleMapSelection();
                return;
            }

            if (Mouse.current.leftButton.isPressed)       PaintCell(currentBrush, currentArrowID);
            else if (Mouse.current.rightButton.isPressed) PaintCell(CellType.EmptyDot, string.Empty);
        }

        // =====================================================================
        // HOTKEYS — Chỉ invoke callback
        // =====================================================================

        private void FireHotkeyCallbacks()
        {
            if (Keyboard.current[hotkeyNewArrow].wasPressedThisFrame) OnNewArrowHotkey?.Invoke();
            if (Keyboard.current[hotkeySelect].wasPressedThisFrame)   OnSelectHotkey?.Invoke();
            if (Keyboard.current[hotkeyErase].wasPressedThisFrame)    OnEraseHotkey?.Invoke();
            if (Keyboard.current[hotkeySwap].wasPressedThisFrame)     OnSwapHotkey?.Invoke();
        }

        // =====================================================================
        // MAP SELECTION — Báo cáo arrow được click lên EditorController
        // =====================================================================

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

        // =====================================================================
        // DRAW LOGIC — Thuộc về InputController
        // =====================================================================

        private void PaintCell(CellType type, string id)
        {
            Vector2Int gridPos = GetMouseGridPosition();

            if (type == CellType.EmptyDot)
            {
                // Xóa path từ vị trí này trở về sau → kết quả là EmptyDot
                LevelMakerManager.Instance.GridSystem.RemoveArrowPathFrom(gridPos.x, gridPos.y);
            }
            else
            {
                LevelMakerManager.Instance.GridSystem.ExtendArrowPath(gridPos.x, gridPos.y, id, drawHeadFirst);
            }
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