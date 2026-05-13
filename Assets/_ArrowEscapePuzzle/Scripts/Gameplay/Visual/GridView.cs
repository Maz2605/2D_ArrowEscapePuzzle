using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
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
        [Header("--- REFERENCES ---")] [SerializeField]
        private ArrowLineView arrowLinePrefab;

        [SerializeField] private Transform container;
        [SerializeField] private GameObject emptyDotPrefab;

        [Header("--- SPECIAL CELL VISUALS ---")] [SerializeField]
        private List<SpecialCellVisualPrefabSlot> specialCellVisualPrefabs =
            new List<SpecialCellVisualPrefabSlot>();

        [Header("--- 1. GRID BASE SETTINGS ---")] [SerializeField]
        private float cellSize = 1.1f;

        [SerializeField] private Vector2 gridOffset;

        [Header("--- 2. INTRO LEVEL ANIMATION ---")] [SerializeField]
        private float introSpawnDuration = 0.6f;

        [SerializeField] private float introSpawnDelayFactor = 0.05f;
        [SerializeField] private float maxAllowedIntroDuration = 2.0f;

        [Header("--- 3. DOT APPEAR ANIMATION ---")] [SerializeField]
        private float dotAppearInitialDelay = 0.1f;

        [SerializeField] private float dotAppearDuration = 0.4f;
        [SerializeField] private float delayBetweenDots = 0.15f;
        [SerializeField] private Ease dotAppearEase = Ease.OutBack;
        [SerializeField] private float dotTargetScale = 1f;

        [Header("--- 4. ARROW BLOCKED ANIMATION ---")] [SerializeField]
        private float blockedBumpOffset = 0.45f;

        [Header("--- 5. LOSE ANIMATION ---")] [SerializeField]
        private float gridSagOffset = -0.15f;

        [SerializeField] private float gridSagDuration = 0.6f;
        [SerializeField] private float delayBeforeLoseEvent = 1.0f;

        [Header("--- 6. WIN ANIMATION ---")] [SerializeField]
        private float winWaveDelayFactor = 0.12f;

        [SerializeField] private float winJumpHeight = 0.5f;
        [SerializeField] private float winScaleMax = 1.15f;
        [SerializeField] private float winJumpUpDuration = 0.25f;
        [SerializeField] private float winFallDownDuration = 0.35f;
        [SerializeField] private float winCompleteDelay = 0.3f;

        private const float DirectionLineToggleDelayFactor = 0.05f;
        private const float DirectionLineToggleDuration = 0.4f;

        private GridSystem _logic;
        private Dictionary<string, ArrowLineView> _activeLines;
        private Sequence _winSequence;
        private Transform _specialMarkerRoot;
        private readonly Dictionary<Vector2Int, SpecialCellViewBase> _specialCellViews = new Dictionary<Vector2Int, SpecialCellViewBase>();

        private void Awake()
        {
            EventManager<LogicGameEventID>.AddListener<ArrowActivationResult>(LogicGameEventID.ArrowEscaped, HandleArrowEscaped);
            EventManager<LogicGameEventID>.AddListener<ArrowActivationResult>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<LogicGameEventID>.AddListener<List<ArrowData>>(LogicGameEventID.ArrowForceRemove, HandleArrowForceRemove);
            EventManager<LogicGameEventID>.AddListener<Vector2Int>(LogicGameEventID.SpecialCellDestroyed, HandleSpecialCellDestroyed);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.ShowHintVisual, HandleShowHintVisual);
            EventManager<VisualEventID>.AddListener<bool>(VisualEventID.ShowDirectionLines, HandleToggleDirectionLines);
            EventManager<VisualEventID>.AddListener<bool>(VisualEventID.BoosterTargetModeChanged, HandleBoosterTargetModeChanged);
            EventManager<VisualEventID>.AddListener<ThemeConfigSO>(VisualEventID.ThemeChanged, HandleThemeChanged);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.ShowFocusHighlight, HandleShowFocusHighlight);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.HideFocusHighlight, HandleHideFocusHighlight);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.PlayDashEscape, HandlePlayDashEscape);
            EventManager<VisualEventID>.AddListener(VisualEventID.TapArrowHit, HandleTapArrowHit);
            EventManager<VisualEventID>.AddListener(VisualEventID.CameraMoved, HandleCameraMoved);
            EventManager<VisualEventID>.AddListener<Vector2Int>(VisualEventID.ArrowPassedGridPosition, HandleArrowPassedGridPosition);
            EventManager<LogicGameEventID>.AddListener<(SpecialCellSaveData, Vector2Int)>(LogicGameEventID.SpecialCellChanged, HandleSpecialCellChanged);        
        }

        private void OnDestroy()
        {
            EventManager<LogicGameEventID>.RemoveListener<ArrowActivationResult>(LogicGameEventID.ArrowEscaped, HandleArrowEscaped);
            EventManager<LogicGameEventID>.RemoveListener<ArrowActivationResult>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChanged);
            EventManager<LogicGameEventID>.RemoveListener<List<ArrowData>>(LogicGameEventID.ArrowForceRemove, HandleArrowForceRemove);
            EventManager<LogicGameEventID>.RemoveListener<Vector2Int>(LogicGameEventID.SpecialCellDestroyed, HandleSpecialCellDestroyed);
            EventManager<VisualEventID>.RemoveListener<string>(VisualEventID.ShowHintVisual, HandleShowHintVisual);
            EventManager<VisualEventID>.RemoveListener<bool>(VisualEventID.ShowDirectionLines, HandleToggleDirectionLines);
            EventManager<VisualEventID>.RemoveListener<bool>(VisualEventID.BoosterTargetModeChanged, HandleBoosterTargetModeChanged);
            EventManager<VisualEventID>.RemoveListener<ThemeConfigSO>(VisualEventID.ThemeChanged, HandleThemeChanged);
            EventManager<VisualEventID>.RemoveListener<string>(VisualEventID.ShowFocusHighlight, HandleShowFocusHighlight);
            EventManager<VisualEventID>.RemoveListener<string>(VisualEventID.HideFocusHighlight, HandleHideFocusHighlight);
            EventManager<VisualEventID>.RemoveListener<string>(VisualEventID.PlayDashEscape, HandlePlayDashEscape);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.TapArrowHit, HandleTapArrowHit);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.CameraMoved, HandleCameraMoved);
            EventManager<LogicGameEventID>.RemoveListener<(SpecialCellSaveData, Vector2Int)>(LogicGameEventID.SpecialCellChanged, HandleSpecialCellChanged);            
            _winSequence?.Kill();
            transform.DOKill();
            if (container != null) container.DOKill();
        }

        private void HandleSpecialCellChanged((SpecialCellSaveData data, Vector2Int dir) payload)
        {
            if (payload.data != null && _specialCellViews.TryGetValue(payload.data.Position, out SpecialCellViewBase view))
            {
                if (view is CounterBlockView counterView)
                {
                    counterView.UpdateCounter(payload.data.Counter);
                }
            }
        }

        private void HandleSpecialCellDestroyed(Vector2Int pos)
        {
            if (!_specialCellViews.TryGetValue(pos, out SpecialCellViewBase view) || view == null) return;

            List<Vector2Int> destroyedFootprint = GetSpecialCellFootprintPositions(view.BoundSpecialCell);
            RemoveSpecialCellViewReferences(view);

            if (view is CounterBlockView counterBlockView)
            {
                counterBlockView.PlayDestroyAnimation(() => PlayEmptyDotsAtPositions(destroyedFootprint));
                return;
            }

            Destroy(view.gameObject);
        }

        public void Initialize(GridSystem logic, LevelSaveData levelData)
        {
            _logic = logic;
            _activeLines = new Dictionary<string, ArrowLineView>();

            EnsureSpecialMarkerRoot();
            SpawnGrid();
            SpawnSpecialMarkers(levelData);
            CenterGrid();
        }

        private void SpawnGrid()
        {
            _winSequence?.Kill();
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (_specialMarkerRoot != null && child == _specialMarkerRoot) continue;

                child.DOKill();
                if (child.gameObject.activeInHierarchy)
                {
                    PoolingManager.Instance.Despawn(child.gameObject);
                }
            }

            _activeLines.Clear();

            ThemeConfigSO currentTheme = ThemeManager.Instance.CurrentTheme;

            foreach (KeyValuePair<string, List<ArrowData>> kvp in _logic.ArrowGroups)
            {
                ArrowLineView lineView = PoolingManager.Instance.Spawn(arrowLinePrefab, Vector3.zero, Quaternion.identity, container);
                lineView.transform.localPosition = Vector3.zero;

                ArrowModel arrowModel = _logic.GetArrowModel(kvp.Key);
                
                // MỌI THỨ CỰC KỲ SẠCH: Chỉ cần vứt Model, Path và Theme cho LineView tự lo
                lineView.Setup(arrowModel, kvp.Value, cellSize, currentTheme);

                _activeLines.Add(kvp.Key, lineView);
            }
        }

        private void SpawnSpecialMarkers(LevelSaveData levelData)
        {
            ClearSpecialMarkers();
            if (_logic == null || _logic.SpecialCells == null || _logic.SpecialCells.Count == 0) return;

            EnsureSpecialMarkerRoot();

            foreach (SpecialCellSaveData specialCell in _logic.SpecialCells)
            {
                if (specialCell == null) continue;

                GameObject markerPrefab = GetSpecialMarkerPrefab(specialCell.Type);
                GameObject markerObject = markerPrefab != null
                    ? Instantiate(markerPrefab, _specialMarkerRoot, false)
                    : new GameObject($"Special_{specialCell.Type}_{specialCell.Position.x}_{specialCell.Position.y}");

                markerObject.name = $"Special_{specialCell.Type}_{specialCell.Position.x}_{specialCell.Position.y}";
                if (markerPrefab == null)
                {
                    markerObject.transform.SetParent(_specialMarkerRoot, false);
                }

                SpecialCellViewBase markerView = GetOrAddSpecialCellView(markerObject, specialCell.Type);
                markerView.Setup(specialCell, cellSize, GetSpecialCellColor(specialCell));
                _specialCellViews[specialCell.Position] = markerView;
                if (specialCell.OccupiedOffsets != null)
                {
                    foreach (Vector2Int offset in specialCell.OccupiedOffsets)
                    {
                        if (offset == Vector2Int.zero) continue;
                        Vector2Int targetPos = specialCell.Position + offset;
                        _specialCellViews[targetPos] = markerView;
                    }
                }
            }
        }

        private void EnsureSpecialMarkerRoot()
        {
            if (_specialMarkerRoot != null) return;

            GameObject root = new GameObject("SpecialMarkers");
            _specialMarkerRoot = root.transform;
            _specialMarkerRoot.SetParent(container, false);
            _specialMarkerRoot.SetAsFirstSibling();
        }

        private void ClearSpecialMarkers()
        {
            HashSet<SpecialCellViewBase> uniqueViews = new HashSet<SpecialCellViewBase>(_specialCellViews.Values);
            foreach (SpecialCellViewBase view in uniqueViews)
            {
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            _specialCellViews.Clear();
        }

        private void RemoveSpecialCellViewReferences(SpecialCellViewBase targetView)
        {
            if (targetView == null || _specialCellViews.Count == 0) return;

            List<Vector2Int> keysToRemove = null;
            foreach (KeyValuePair<Vector2Int, SpecialCellViewBase> kvp in _specialCellViews)
            {
                if (kvp.Value != targetView) continue;

                keysToRemove ??= new List<Vector2Int>();
                keysToRemove.Add(kvp.Key);
            }

            if (keysToRemove == null) return;

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _specialCellViews.Remove(keysToRemove[i]);
            }
        }

        private void HandleThemeChanged(ThemeConfigSO newTheme)
        {
            if (_activeLines != null)
            {
                foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
                {
                    // CLEAN CODE: ArrowLineView tự nhận Theme và cập nhật
                    kvp.Value.UpdateThemeColor(newTheme);
                }
            }

            HashSet<SpecialCellViewBase> updatedViews = new HashSet<SpecialCellViewBase>(_specialCellViews.Values);
            foreach (SpecialCellViewBase view in updatedViews)
            {
                if (view?.BoundSpecialCell == null) continue;
                view.RefreshVisualColor(GetSpecialCellColor(view.BoundSpecialCell, newTheme));
            }
        }

        private GameObject SpawnSingleDot(int x, int y)
        {
            if (emptyDotPrefab == null) return null;

            GameObject dot = PoolingManager.Instance.Spawn(emptyDotPrefab, Vector3.zero, Quaternion.identity, container);
            dot.transform.DOKill();
            dot.transform.localScale = Vector3.zero;
            dot.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0f);
            dot.transform.SetAsFirstSibling();
            return dot;
        }

        public Vector2Int WorldToGridPos(Vector3 worldPos)
        {
            Vector2 localPos = container.InverseTransformPoint(worldPos);
            return new Vector2Int(Mathf.RoundToInt(localPos.x / cellSize), Mathf.RoundToInt(localPos.y / cellSize));
        }

        private void HandleArrowPassedGridPosition(Vector2Int gridPos)
        {
            if (_specialCellViews.TryGetValue(gridPos, out SpecialCellViewBase view))
            {
                view.PlayHighlight();
            }
        }

        private void HandleArrowEscaped(ArrowActivationResult activationResult)
        {
            if (activationResult == null || activationResult.Entries == null) return;

            for (int i = 0; i < activationResult.Entries.Count; i++)
            {
                ArrowActivationEntry entry = activationResult.Entries[i];
                if (entry == null || !_activeLines.TryGetValue(entry.ArrowId, out ArrowLineView lineView)) continue;

                if (entry.Endpoint != null) 
                {
                    lineView.SetActiveEndpoint(entry.Endpoint.PathIndex);
                }

                ApplyTraceToView(entry.ArrowId, lineView, entry.TraceResult);
                lineView.PlayEscapeAnimation(onEscapeStart: () => PlayEmptyDotsForGroup(entry.GroupSnapshot));
                _activeLines.Remove(entry.ArrowId);
            }
        }

        private void HandleArrowBlocked(ArrowActivationResult activationResult)
        {
            if (activationResult == null || activationResult.Entries == null || activationResult.Entries.Count == 0)
            {
                return;
            }

            ArrowActivationEntry firstBlockedEntry = activationResult.GetFirstBlockedEntry();
            firstBlockedEntry ??= activationResult.Entries[0];

            for (int i = 0; i < activationResult.Entries.Count; i++)
            {
                ArrowActivationEntry entry = activationResult.Entries[i];
                if (entry == null || !_activeLines.TryGetValue(entry.ArrowId, out ArrowLineView lineView)) continue;

                // ---> THÊM VÀO ĐÂY: Đồng bộ hướng trước khi diễn hoạt Blocked
                if (entry.Endpoint != null) 
                {
                    lineView.SetActiveEndpoint(entry.Endpoint.PathIndex);
                }

                ApplyTraceToView(entry.ArrowId, lineView, entry.TraceResult);
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

        private void HandleArrowForceRemove(List<ArrowData> removedGroup)
        {
            if (removedGroup == null || removedGroup.Count == 0) return;

            string targetID = removedGroup[0].ID;

            if (_activeLines.TryGetValue(targetID, out ArrowLineView lineView))
            {
                _activeLines.Remove(targetID);

                if (lineView != null && lineView.gameObject.activeInHierarchy)
                {
                    lineView.OnDespawn();
                }

                PlayEmptyDotsForGroup(removedGroup);
            }
        }

        private void HandleShowHintVisual(string arrowID)
        {
            if (_activeLines == null || _activeLines.Count == 0) return;

            if (_activeLines.TryGetValue(arrowID, out ArrowLineView view))
            {
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    ApplyTraceToView(arrowID, view, _logic.GetLiveTraceResult(arrowID));
                    view.PlayHintEffect();
                }
            }
        }

        private void HandleToggleDirectionLines(bool isOn)
        {
            if (_activeLines == null || _activeLines.Count == 0) return;

            if (!isOn)
            {
                foreach (ArrowLineView view in _activeLines.Values)
                {
                    if (view != null && view.gameObject.activeInHierarchy)
                    {
                        view.ClearTraceRoute();
                        view.ForceToggleDirectionLine(false, 0f);
                    }
                }

                return;
            }

            int activeLineCount = 0;
            foreach (ArrowLineView view in _activeLines.Values)
            {
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    activeLineCount++;
                }
            }

            int batchSize = CalculateStaggerBatchSize(activeLineCount, DirectionLineToggleDuration, DirectionLineToggleDelayFactor, maxAllowedIntroDuration);
            float currentDelay = 0f;
            int currentBatchCount = 0;

            foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
            {
                ArrowLineView view = kvp.Value;
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    ApplyTraceToView(kvp.Key, view, _logic.GetLiveTraceResult(kvp.Key));
                    view.ForceToggleDirectionLine(true, currentDelay);
                    currentBatchCount++;
                    if (currentBatchCount >= batchSize)
                    {
                        currentDelay += DirectionLineToggleDelayFactor;
                        currentBatchCount = 0;
                    }
                }
            }
        }

        private void HandleCameraMoved()
        {
            if (_activeLines == null || _activeLines.Count == 0) return;

            foreach (ArrowLineView view in _activeLines.Values)
            {
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    view.UpdateDirectionLineIfEnabled();
                }
            }
        }

        private void HandleBoosterTargetModeChanged(bool isSelecting)
        {
            foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ToggleTargetSelectionState(isSelecting);
                }
            }
        }

        private void HandleShowFocusHighlight(string id)
        {
            if (_activeLines == null || _activeLines.Count == 0) return;
            if (_activeLines.TryGetValue(id, out ArrowLineView view))
            {
                if (view != null && view.gameObject.activeInHierarchy) view.PlayFocusHighlight(true);
            }
        }

        private void HandleHideFocusHighlight(string id)
        {
            if (_activeLines == null || _activeLines.Count == 0) return;
            if (_activeLines.TryGetValue(id, out ArrowLineView view))
            {
                if (view != null && view.gameObject.activeInHierarchy) view.PlayFocusHighlight(false);
            }
        }

        private void HandleTapArrowHit()
        {
            // PlayGridImpactBounce();
        }

        private void PlayGridImpactBounce()
        {
            container.DOKill(false);
            Sequence bounce = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            bounce.Append(container.DOScale(0.975f, 0.07f).SetEase(Ease.OutCubic));
            bounce.Append(container.DOScale(1f, 0.14f).SetEase(Ease.OutBack));
        }

        private void HandlePlayDashEscape(string id)
        {
            if (string.IsNullOrEmpty(id) || _activeLines == null) return;

            List<ArrowData> dashGroup = null;
            if (_logic != null && _logic.ArrowGroups.TryGetValue(id, out List<ArrowData> group))
            {
                dashGroup = group;
            }

            if (_activeLines.TryGetValue(id, out ArrowLineView view))
            {
                ApplyTraceToView(id, view, _logic.GetLiveTraceResult(id));
                view.PlayFocusHighlight(false);
                view.PlayEscapeAnimation(onEscapeStart: () => PlayEmptyDotsForGroup(dashGroup));
                _activeLines.Remove(id);
            }
        }

        private void PlayEmptyDotsForGroup(IReadOnlyList<ArrowData> arrowGroup)
        {
            if (arrowGroup == null || arrowGroup.Count == 0) return;

            List<Vector2Int> positions = new List<Vector2Int>(arrowGroup.Count);
            for (int i = 0; i < arrowGroup.Count; i++)
            {
                ArrowData arrow = arrowGroup[i];
                positions.Add(new Vector2Int(arrow.X, arrow.Y));
            }

            PlayEmptyDotsAtPositions(positions);
        }

        private void PlayEmptyDotsAtPositions(List<Vector2Int> positions)
        {
            if (positions == null || positions.Count == 0) return;

            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int pos = positions[i];
                GameObject dot = SpawnSingleDot(pos.x, pos.y);
                if (dot == null) continue;

                float delayTime = dotAppearInitialDelay + (i * delayBetweenDots);
                dot.transform.DOScale(Vector3.one * dotTargetScale, dotAppearDuration)
                    .SetDelay(delayTime).SetEase(dotAppearEase).SetLink(dot, LinkBehaviour.KillOnDisable);
            }
        }

        private static List<Vector2Int> GetSpecialCellFootprintPositions(SpecialCellSaveData specialCell)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            if (specialCell == null) return positions;

            HashSet<Vector2Int> uniquePositions = new HashSet<Vector2Int>();
            foreach (Vector2Int occupiedPos in CounterBlockUtility.GetOccupiedPositions(specialCell))
            {
                if (uniquePositions.Add(occupiedPos))
                {
                    positions.Add(occupiedPos);
                }
            }

            positions.Sort((a, b) =>
            {
                int yCompare = b.y.CompareTo(a.y);
                return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
            });

            return positions;
        }

        private void CenterGrid()
        {
            float totalWidth = (_logic.Width - 1) * cellSize;
            float totalHeight = (_logic.Height - 1) * cellSize;
            container.localPosition = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0f) + (Vector3)gridOffset;
        }

        private void PlayIntroLevelAnimation()
        {
            int totalArrows = _activeLines.Count;
            int spawnBatchSize = totalArrows > 0
                ? CalculateStaggerBatchSize(totalArrows, introSpawnDuration, introSpawnDelayFactor,
                    maxAllowedIntroDuration)
                : 1;
            int completedArrows = 0;
            bool introCompletePosted = false;

            float delay = 0f;
            int currentBatchCount = 0;
            bool showLine = BoosterManager.Instance.IsLineGuideActive();

            foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
            {
                kvp.Value.PlaySpawnAnimation(delay, introSpawnDuration, showLine, () =>
                {
                    completedArrows++;
                    if (!introCompletePosted && completedArrows >= totalArrows)
                    {
                        introCompletePosted = true;
                        EventManager<VisualEventID>.Post(VisualEventID.GridIntroComplete);
                    }
                });

                currentBatchCount++;
                if (currentBatchCount >= spawnBatchSize)
                {
                    delay += introSpawnDelayFactor;
                    currentBatchCount = 0;
                }
            }

            foreach (var kvp in _specialCellViews)
            {
                if (kvp.Value != null) kvp.Value.PlaySpawnAnimation(0f, introSpawnDuration);
            }

            if (totalArrows == 0)
            {
                EventManager<VisualEventID>.Post(VisualEventID.GridIntroComplete);
            }
        }

        private int CalculateStaggerBatchSize(int totalItems, float itemDuration, float delayFactor,
            float maxAllowedDuration)
        {
            if (totalItems <= 0) return 1;

            int batchSize = 1;
            float timeIfSpawnSingle = (totalItems - 1) * delayFactor + itemDuration;

            if (timeIfSpawnSingle > maxAllowedDuration)
            {
                float availableTimeForDelays = maxAllowedDuration - itemDuration;
                int maxAllowedWaves = Mathf.FloorToInt(availableTimeForDelays / delayFactor) + 1;
                maxAllowedWaves = Mathf.Max(1, maxAllowedWaves);
                batchSize = Mathf.CeilToInt((float)totalItems / maxAllowedWaves);
            }

            return batchSize;
        }

        private void PlayWinAnimation()
        {
            float totalWidth = (_logic.Width - 1) * cellSize;
            float totalHeight = (_logic.Height - 1) * cellSize;
            Vector3 centerPos = new Vector3(totalWidth / 2f, totalHeight / 2f, 0f);

            _winSequence?.Kill();
            _winSequence = DOTween.Sequence().SetId(this).SetLink(gameObject);

            for (int i = 0; i < container.childCount; i++)
            {
                Transform dot = container.GetChild(i);
                if (_specialMarkerRoot != null && dot == _specialMarkerRoot) continue;

                float dist = Vector3.Distance(dot.localPosition, centerPos);
                float delay = dist * winWaveDelayFactor;
                Vector3 originalPos = dot.localPosition;

                Sequence dotSeq = DOTween.Sequence();
                dotSeq.Append(dot.DOLocalMoveY(originalPos.y + winJumpHeight, winJumpUpDuration).SetEase(Ease.OutQuad));
                dotSeq.Join(dot.DOScale(winScaleMax, winJumpUpDuration).SetEase(Ease.OutQuad));
                dotSeq.Append(dot.DOLocalMoveY(originalPos.y, winFallDownDuration).SetEase(Ease.InQuad));
                dotSeq.Join(dot.DOScale(1f, winFallDownDuration).SetEase(Ease.OutBounce));

                _winSequence.Insert(delay, dotSeq);
                dotSeq.SetLink(dot.gameObject, LinkBehaviour.KillOnDisable);
            }

            foreach (var kvp in _specialCellViews)
            {
                if (kvp.Value != null)
                {
                    float dist = Vector3.Distance(kvp.Value.transform.localPosition, centerPos);
                    float delay = dist * winWaveDelayFactor;
                    kvp.Value.PlayWinAnimation(delay, winJumpHeight, winJumpUpDuration, winFallDownDuration, winScaleMax);
                }
            }

            _winSequence.AppendInterval(winCompleteDelay);
            _winSequence.OnComplete(() => { EventManager<VisualEventID>.Post(VisualEventID.WinAnimationComplete); });
        }

        public void PlayLoseAnimation()
        {
            foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
            {
                if (kvp.Value != null) kvp.Value.PlayLoseAnimation();
            }

            ThemeConfigSO theme = ThemeManager.Instance.CurrentTheme;
            Color loseColor = theme != null ? theme.arrowLoseColor : new Color(0.5f, 0.5f, 0.5f, 0.5f);

            foreach (var kvp in _specialCellViews)
            {
                if (kvp.Value != null) kvp.Value.PlayLoseAnimation(gridSagDuration, 0.8f, loseColor);
            }

            Vector3 originalLocalPos = container.localPosition;

            _winSequence?.Kill();
            container.DOKill();

            container.DOLocalMoveY(originalLocalPos.y + gridSagOffset, gridSagDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() => { container.localPosition = originalLocalPos; })
                .SetLink(container.gameObject);

            DOVirtual.DelayedCall(delayBeforeLoseEvent,
                    () => { EventManager<VisualEventID>.Post(VisualEventID.LoseAnimationComplete); })
                .SetId(this).SetLink(gameObject);
        }

        private void HandleInGameStateChanged(InGameState state)
        {
            switch (state)
            {
                case InGameState.WinAnimating:
                    PlayWinAnimation();
                    break;
                case InGameState.Intro:
                    PlayIntroLevelAnimation();
                    break;
                case InGameState.LoseAnimating:
                    PlayLoseAnimation();
                    break;
            }
        }

        public ArrowLineView GetArrowViewAt(Vector2Int gridPos)
        {
            ArrowData arrowData = _logic.GetArrow(gridPos.x, gridPos.y);
            if (arrowData != null && !string.IsNullOrEmpty(arrowData.ID))
            {
                if (_activeLines.TryGetValue(arrowData.ID, out ArrowLineView view))
                {
                    ArrowEndpoint endpoint = _logic.ResolveEndpointFromTap(arrowData.ID, gridPos);
                    
                    // ---> THÊM VÀO ĐÂY: Xoay hướng mũi tên ngay lúc tap/hold
                    if (endpoint != null) 
                    {
                        view.SetActiveEndpoint(endpoint.PathIndex);
                    }

                    ApplyTraceToView(arrowData.ID, view, endpoint != null
                        ? _logic.GetLiveTraceResult(arrowData.ID, endpoint)
                        : _logic.GetLiveTraceResult(arrowData.ID));
                    return view;
                }
            }
            return null;
        }

        public bool TryPlaySpecialCellRejection(Vector2Int gridPos)
        {
            if (_specialCellViews.TryGetValue(gridPos, out SpecialCellViewBase view))
            {
                if (view != null)
                {
                    view.PlayRejectionAnimation();
                    return true;
                }
            }
            return false;
        }

        private Color GetSpecialCellColor(SpecialCellSaveData specialCell, ThemeConfigSO themeOverride = null)
        {
            ThemeConfigSO theme = themeOverride ?? ThemeManager.Instance.CurrentTheme;
            
            if (specialCell.Type == BoardSpecialType.Redirect)
            {
                if (theme != null && theme.isRandomRedirectColor && theme.redirectColorPalette != null && theme.redirectColorPalette.Count > 0)
                {
                    int seed = Mathf.Abs(specialCell.Position.GetHashCode());
                    return theme.redirectColorPalette[seed % theme.redirectColorPalette.Count];
                }
                
                return theme != null ? theme.redirectDefaultColor : Color.white;
            }

            if (specialCell.Type == BoardSpecialType.CounterBlock)
            {
                return theme != null ? theme.blockerCounterColor : Color.gray;
            }

            // CLEAN CODE: Bỏ Random HSV, dùng từ Theme
            if (specialCell.Type == BoardSpecialType.Portal)
            {
                // if (theme != null && theme.portalColorPalette != null && theme.portalColorPalette.Count > 0)
                // {
                //     int portalSeed = Mathf.Abs((specialCell.PortalId ?? string.Empty).GetHashCode());
                //     return theme.portalColorPalette[portalSeed % theme.portalColorPalette.Count];
                // }
                return theme != null ? theme.portalDefaultColor : Color.magenta;
            }

            return Color.white;
        }

        private GameObject GetSpecialMarkerPrefab(BoardSpecialType type)
        {
            for (int i = 0; i < specialCellVisualPrefabs.Count; i++)
            {
                SpecialCellVisualPrefabSlot slot = specialCellVisualPrefabs[i];
                if (slot != null && slot.Type == type && slot.Prefab != null)
                    return slot.Prefab;
            }

            return null;
        }

        private static SpecialCellViewBase GetOrAddSpecialCellView(GameObject markerObject, BoardSpecialType type)
        {
            SpecialCellViewBase view = markerObject.GetComponent<SpecialCellViewBase>();
            if (view != null) return view;

            return type switch
            {
                BoardSpecialType.Portal => markerObject.AddComponent<PortalSpecialCellView>(),
                BoardSpecialType.Redirect => markerObject.AddComponent<RedirectSpecialCellView>(),
                BoardSpecialType.CounterBlock => markerObject.AddComponent<CounterBlockView>(),
                _ => markerObject.AddComponent<RedirectSpecialCellView>()
            };
        }

        public ArrowLineView GetArrowViewById(string arrowId)
        {
            if (string.IsNullOrEmpty(arrowId)) return null;
            if (_activeLines.TryGetValue(arrowId, out ArrowLineView view))
            {
                ApplyTraceToView(arrowId, view, _logic.GetLiveTraceResult(arrowId));
                return view;
            }

            return null;
        }

        public void CullArrowView(string arrowId)
        {
            if (string.IsNullOrEmpty(arrowId)) return;

            if (_activeLines.ContainsKey(arrowId))
            {
                _activeLines.Remove(arrowId);
            }
        }

        private void ForceApplyDirectionLines(bool isOn)
        {
            foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
            {
                ArrowLineView view = kvp.Value;
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    if (isOn) ApplyTraceToView(kvp.Key, view, _logic.GetLiveTraceResult(kvp.Key));
                    else view.ClearTraceRoute();
                    view.ForceToggleDirectionLine(isOn, 0f);
                }
            }
        }

        private void ApplyTraceToView(string arrowId, ArrowLineView view, EscapeTraceResult trace)
        {
            if (view == null) return;

            if (trace == null || trace.VisitedCells == null || trace.VisitedCells.Count == 0)
            {
                view.ClearTraceRoute();
                return;
            }

            view.SetTraceRoutes(trace, GetAlternateTraceForArrow(arrowId, trace));
        }

        private EscapeTraceResult GetAlternateTraceForArrow(string arrowId, EscapeTraceResult activeTrace)
        {
            if (_logic == null || string.IsNullOrEmpty(arrowId)) return null;

            IReadOnlyList<ArrowEndpoint> endpoints = _logic.GetAvailableEndpoints(arrowId);
            if (endpoints == null || endpoints.Count < 2) return null;

            ArrowEndpoint alternateEndpoint = null;
            int activePathIndex = activeTrace != null ? activeTrace.StartPathIndex : -1;

            for (int i = 0; i < endpoints.Count; i++)
            {
                ArrowEndpoint endpoint = endpoints[i];
                if (endpoint == null) continue;

                if (activePathIndex >= 0)
                {
                    if (endpoint.PathIndex != activePathIndex)
                    {
                        alternateEndpoint = endpoint;
                        break;
                    }
                }
                else if (!endpoint.IsPrimary)
                {
                    alternateEndpoint = endpoint;
                    break;
                }
            }

            if (alternateEndpoint == null)
            {
                for (int i = 0; i < endpoints.Count; i++)
                {
                    ArrowEndpoint endpoint = endpoints[i];
                    if (endpoint != null)
                    {
                        alternateEndpoint = endpoint;
                        break;
                    }
                }
            }

            if (alternateEndpoint == null) return null;
            if (activeTrace != null && alternateEndpoint.PathIndex == activeTrace.StartPathIndex) return null;

            return _logic.GetLiveTraceResult(arrowId, alternateEndpoint);
        }

        private void PlayBlockedFeedback(ArrowActivationEntry entry, ArrowLineView lineView)
        {
            EscapeTraceResult trace = entry.TraceResult;
            ArrowData headData = entry.GetHeadSnapshot();
            int travelCells = trace != null ? trace.DistanceBeforeStop : (headData != null ? _logic.GetEmptyCellsBeforeBlock(headData) : 0);
            float realBumpDistance = (travelCells * cellSize) + blockedBumpOffset;
            ArrowLineView blockerView = null;
            CounterBlockView counterBlockView = null;
            Vector2Int counterBlockHitDirection = Vector2Int.zero;
            Color counterBlockBlockedColor = default;

            if (trace != null && !string.IsNullOrEmpty(trace.BlockerId))
            {
                _activeLines.TryGetValue(trace.BlockerId, out blockerView);
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

                if (_specialCellViews.TryGetValue(blockerPos, out SpecialCellViewBase cellView))
                {
                    counterBlockView = cellView as CounterBlockView;
                    if (counterBlockView != null)
                    {
                        counterBlockHitDirection = finalDirection;
                        ThemeConfigSO theme = ThemeManager.Instance.CurrentTheme;
                        counterBlockBlockedColor = theme != null ? theme.arrowBlockedColor : Color.white;
                    }
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
    }
}
