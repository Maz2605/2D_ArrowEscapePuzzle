using System;
using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.System;
using ShareCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using ShareCore.Scripts.Data;

namespace EditorTool.Scripts.EditorTool.Controller
{
    public class InputController : MonoBehaviour
    {
        [Header("Brush State")]
        public CellType currentBrush = CellType.ArrowBodyVertical;
        public string currentArrowID = "1";
        public bool drawHeadFirst;
        public bool isSelectMode;
        public EditorBrushMode brushMode = EditorBrushMode.Arrow;
        public BoardSpecialType currentSpecialType = BoardSpecialType.Redirect;
        public Direction4 currentSpecialDirection = Direction4.Up;
        public string currentPortalId = "A";

        [Header("Hotkeys")]
        public Key hotkeyNewArrow = Key.A;
        public Key hotkeySelect = Key.V;
        public Key hotkeyErase = Key.E;
        public Key hotkeySwap = Key.S;
        public Key hotkeyArrowMode = Key.Digit1;
        public Key hotkeyPortalMode = Key.Digit2;
        public Key hotkeyRedirectMode = Key.Digit3;
        public Key hotkeyRotateDirection = Key.F;
        public Key hotkeyCyclePortalId = Key.G;
        public Key hotkeyUp = Key.UpArrow;
        public Key hotkeyRight = Key.RightArrow;
        public Key hotkeyDown = Key.DownArrow;
        public Key hotkeyLeft = Key.LeftArrow;
        public Key hotkeyToggleLeftPanel = Key.Tab;
        public Key hotkeyToggleRightPanel = Key.Backslash;

        public Action OnNewArrowHotkey;
        public Action OnSwapHotkey;
        public Action OnEraseHotkey;
        public Action OnSelectHotkey;
        public Action<string> OnArrowSelectedFromMap;
        public Action OnArrowModeHotkey;
        public Action OnPortalBrushHotkey;
        public Action OnRedirectBrushHotkey;
        public Action OnRotateDirectionHotkey;
        public Action OnCyclePortalIdHotkey;
        public Action<Direction4> OnDirectionHotkey;
        public Action OnToggleLeftPanelHotkey;
        public Action OnToggleRightPanelHotkey;
        public Action<SpecialCellSaveData> OnSpecialSelectedFromMap;
        public Action OnSpecialCellPlaced;
        public Action OnSpecialCellRemoved;

        private Camera _mainCam;
        private Vector2Int? _lastPaintedPos;

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

            if (Mouse.current.leftButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame)
            {
                _lastPaintedPos = null;
            }

