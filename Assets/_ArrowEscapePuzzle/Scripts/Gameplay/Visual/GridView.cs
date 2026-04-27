using System.Collections.Generic;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using UnityEngine;
using DG.Tweening;
using ShareCore.Scripts.Data;

namespace ArrowGame.Gameplay.Visual
{
    public class GridView : MonoBehaviour
    {
        [Header("--- REFERENCES ---")] [SerializeField]
        private ArrowLineView arrowLinePrefab;

        [SerializeField] private Transform container;
        [SerializeField] private GameObject emptyDotPrefab;

        [Header("--- 1. GRID BASE SETTINGS ---")] [SerializeField]
        private float cellSize = 1.1f;

        [SerializeField] private Vector2 gridOffset;

        [Header("--- 2. INTRO LEVEL ANIMATION ---")]
        [SerializeField] private float introSpawnDuration = 0.6f;
        [SerializeField] private float introSpawnDelayFactor = 0.05f;
        [SerializeField] private float maxAllowedIntroDuration = 2.0f;
        [SerializeField] private float minIntroDuration = 1f;

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

        private void Awake()
        {
            EventManager<LogicGameEventID>.AddListener<List<ArrowData>>(LogicGameEventID.ArrowEscaped,
                HandleArrowEscaped);
            EventManager<LogicGameEventID>.AddListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged,
                HandleInGameStateChanged);
            EventManager<LogicGameEventID>.AddListener<List<ArrowData>>(LogicGameEventID.ArrowForceRemove,
                HandleArrowForceRemove);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.ShowHintVisual, HandleShowHintVisual);
            EventManager<VisualEventID>.AddListener<bool>(VisualEventID.ShowDirectionLines, HandleToggleDirectionLines);
            EventManager<VisualEventID>.AddListener<bool>(VisualEventID.BoosterTargetModeChanged,
                HandleBoosterTargetModeChanged);
            EventManager<VisualEventID>.AddListener<ThemeConfigSO>(VisualEventID.ThemeChanged, HandleThemeChanged);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.ShowFocusHighlight, HandleShowFocusHighlight);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.HideFocusHighlight, HandleHideFocusHighlight);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.PlayDashEscape, HandlePlayDashEscape);
        }

        private void OnDestroy()
        {
            EventManager<LogicGameEventID>.RemoveListener<List<ArrowData>>(LogicGameEventID.ArrowEscaped,
                HandleArrowEscaped);
            EventManager<LogicGameEventID>.RemoveListener<ArrowData>(LogicGameEventID.ArrowBlocked, HandleArrowBlocked);
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged,
                HandleInGameStateChanged);
            EventManager<LogicGameEventID>.RemoveListener<List<ArrowData>>(LogicGameEventID.ArrowForceRemove,
                HandleArrowForceRemove);
            EventManager<VisualEventID>.RemoveListener<string>(VisualEventID.ShowHintVisual, HandleShowHintVisual);
            EventManager<VisualEventID>.RemoveListener<bool>(VisualEventID.ShowDirectionLines,
                HandleToggleDirectionLines);
            EventManager<VisualEventID>.RemoveListener<bool>(VisualEventID.BoosterTargetModeChanged,
                HandleBoosterTargetModeChanged);
            EventManager<VisualEventID>.RemoveListener<ThemeConfigSO>(VisualEventID.ThemeChanged, HandleThemeChanged);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.ShowFocusHighlight, HandleShowFocusHighlight);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.HideFocusHighlight, HandleHideFocusHighlight);
            EventManager<VisualEventID>.AddListener<string>(VisualEventID.PlayDashEscape, HandlePlayDashEscape);
            _winSequence?.Kill();
            transform.DOKill();
            if (container != null) container.DOKill();
        }


        public void Initialize(GridSystem logic, LevelSaveData levelData)
        {
            _logic = logic;
            _activeLines = new Dictionary<string, ArrowLineView>();

            SpawnGrid();
            CenterGrid();

            bool currentLineGuideState = BoosterManager.Instance.IsLineGuideActive();
            ForceApplyDirectionLines(currentLineGuideState);
        }

        private void SpawnGrid()
        {
            _winSequence?.Kill();
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                child.DOKill();
                if (child.gameObject.activeInHierarchy)
                {
                    PoolingManager.Instance.Despawn(child.gameObject);
                }
            }

            _activeLines.Clear();

            var currentTheme = ThemeManager.Instance.CurrentTheme;

            foreach (var kvp in _logic.ArrowGroups)
            {
                string id = kvp.Key;
                List<ArrowData> sortedPath = kvp.Value;

                ArrowLineView lineView =
                    PoolingManager.Instance.Spawn(arrowLinePrefab, Vector3.zero, Quaternion.identity, container);
                lineView.transform.localPosition = Vector3.zero;

                Color assignedColor = GetAssignedColorForArrow(id, currentTheme);
                lineView.Setup(sortedPath, cellSize, assignedColor, currentTheme);

                _activeLines.Add(id, lineView);
            }
        }

        private void HandleThemeChanged(ThemeConfigSO newTheme)
        {
            if (_activeLines == null || _activeLines.Count == 0) return;
            foreach (var kvp in _activeLines)
            {
                string id = kvp.Key;
                ArrowLineView lineView = kvp.Value;

                Color newAssignedColor = GetAssignedColorForArrow(id, newTheme);
                lineView.UpdateThemeColor(newAssignedColor, newTheme);
            }
        }

        private GameObject SpawnSingleDot(int x, int y)
        {
            if (emptyDotPrefab == null) return null;

            GameObject dot =
                PoolingManager.Instance.Spawn(emptyDotPrefab, Vector3.zero, Quaternion.identity, container);
            dot.transform.DOKill();
            dot.transform.localScale = Vector3.zero;
            dot.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0);
            dot.transform.SetAsFirstSibling();
            return dot;
        }

        public Vector2Int WorldToGridPos(Vector3 worldPos)
        {
            Vector2 localPos = container.InverseTransformPoint(worldPos);
            return new Vector2Int(Mathf.RoundToInt(localPos.x / cellSize), Mathf.RoundToInt(localPos.y / cellSize));
        }

        private void HandleArrowEscaped(List<ArrowData> escapedGroup)
        {
            if (escapedGroup == null || escapedGroup.Count == 0) return;

            string targetID = escapedGroup[0].ID;

            if (_activeLines.TryGetValue(targetID, out ArrowLineView lineView))
            {
                lineView.PlayEscapeAnimation();
                _activeLines.Remove(targetID);
            }

            PlayEmptyDotsForGroup(escapedGroup);
        }

        private void HandleArrowBlocked(ArrowData headData)
        {
            if (_activeLines.TryGetValue(headData.ID, out ArrowLineView lineView))
            {
                int emptySpaces = _logic.GetEmptyCellsBeforeBlock(headData);
                float realBumpDistance = (emptySpaces * cellSize) + blockedBumpOffset;
                lineView.PlayBlockedAnimation(realBumpDistance);
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
                    view.PlayHintEffect();

                    var camController = Camera.main.GetComponent<Controllers.CameraController>();
                    if (camController != null)
                    {
                        camController.FocusOnPosition(view.HeadPosition, 0.6f);
                    }
                }
            }
        }

        private void HandleToggleDirectionLines(bool isOn)
        {
            if (_activeLines == null || _activeLines.Count == 0) return;

            if (!isOn)
            {
                foreach (var view in _activeLines.Values)
                {
                    if (view != null && view.gameObject.activeInHierarchy)
                    {
                        view.ForceToggleDirectionLine(false, 0f);
                    }
                }

                return;
            }

            int activeLineCount = 0;
            foreach (var view in _activeLines.Values)
            {
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    activeLineCount++;
                }
            }

            int batchSize = CalculateStaggerBatchSize(activeLineCount, DirectionLineToggleDuration,
                DirectionLineToggleDelayFactor, maxAllowedIntroDuration);
            float currentDelay = 0f;
            int currentBatchCount = 0;

            foreach (var view in _activeLines.Values)
            {
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    view.ForceToggleDirectionLine(isOn, currentDelay);
                    currentBatchCount++;
                    if (currentBatchCount >= batchSize)
                    {
                        currentDelay += DirectionLineToggleDelayFactor;
                        currentBatchCount = 0;
                    }
                }
            }
        }

        private void HandleBoosterTargetModeChanged(bool isSelecting)
        {
            foreach (var kvp in _activeLines)
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

        private void HandlePlayDashEscape(string id)
        {
            if (string.IsNullOrEmpty(id) || _activeLines == null) return;

            List<ArrowData> dashGroup = null;
            if (_logic != null && _logic.ArrowGroups.TryGetValue(id, out var group))
            {
                dashGroup = group;
            }

            if (_activeLines.TryGetValue(id, out var view))
            {
                view.PlayFocusHighlight(false);
                view.PlayEscapeAnimation();

                _activeLines.Remove(id);
            }

            PlayEmptyDotsForGroup(dashGroup);
        }

        private void PlayEmptyDotsForGroup(List<ArrowData> arrowGroup)
        {
            if (arrowGroup == null || arrowGroup.Count == 0) return;

            for (int i = 0; i < arrowGroup.Count; i++)
            {
                ArrowData arrow = arrowGroup[i];
                GameObject dot = SpawnSingleDot(arrow.X, arrow.Y);
                if (dot == null) continue;

                float delayTime = dotAppearInitialDelay + (i * delayBetweenDots);
                dot.transform.DOScale(Vector3.one * dotTargetScale, dotAppearDuration)
                    .SetDelay(delayTime).SetEase(dotAppearEase).SetLink(dot, LinkBehaviour.KillOnDisable);
            }
        }

        private void CenterGrid()
        {
            float totalWidth = (_logic.Width - 1) * cellSize;
            float totalHeight = (_logic.Height - 1) * cellSize;
            container.localPosition = new Vector3(-totalWidth / 2f, -totalHeight / 2f, 0) + (Vector3)gridOffset;
        }

        

        private void PlayIntroLevelAnimation()
        {
            int totalArrows = _activeLines.Count;
            if (totalArrows == 0) return;

            int spawnBatchSize =
                CalculateStaggerBatchSize(totalArrows, introSpawnDuration, introSpawnDelayFactor,
                    maxAllowedIntroDuration);

            float delay = 0f;
            int currentBatchCount = 0;

            foreach (var kvp in _activeLines)
            {
                kvp.Value.PlaySpawnAnimation(delay, introSpawnDuration);

                currentBatchCount++;
                if (currentBatchCount >= spawnBatchSize)
                {
                    delay += introSpawnDelayFactor;
                    currentBatchCount = 0;
                }
            }

            int totalBatches = Mathf.CeilToInt((float)totalArrows / spawnBatchSize);
            float arrowsDuration =
                totalBatches > 0 ? (totalBatches - 1) * introSpawnDelayFactor + introSpawnDuration : 0f;
            float totalDuration = Mathf.Max(minIntroDuration, arrowsDuration);

            DOVirtual.DelayedCall(totalDuration,
                () => { EventManager<VisualEventID>.Post(VisualEventID.IntroAnimationComplete); }).SetLink(gameObject);
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
            Vector3 centerPos = new Vector3(totalWidth / 2f, totalHeight / 2f, 0);

            _winSequence?.Kill();
            _winSequence = DOTween.Sequence().SetId(this).SetLink(gameObject);

            for (int i = 0; i < container.childCount; i++)
            {
                Transform dot = container.GetChild(i);
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

            _winSequence.AppendInterval(winCompleteDelay);
            _winSequence.OnComplete(() => { EventManager<VisualEventID>.Post(VisualEventID.WinAnimationComplete); });
        }

        public void PlayLoseAnimation()
        {
            foreach (var kvp in _activeLines)
            {
                if (kvp.Value != null) kvp.Value.PlayLoseAnimation();
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
                case InGameState.WinAnimating: PlayWinAnimation(); break;
                case InGameState.Intro: PlayIntroLevelAnimation(); break;
                case InGameState.LoseAnimating: PlayLoseAnimation(); break;
            }
        }


        public ArrowLineView GetArrowViewAt(Vector2Int gridPos)
        {
            var arrowData = _logic.GetArrow(gridPos.x, gridPos.y);
            if (arrowData != null && !string.IsNullOrEmpty(arrowData.ID))
            {
                if (_activeLines.TryGetValue(arrowData.ID, out var view)) return view;
            }

            return null;
        }

        private Color GetAssignedColorForArrow(string arrowId, ThemeConfigSO theme)
        {
            if (!theme.isRandomArrowColor) return theme.arrowDefaultColor;

            int seed = Mathf.Abs(arrowId.GetHashCode());

            if (theme.arrowColorPalette == null || theme.arrowColorPalette.Count == 0)
            {
                return Color.HSVToRGB((seed % 100) / 100f, 0.85f, 0.95f);
            }

            Color baseColor = theme.arrowColorPalette[seed % theme.arrowColorPalette.Count];
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);

            h = Mathf.Repeat(h + (((seed % 11) - 5) / 100f), 1f);
            v = Mathf.Clamp01(v + (((seed % 7) - 3) / 100f));

            return Color.HSVToRGB(h, s, v);
        }

        public ArrowLineView GetArrowViewById(string arrowId)
        {
            if (string.IsNullOrEmpty(arrowId)) return null;
            if (_activeLines.TryGetValue(arrowId, out var view)) return view;
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
            foreach (var view in _activeLines.Values)
            {
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    view.ForceToggleDirectionLine(isOn, 0f); // Delay = 0 vì là trạng thái ban đầu
                }
            }
        }
    }
}
