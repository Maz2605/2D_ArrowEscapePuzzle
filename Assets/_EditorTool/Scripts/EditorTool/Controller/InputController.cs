using System;
using System.Collections.Generic;
using EditorTool.Scripts.Data;
using EditorTool.Scripts.EditorTool.System;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
        public int currentCounterBlockValue = 1;
        public EditorArrowMechanicMode arrowMechanicMode = EditorArrowMechanicMode.None;
        public bool mysteryBoxHasWrappedCell = false;
        public BoardSpecialType mysteryBoxWrappedType = BoardSpecialType.Redirect;

        [Header("Hotkeys")]
        public Key hotkeyNewArrow = Key.A;
        public Key hotkeySelect = Key.V;
        public Key hotkeyErase = Key.E;
        public Key hotkeySwap = Key.S;
        public Key hotkeyArrowMode = Key.Digit1;
        public Key hotkeyPortalMode = Key.Digit2;
        public Key hotkeyRedirectMode = Key.Digit3;
        public Key hotkeyCounterBlockMode = Key.Digit4;
        public Key hotkeyTwoHeadMode = Key.Digit5;
        public Key hotkeyLinkMode = Key.Digit6;
        public Key hotkeyKeyBoxMode = Key.Digit7;
        public Key hotkeyRotateDirection = Key.F;
        public Key hotkeyCyclePortalId = Key.G;
        public Key hotkeySelectPrevArrow = Key.Q;
        public Key hotkeySelectNextArrow = Key.W;
        public Key hotkeyToggleSelectedTwoHead = Key.T;
        public Key hotkeyCreateLinkGroup = Key.G;
        public Key hotkeyClearLinkGroup = Key.Delete;
        public Key hotkeyClearLinkGroupAlt = Key.Backspace;
        public Key hotkeyUndo = Key.P;
        public Key hotkeyCancelMechanicMode = Key.Escape;
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
        public Action OnCounterBlockBrushHotkey;
        public Action OnKeyBoxBrushHotkey;
        public Action OnNewLockGroupHotkey;
        public Action OnToggleKeyBoxModeHotkey;
        public Action OnTwoHeadModeHotkey;
        public Action OnLinkModeHotkey;
        public Action OnCancelArrowMechanicModeHotkey;
        public Action OnRotateDirectionHotkey;
        public Action OnCyclePortalIdHotkey;
        public Action OnSelectPreviousArrowHotkey;
        public Action OnSelectNextArrowHotkey;
        public Action OnToggleSelectedTwoHeadHotkey;
        public Action OnCreateLinkGroupHotkey;
        public Action OnClearLinkGroupHotkey;
        public Action OnUndoHotkey;
        public Action<Direction4> OnDirectionHotkey;
        public Action OnToggleLeftPanelHotkey;
        public Action OnToggleRightPanelHotkey;
        public Action<SpecialCellSaveData> OnSpecialSelectedFromMap;
        public Action OnSpecialCellPlaced;
        public Action OnSpecialCellRemoved;
        public Action<string> OnArrowTwoHeadClicked;
        public Action<string, bool> OnArrowLinkClicked;
        public Action OnBeginMutation;

        private Camera _mainCam;
        private Vector2Int? _lastPaintedPos;

        private void Start() => _mainCam = Camera.main;

        private void Update()
        {
            if (LevelMakerManager.Instance.CurrentPhase != MakerPhase.BaseMap) return;
            if (Keyboard.current == null || Mouse.current == null) return;

            if (!IsTypingInInputField())
            {
                FireHotkeyCallbacks();
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (HandleArrowMechanicMapInput()) return;

            if (isSelectMode)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    HandleMapSelection();
                }

                return;
            }

            if (Mouse.current.leftButton.wasReleasedThisFrame || Mouse.current.rightButton.wasReleasedThisFrame)
            {
                _lastPaintedPos = null;
            }

            if (brushMode == EditorBrushMode.Special)
            {
                if (Mouse.current.leftButton.isPressed)
                {
                    if (Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        OnBeginMutation?.Invoke();
                    }

                    bool isCtrlPressed = IsCtrlPressed();
                    if (isCtrlPressed && currentSpecialType == BoardSpecialType.CounterBlock)
                    {
                        Vector2Int gridPos = GetMouseGridPosition();
                        if (!_lastPaintedPos.HasValue || _lastPaintedPos.Value != gridPos)
                        {
                            if (_lastPaintedPos.HasValue)
                            {
                                List<Vector2Int> points = GetManhattanLine(_lastPaintedPos.Value, gridPos);
                                for (int i = 1; i < points.Count; i++)
                                {
                                    ExpandSpecialCellAt(points[i]);
                                }
                            }
                            else
                            {
                                ExpandSpecialCellAt(gridPos);
                            }

                            _lastPaintedPos = gridPos;
                        }
                    }
                    else if (Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        PaintSpecialCell();
                    }
                }
                else if (Mouse.current.rightButton.isPressed)
                {
                    if (Mouse.current.rightButton.wasPressedThisFrame)
                    {
                        OnBeginMutation?.Invoke();
                    }

                    RemoveSpecialCell();
                }

                return;
            }

            if (Mouse.current.leftButton.isPressed)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    OnBeginMutation?.Invoke();
                }

                PaintArrowCell(currentBrush, currentArrowID);
            }
            else if (Mouse.current.rightButton.isPressed)
            {
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    OnBeginMutation?.Invoke();
                }

                PaintArrowCell(CellType.EmptyDot, string.Empty);
            }
        }

        private void FireHotkeyCallbacks()
        {
            bool isKeyBoxMode = brushMode == EditorBrushMode.Special &&
                (currentSpecialType == BoardSpecialType.Key || currentSpecialType == BoardSpecialType.MysteryBox);

            if (Keyboard.current[hotkeyNewArrow].wasPressedThisFrame)
            {
                if (isKeyBoxMode) OnNewLockGroupHotkey?.Invoke();
                else OnNewArrowHotkey?.Invoke();
            }

            if (Keyboard.current[hotkeySelect].wasPressedThisFrame) OnSelectHotkey?.Invoke();
            if (Keyboard.current[hotkeyErase].wasPressedThisFrame) OnEraseHotkey?.Invoke();
            if (Keyboard.current[hotkeySwap].wasPressedThisFrame) OnSwapHotkey?.Invoke();
            if (Keyboard.current[hotkeyArrowMode].wasPressedThisFrame) OnArrowModeHotkey?.Invoke();
            if (Keyboard.current[hotkeyPortalMode].wasPressedThisFrame) OnPortalBrushHotkey?.Invoke();
            if (Keyboard.current[hotkeyRedirectMode].wasPressedThisFrame) OnRedirectBrushHotkey?.Invoke();
            if (Keyboard.current[hotkeyCounterBlockMode].wasPressedThisFrame) OnCounterBlockBrushHotkey?.Invoke();
            if (Keyboard.current[hotkeyKeyBoxMode].wasPressedThisFrame) OnKeyBoxBrushHotkey?.Invoke();
            if (Keyboard.current[hotkeyTwoHeadMode].wasPressedThisFrame) OnTwoHeadModeHotkey?.Invoke();
            if (Keyboard.current[hotkeyLinkMode].wasPressedThisFrame) OnLinkModeHotkey?.Invoke();

            if (Keyboard.current[hotkeyCancelMechanicMode].wasPressedThisFrame &&
                arrowMechanicMode != EditorArrowMechanicMode.None)
            {
                OnCancelArrowMechanicModeHotkey?.Invoke();
            }

            if (Keyboard.current[hotkeyRotateDirection].wasPressedThisFrame) OnRotateDirectionHotkey?.Invoke();

            if (Keyboard.current[hotkeyCyclePortalId].wasPressedThisFrame)
            {
                if (arrowMechanicMode == EditorArrowMechanicMode.Link)
                {
                    OnCreateLinkGroupHotkey?.Invoke();
                }
                else
                {
                    OnCyclePortalIdHotkey?.Invoke();
                }
            }

            if (Keyboard.current[hotkeySelectPrevArrow].wasPressedThisFrame)
            {
                if (isKeyBoxMode) OnToggleKeyBoxModeHotkey?.Invoke();
                else OnSelectPreviousArrowHotkey?.Invoke();
            }

            if (Keyboard.current[hotkeySelectNextArrow].wasPressedThisFrame) OnSelectNextArrowHotkey?.Invoke();
            if (Keyboard.current[hotkeyToggleSelectedTwoHead].wasPressedThisFrame) OnToggleSelectedTwoHeadHotkey?.Invoke();
            if (Keyboard.current[hotkeyClearLinkGroup].wasPressedThisFrame ||
                Keyboard.current[hotkeyClearLinkGroupAlt].wasPressedThisFrame)
            {
                OnClearLinkGroupHotkey?.Invoke();
            }

            if (Keyboard.current[hotkeyUndo].wasPressedThisFrame) OnUndoHotkey?.Invoke();

            if (Keyboard.current[hotkeyUp].wasPressedThisFrame) OnDirectionHotkey?.Invoke(Direction4.Up);
            if (Keyboard.current[hotkeyRight].wasPressedThisFrame) OnDirectionHotkey?.Invoke(Direction4.Right);
            if (Keyboard.current[hotkeyDown].wasPressedThisFrame) OnDirectionHotkey?.Invoke(Direction4.Down);
            if (Keyboard.current[hotkeyLeft].wasPressedThisFrame) OnDirectionHotkey?.Invoke(Direction4.Left);

            if (Keyboard.current[hotkeyToggleLeftPanel].wasPressedThisFrame) OnToggleLeftPanelHotkey?.Invoke();
            if (Keyboard.current[hotkeyToggleRightPanel].wasPressedThisFrame) OnToggleRightPanelHotkey?.Invoke();
        }

        private bool HandleArrowMechanicMapInput()
        {
            if (arrowMechanicMode == EditorArrowMechanicMode.None) return false;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!TryGetHoveredArrowId(out string arrowId))
                {
                    return true;
                }

                if (arrowMechanicMode == EditorArrowMechanicMode.TwoHead)
                {
                    OnArrowTwoHeadClicked?.Invoke(arrowId);
                    return true;
                }

                if (arrowMechanicMode == EditorArrowMechanicMode.Link)
                {
                    OnArrowLinkClicked?.Invoke(arrowId, IsCtrlPressed());
                    return true;
                }
            }

            return true;
        }

        private void HandleMapSelection()
        {
            Vector2Int gridPos = GetMouseGridPosition();

            SpecialCellSaveData special = LevelMakerManager.Instance.GridSystem.GetSpecialCellAt(gridPos.x, gridPos.y);
            if (special != null)
            {
                isSelectMode = false;
                OnSpecialSelectedFromMap?.Invoke(special);
                return;
            }

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
            int counter = currentSpecialType == BoardSpecialType.CounterBlock ? Mathf.Max(1, currentCounterBlockValue) : 0;
            string portalId = currentSpecialType == BoardSpecialType.CounterBlock ? counter.ToString() : currentPortalId;

            SpecialCellSaveData wrapped = null;
            if (currentSpecialType == BoardSpecialType.MysteryBox && mysteryBoxHasWrappedCell)
            {
                if (mysteryBoxWrappedType == BoardSpecialType.Redirect)
                {
                    wrapped = new SpecialCellSaveData(gridPos, mysteryBoxWrappedType, currentSpecialDirection);
                }
                else if (mysteryBoxWrappedType == BoardSpecialType.Portal)
                {
                    wrapped = new SpecialCellSaveData(gridPos, mysteryBoxWrappedType, currentSpecialDirection, currentPortalId);
                }
            }

            if (LevelMakerManager.Instance.GridSystem.SetSpecialCell(gridPos.x, gridPos.y, currentSpecialType,
                    currentSpecialDirection, portalId, counter, wrapped))
            {
                OnSpecialCellPlaced?.Invoke();
            }
        }

        private void ExpandSpecialCellAt(Vector2Int gridPos)
        {
            if (LevelMakerManager.Instance.GridSystem.TryExpandCounterBlock(gridPos))
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

        private bool TryGetHoveredArrowId(out string arrowId)
        {
            Vector2Int gridPos = GetMouseGridPosition();
            CellData cell = LevelMakerManager.Instance.GridSystem.GetCell(gridPos.x, gridPos.y);
            if (cell != null && !string.IsNullOrEmpty(cell.arrowID))
            {
                arrowId = cell.arrowID;
                return true;
            }

            arrowId = string.Empty;
            return false;
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

        private static bool IsCtrlPressed()
        {
            return Keyboard.current[Key.LeftCtrl].isPressed || Keyboard.current[Key.RightCtrl].isPressed;
        }

        private bool IsTypingInInputField()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null && selected.GetComponent<TMP_InputField>() != null;
        }
    }
}