            if (brushMode == EditorBrushMode.Special)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) PaintSpecialCell();
                else if (Mouse.current.rightButton.isPressed) RemoveSpecialCell();
                return;
            }

            if (Mouse.current.leftButton.isPressed) PaintArrowCell(currentBrush, currentArrowID);
            else if (Mouse.current.rightButton.isPressed) PaintArrowCell(CellType.EmptyDot, string.Empty);
        }

        private void FireHotkeyCallbacks()
        {
            if (Keyboard.current[hotkeyNewArrow].wasPressedThisFrame) OnNewArrowHotkey?.Invoke();
            if (Keyboard.current[hotkeySelect].wasPressedThisFrame) OnSelectHotkey?.Invoke();
            if (Keyboard.current[hotkeyErase].wasPressedThisFrame) OnEraseHotkey?.Invoke();
            if (Keyboard.current[hotkeySwap].wasPressedThisFrame) OnSwapHotkey?.Invoke();
            if (Keyboard.current[hotkeyArrowMode].wasPressedThisFrame) OnArrowModeHotkey?.Invoke();
            if (Keyboard.current[hotkeyPortalMode].wasPressedThisFrame) OnPortalBrushHotkey?.Invoke();
            if (Keyboard.current[hotkeyRedirectMode].wasPressedThisFrame) OnRedirectBrushHotkey?.Invoke();
            if (Keyboard.current[hotkeyRotateDirection].wasPressedThisFrame) OnRotateDirectionHotkey?.Invoke();
            if (Keyboard.current[hotkeyCyclePortalId].wasPressedThisFrame) OnCyclePortalIdHotkey?.Invoke();
            
            if (Keyboard.current[hotkeyUp].wasPressedThisFrame) OnDirectionHotkey?.Invoke(Direction4.Up);
            if (Keyboard.current[hotkeyRight].wasPressedThisFrame) OnDirectionHotkey?.Invoke(Direction4.Right);
            if (Keyboard.current[hotkeyDown].wasPressedThisFrame) OnDirectionHotkey?.Invoke(Direction4.Down);
            if (Keyboard.current[hotkeyLeft].wasPressedThisFrame) OnDirectionHotkey?.Invoke(Direction4.Left);
            
            if (Keyboard.current[hotkeyToggleLeftPanel].wasPressedThisFrame) OnToggleLeftPanelHotkey?.Invoke();
            if (Keyboard.current[hotkeyToggleRightPanel].wasPressedThisFrame) OnToggleRightPanelHotkey?.Invoke();
        }

        private void HandleMapSelection()
        {
            Vector2Int gridPos = GetMouseGridPosition();
            
            // 1. Check Special Cells first
            SpecialCellSaveData special = LevelMakerManager.Instance.GridSystem.GetSpecialCellAt(gridPos.x, gridPos.y);
            if (special != null)
            {
                isSelectMode = false;
                OnSpecialSelectedFromMap?.Invoke(special);
                return;
            }

            // 2. Check Arrows
            CellData cell = LevelMakerManager.Instance.GridSystem.GetCell(gridPos.x, gridPos.y);
            if (cell != null && !string.IsNullOrEmpty(cell.arrowID))
            {
                isSelectMode = false;
                OnArrowSelectedFromMap?.Invoke(cell.arrowID);
            }
        }

        private void PaintArrowCell(CellType type, string id)
        {
            Vector2Int gridPos = GetMouseGridPosition();

            if (type == CellType.EmptyDot)
            {
                LevelMakerManager.Instance.GridSystem.RemoveArrowPathFrom(gridPos.x, gridPos.y);
                _lastPaintedPos = gridPos;
                return;
            }

            if (_lastPaintedPos.HasValue && _lastPaintedPos.Value != gridPos)
            {
                List<Vector2Int> points = GetManhattanLine(_lastPaintedPos.Value, gridPos);
                for (int i = 1; i < points.Count; i++)
                {
                    LevelMakerManager.Instance.GridSystem.ExtendArrowPath(points[i].x, points[i].y, id, drawHeadFirst);
                }
            }
            else
            {
                LevelMakerManager.Instance.GridSystem.ExtendArrowPath(gridPos.x, gridPos.y, id, drawHeadFirst);
            }

            _lastPaintedPos = gridPos;
        }

        private void PaintSpecialCell()
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (LevelMakerManager.Instance.GridSystem.SetSpecialCell(gridPos.x, gridPos.y, currentSpecialType,
                currentSpecialDirection, currentPortalId))
            {
                OnSpecialCellPlaced?.Invoke();
            }
        }

        private void RemoveSpecialCell()
        {
            Vector2Int gridPos = GetMouseGridPosition();
            if (LevelMakerManager.Instance.GridSystem.GetSpecialCellAt(gridPos.x, gridPos.y) != null)
            {
                LevelMakerManager.Instance.GridSystem.RemoveSpecialCellAt(gridPos.x, gridPos.y);
                OnSpecialCellRemoved?.Invoke();
            }
        }

        private List<Vector2Int> GetManhattanLine(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> result = new List<Vector2Int> { start };

            int currentX = start.x;
            int currentY = start.y;

            while (currentX != end.x || currentY != end.y)
            {
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
            Vector3 mouseWorldPos = _mainCam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
            return new Vector2Int(Mathf.RoundToInt(mouseWorldPos.x), Mathf.RoundToInt(mouseWorldPos.y));
        }

        private bool IsTypingInInputField()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && selected.GetComponent<TMP_InputField>() != null;
        }
    }
}
