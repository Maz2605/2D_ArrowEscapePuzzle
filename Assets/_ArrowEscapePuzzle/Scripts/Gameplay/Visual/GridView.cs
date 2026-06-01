using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
using ArrowGame.Gameplay.Visual.GridComponents;
using ArrowGame.UI.Components;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    public class GridView : MonoBehaviour
    {
        [Header("--- LAYOUT ---")]
        [SerializeField] private Transform container;
        [SerializeField] private float cellSize = 1.1f;
        [SerializeField] private Vector2 gridOffset;

        [Header("--- CORE VISUAL COMPONENTS ---")]
        [SerializeField] private ArrowLineVisuals arrowVisuals;
        [SerializeField] private GridAnimator animator;
        [SerializeField] private SpecialCellVisuals specialCellVisuals;
        [SerializeField] private ArrowLinkVisuals linkVisuals;

        [Header("--- RUNTIME ROOTS ---")]
        [SerializeField] private Transform arrowRoot;
        [SerializeField] private Transform specialCellRoot;
        [SerializeField] private Transform linkRoot;
        [SerializeField] private Transform dotRoot;

        private GridSystem _logic;
        private bool _isInitialized;

        private void OnEnable()
        {
            RegisterLogicEvents();
            RegisterVisualEvents();
        }

        private void OnDisable()
        {
            UnregisterLogicEvents();
            UnregisterVisualEvents();
            _isInitialized = false;
        }

        public void Initialize(GridSystem logic, LevelSaveData levelData)
        {
            _isInitialized = false;
            _logic = logic;

            if (!HasRequiredReferences())
            {
                Debug.LogError("[GridView] Missing inspector references. Please assign Grid components and runtime roots manually.");
                return;
            }

            ClearRuntimeRoot(dotRoot);

            animator.Initialize(container, dotRoot);
            arrowVisuals.Initialize(_logic, cellSize, animator.MaxAllowedIntroDuration, arrowRoot);
            specialCellVisuals.Initialize(_logic, cellSize, specialCellRoot);
            linkVisuals.Initialize(linkRoot);
            linkVisuals.RebuildLinks(_logic, arrowVisuals.ActiveLines);

            CenterGrid();
            _isInitialized = true;
        }

        public Vector2Int WorldToGridPos(Vector3 worldPos)
        {
            Vector2 localPos = container.InverseTransformPoint(worldPos);
            return new Vector2Int(Mathf.RoundToInt(localPos.x / cellSize), Mathf.RoundToInt(localPos.y / cellSize));
        }

        public Vector3 GetCellWorldPosition(Vector2Int gridPos)
        {
            if (container == null) return Vector3.zero;
            Vector3 localPos = new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
            return container.TransformPoint(localPos);
        }

        public ArrowLineView GetArrowViewAt(Vector2Int gridPos)
        {
            return arrowVisuals != null ? arrowVisuals.GetArrowViewAt(gridPos) : null;
        }

        public bool TryPlaySpecialCellRejection(Vector2Int gridPos)
        {
            return specialCellVisuals != null && specialCellVisuals.TryPlaySpecialCellRejection(gridPos);
        }

        public ArrowLineView GetArrowViewById(string arrowId)
        {
            return arrowVisuals != null ? arrowVisuals.GetArrowViewById(arrowId) : null;
        }

        public void CullArrowView(string arrowId)
        {
            arrowVisuals?.CullArrowView(arrowId);
        }

        public void PlayLoseAnimation()
        {
            if (!_isInitialized || animator == null || arrowVisuals == null || specialCellVisuals == null) return;
            animator.PlayLoseAnimation(arrowVisuals.ActiveLines, specialCellVisuals.GetUniqueViews());
        }

        public void RestoreFromLose(float duration = 0.4f)
        {
            if (!_isInitialized || animator == null || arrowVisuals == null || specialCellVisuals == null) return;

            foreach (KeyValuePair<string, ArrowLineView> kvp in arrowVisuals.ActiveLines)
            {
                if (kvp.Value != null) kvp.Value.RestoreFromLose(duration);
            }

            specialCellVisuals.PlayRestoreFromLoseAnimation(duration);
        }

        private void RegisterLogicEvents()
        {
            EventManager<LogicGameEventID>.AddListener<ArrowActivationResult>(LogicGameEventID.ArrowEscaped, HandleArrowEscaped);
            EventManager<LogicGameEventID>.AddListener<ArrowActivationResult>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<LogicGameEventID>.AddListener<List<ArrowData>>(LogicGameEventID.ArrowForceRemove, HandleArrowForceRemove);
            EventManager<LogicGameEventID>.AddListener<Vector2Int>(LogicGameEventID.SpecialCellDestroyed, HandleSpecialCellDestroyed);
            EventManager<LogicGameEventID>.AddListener<(SpecialCellSaveData, Vector2Int)>(LogicGameEventID.SpecialCellChanged, HandleSpecialCellChanged);
        }

        private void UnregisterLogicEvents()
        {
            EventManager<LogicGameEventID>.RemoveListener<ArrowActivationResult>(LogicGameEventID.ArrowEscaped, HandleArrowEscaped);
            EventManager<LogicGameEventID>.RemoveListener<ArrowActivationResult>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<LogicGameEventID>.RemoveListener<List<ArrowData>>(LogicGameEventID.ArrowForceRemove, HandleArrowForceRemove);
            EventManager<LogicGameEventID>.RemoveListener<Vector2Int>(LogicGameEventID.SpecialCellDestroyed, HandleSpecialCellDestroyed);
            EventManager<LogicGameEventID>.RemoveListener<(SpecialCellSaveData, Vector2Int)>(LogicGameEventID.SpecialCellChanged, HandleSpecialCellChanged);
        }

        private void RegisterVisualEvents()
        {
            EventManager<VisualEventID>.AddListener<ThemeConfigSO>(VisualEventID.ThemeChanged, HandleThemeChanged);
            // EventManager<VisualEventID>.AddListener(VisualEventID.TapArrowHit, HandleTapArrowHit);
            EventManager<VisualEventID>.AddListener(VisualEventID.CameraMoved, HandleCameraMoved);
            EventManager<VisualEventID>.AddListener<ArrowPathVisualTrigger>(VisualEventID.ArrowPassedGridPosition, HandleArrowPassedGridPosition);
        }

        private void UnregisterVisualEvents()
        {
            EventManager<VisualEventID>.RemoveListener<ThemeConfigSO>(VisualEventID.ThemeChanged, HandleThemeChanged);
            // EventManager<VisualEventID>.RemoveListener(VisualEventID.TapArrowHit, HandleTapArrowHit);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.CameraMoved, HandleCameraMoved);
            EventManager<VisualEventID>.RemoveListener<ArrowPathVisualTrigger>(VisualEventID.ArrowPassedGridPosition, HandleArrowPassedGridPosition);
        }

        private void HandleArrowEscaped(ArrowActivationResult activationResult)
        {
            if (!_isInitialized || arrowVisuals == null || linkVisuals == null || animator == null) return;

            List<ArrowVisualRemovalContext> removed = arrowVisuals.HandleArrowEscaped(activationResult,
                group => animator.PlayEmptyDotsForGroup(group, cellSize),
                context => linkVisuals.RemoveArrow(context.ArrowId, context.View));
        }

        private void HandleArrowForceRemove(List<ArrowData> removedGroup)
        {
            if (!_isInitialized || arrowVisuals == null || linkVisuals == null || animator == null) return;

            ArrowVisualRemovalContext? removed = arrowVisuals.HandleArrowForceRemove(removedGroup);
            if (!removed.HasValue) return;

            ArrowVisualRemovalContext context = removed.Value;
            linkVisuals.RemoveArrow(context.ArrowId, context.View);
            animator.PlayEmptyDotsForGroup(context.GroupSnapshot, cellSize);
        }

        public void PlayDashEscape(string id)
        {
            if (!_isInitialized || arrowVisuals == null || linkVisuals == null || animator == null) return;

            ArrowVisualRemovalContext? removed = arrowVisuals.HandlePlayDashEscape(id,
                group => animator.PlayEmptyDotsForGroup(group, cellSize));
            if (!removed.HasValue) return;

            ArrowVisualRemovalContext context = removed.Value;
            linkVisuals.RemoveArrow(context.ArrowId, context.View);
        }

        private void HandleSpecialCellChanged((SpecialCellSaveData data, Vector2Int dir) payload)
        {
            if (_isInitialized)
            {
                specialCellVisuals?.HandleSpecialCellChanged(payload);
            }
        }

        private void HandleSpecialCellDestroyed(Vector2Int pos)
        {
            if (!_isInitialized || specialCellVisuals == null || animator == null) return;

            specialCellVisuals.HandleSpecialCellDestroyed(pos,
                positions => animator.PlayEmptyDotsAtPositions(positions, cellSize));
        }

        private void HandleThemeChanged(ThemeConfigSO newTheme)
        {
            if (!_isInitialized) return;

            arrowVisuals?.ApplyTheme(newTheme);
            specialCellVisuals?.ApplyTheme(newTheme);
            linkVisuals?.RefreshColors();
        }

        private void HandleArrowPassedGridPosition(ArrowPathVisualTrigger trigger)
        {
            if (_isInitialized)
            {
                specialCellVisuals?.HandleArrowPassedGridPosition(trigger);
            }
        }

        private void HandleArrowBlocked(ArrowActivationResult activationResult)
        {
            if (!_isInitialized || activationResult == null || activationResult.Entries == null ||
                activationResult.Entries.Count == 0 || arrowVisuals == null)
            {
                return;
            }

            ArrowActivationEntry firstBlockedEntry = activationResult.GetFirstBlockedEntry();
            firstBlockedEntry ??= activationResult.Entries[0];

            for (int i = 0; i < activationResult.Entries.Count; i++)
            {
                ArrowActivationEntry entry = activationResult.Entries[i];
                if (entry == null || !arrowVisuals.TryGetActiveLine(entry.ArrowId, out ArrowLineView lineView)) continue;

                if (entry.Endpoint != null)
                {
                    lineView.SetActiveEndpoint(entry.Endpoint.PathIndex);
                }

                arrowVisuals.ApplyTraceToView(entry.ArrowId, lineView, entry.TraceResult);
                if (entry == firstBlockedEntry)
                {
                    PlayBlockedFeedback(entry, lineView);
                }
                else if (activationResult.IsLinkedGroup)
                {
                    lineView.PlayCollisionFlash();
                }
            }
        }

        public void ShowHint(string arrowId)
        {
            if (_isInitialized)
            {
                arrowVisuals?.ShowHintVisual(arrowId);
            }
        }

        public void ToggleDirectionLines(bool isOn)
        {
            if (_isInitialized)
            {
                arrowVisuals?.ToggleDirectionLines(isOn);
            }
        }

        private void HandleCameraMoved()
        {
            if (_isInitialized)
            {
                arrowVisuals?.HandleCameraMoved();
            }
        }

        public void SetBoosterTargetMode(bool isSelecting)
        {
            if (_isInitialized)
            {
                arrowVisuals?.HandleBoosterTargetModeChanged(isSelecting);
            }
        }

        public void ShowFocus(string id)
        {
            if (_isInitialized)
            {
                arrowVisuals?.ShowFocusHighlight(id);
            }
        }

        public void HideFocus(string id)
        {
            if (_isInitialized)
            {
                arrowVisuals?.HideFocusHighlight(id);
            }
        }

        public void SetBoardDarken(bool isDarkened)
        {
            BoosterOverlayUI.Instance?.SetBoardDarken(isDarkened);
        }

        private void HandleTapArrowHit()
        {
            if (_isInitialized)
            {
                animator?.PlayGridImpactBounce();
            }
        }

        private void HandleInGameStateChanged(InGameState state)
        {
            if (!_isInitialized || _logic == null || arrowVisuals == null || specialCellVisuals == null || animator == null)
            {
                return;
            }

            switch (state)
            {
                case InGameState.WinAnimating:
                    animator.PlayWinAnimation(arrowVisuals.ActiveLines, specialCellVisuals.GetUniqueViews(),
                        _logic.Width, _logic.Height, cellSize);
                    break;
                case InGameState.Intro:
                    animator.PlayIntroLevelAnimation(arrowVisuals.ActiveLines, specialCellVisuals.GetUniqueViews());
                    break;
                case InGameState.LoseAnimating:
                    animator.PlayLoseAnimation(arrowVisuals.ActiveLines, specialCellVisuals.GetUniqueViews());
                    break;
            }
        }

        private void CenterGrid()
        {
            if (_logic == null || container == null) return;

            float totalWidth = (_logic.Width - 1) * cellSize;
            float totalHeight = (_logic.Height - 1) * cellSize;
            container.localPosition = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0f) + (Vector3)gridOffset;
        }

        private void PlayBlockedFeedback(ArrowActivationEntry entry, ArrowLineView lineView)
        {
            EscapeTraceResult trace = entry.TraceResult;
            ArrowData headData = entry.GetHeadSnapshot();
            int travelCells = trace != null
                ? trace.DistanceBeforeStop
                : (headData != null ? _logic.GetEmptyCellsBeforeBlock(headData) : 0);
            float realBumpDistance = (travelCells * cellSize) + 0.45f;
            ArrowLineView blockerView = null;
            CounterBlockView counterBlockView = null;
            Vector2Int counterBlockHitDirection = Vector2Int.zero;
            Color counterBlockBlockedColor = default;

            if (trace != null && !string.IsNullOrEmpty(trace.BlockerId))
            {
                arrowVisuals.TryGetActiveLine(trace.BlockerId, out blockerView);
            }

            if (trace != null && trace.BlockReason == EscapeBlockReason.CounterBlock)
            {
                Vector2Int finalDirection = trace.FinalDirection.ToVector2Int();
                Vector2Int blockerPos;
                if (trace.RouteWaypoints != null && trace.RouteWaypoints.Count > 0)
                {
                    Vector2Int lastWaypointPos = trace.RouteWaypoints[trace.RouteWaypoints.Count - 1].Position;
                    blockerPos = lastWaypointPos + finalDirection;
                }
                else
                {
                    Vector2Int headPos = entry.Endpoint != null ? entry.Endpoint.Position : Vector2Int.zero;
                    blockerPos = headPos + finalDirection;
                }

                if (specialCellVisuals != null &&
                    specialCellVisuals.TryGetCounterBlockView(blockerPos, out counterBlockView))
                {
                    counterBlockHitDirection = finalDirection;
                    ThemeConfigSO theme = ThemeManager.Instance.CurrentTheme;
                    counterBlockBlockedColor = theme != null ? theme.arrowBlockedColor : Color.white;
                }
            }

            lineView.PlayBlockedAnimation(realBumpDistance, () =>
            {
                if (blockerView != null && blockerView.gameObject.activeInHierarchy)
                {
                    blockerView.PlayCollisionFlash();
                }

                if (counterBlockView != null && counterBlockView.gameObject.activeInHierarchy)
                {
                    counterBlockView.PlayHitAnimation(counterBlockHitDirection, counterBlockBlockedColor);
                }
            });
        }

        private bool HasRequiredReferences()
        {
            return container != null &&
                   arrowVisuals != null &&
                   animator != null &&
                   specialCellVisuals != null &&
                   linkVisuals != null &&
                   arrowRoot != null &&
                   specialCellRoot != null &&
                   linkRoot != null &&
                   dotRoot != null;
        }

        private static void ClearRuntimeRoot(Transform root)
        {
            if (root == null) return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                child.DOKill();
                if (child.gameObject.activeInHierarchy)
                {
                    PoolingManager.Instance.Despawn(child.gameObject);
                }
            }
        }
    }
}
