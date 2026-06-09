using System;
using System.Collections.Generic;
using ArrowGame.Data.Theme;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
using DG.Tweening;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual.GridComponents
{
    public class ArrowLineVisuals : MonoBehaviour
    {
        private const float DirectionLineToggleDelayFactor = 0.05f;
        private const float DirectionLineToggleDuration = 0.4f;
        [Header("Arrow Linked Group")]
        [SerializeField] private float linkedEscapeFollowDelayStep = 0.08f;

        [Header("--- REFERENCES ---")]
        [SerializeField] private ArrowLineView arrowLinePrefab;

        private readonly Dictionary<string, ArrowLineView> _activeLines = new Dictionary<string, ArrowLineView>();

        private GridSystem _logic;
        private Transform _arrowRoot;
        private float _cellSize;
        private float _maxAllowedIntroDuration;

        public IReadOnlyDictionary<string, ArrowLineView> ActiveLines => _activeLines;

        public void Initialize(GridSystem logic, float cellSize, float maxAllowedIntroDuration, Transform arrowRoot)
        {
            _logic = logic;
            _cellSize = cellSize;
            _maxAllowedIntroDuration = maxAllowedIntroDuration;
            _arrowRoot = arrowRoot;

            RebuildArrowViews();
        }

        public void RebuildArrowViews()
        {
            DespawnAllArrowViews();
            _activeLines.Clear();

            if (_logic == null || _arrowRoot == null || arrowLinePrefab == null) return;

            ThemeConfigSO currentTheme = ThemeManager.Instance.CurrentTheme;
            foreach (KeyValuePair<string, List<ArrowData>> kvp in _logic.ArrowGroups)
            {
                ArrowLineView lineView = PoolingManager.Instance.Spawn(arrowLinePrefab, Vector3.zero, Quaternion.identity, _arrowRoot);
                lineView.transform.localPosition = Vector3.zero;

                ArrowModel arrowModel = _logic.GetArrowModel(kvp.Key);
                lineView.Setup(arrowModel, kvp.Value, _cellSize, currentTheme);
                _activeLines[kvp.Key] = lineView;
            }
        }

        public void ApplyTheme(ThemeConfigSO newTheme)
        {
            foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
            {
                if (kvp.Value != null) kvp.Value.UpdateThemeColor(newTheme);
            }
        }

        public List<ArrowVisualRemovalContext> HandleArrowEscaped(ArrowActivationResult activationResult,
            Action<IReadOnlyList<ArrowData>> onEscapeStart, Action<ArrowVisualRemovalContext> onEscapeComplete = null)
        {
            List<ArrowVisualRemovalContext> removed = new List<ArrowVisualRemovalContext>();
            if (activationResult == null || activationResult.Entries == null) return removed;

            List<ArrowActivationEntry> orderedEntries = BuildOrderedEntries(activationResult);
            HashSet<string> handledSplitArrows = new HashSet<string>();
            int originalArrowOrder = 0;

            for (int i = 0; i < orderedEntries.Count; i++)
            {
                ArrowActivationEntry entry = orderedEntries[i];
                if (entry == null) continue;

                float startDelay = activationResult.IsLinkedGroup ? originalArrowOrder * linkedEscapeFollowDelayStep : 0f;
                if (entry.IsSplitPart)
                {
                    if (!handledSplitArrows.Add(entry.ArrowId)) continue;

                    List<ArrowActivationEntry> splitEntries = CollectSplitEntries(orderedEntries, entry.ArrowId);
                    if (HandleSplitArrowEscaped(entry.ArrowId, splitEntries, startDelay, onEscapeStart,
                            onEscapeComplete, removed))
                    {
                        originalArrowOrder++;
                    }

                    continue;
                }

                if (!_activeLines.TryGetValue(entry.ArrowId, out ArrowLineView lineView)) continue;

                if (entry.Endpoint != null)
                {
                    lineView.SetActiveEndpoint(entry.Endpoint.PathIndex);
                }

                ApplyTraceToView(entry.ArrowId, lineView, entry.TraceResult);
                IReadOnlyList<ArrowData> groupSnapshot = entry.GroupSnapshot;
                ArrowVisualRemovalContext context = new ArrowVisualRemovalContext(entry.ArrowId, lineView, groupSnapshot);
                lineView.PlayEscapeAnimation(startDelay,
                    onEscapeStart: () => onEscapeStart?.Invoke(groupSnapshot),
                    onEscapeComplete: () => onEscapeComplete?.Invoke(context));

                _activeLines.Remove(entry.ArrowId);
                removed.Add(context);
                originalArrowOrder++;
            }

            return removed;
        }

        public ArrowVisualRemovalContext? HandleArrowForceRemove(List<ArrowData> removedGroup)
        {
            if (removedGroup == null || removedGroup.Count == 0) return null;

            string targetId = removedGroup[0].ID;
            if (!_activeLines.TryGetValue(targetId, out ArrowLineView lineView)) return null;

            _activeLines.Remove(targetId);
            if (lineView != null && lineView.gameObject.activeInHierarchy)
            {
                PoolingManager.Instance.Despawn(lineView.gameObject);
            }

            return new ArrowVisualRemovalContext(targetId, lineView, removedGroup);
        }

        public ArrowVisualRemovalContext? HandlePlayDashEscape(string id, Action<IReadOnlyList<ArrowData>> onEscapeStart)
        {
            if (string.IsNullOrEmpty(id) || _logic == null || !_activeLines.TryGetValue(id, out ArrowLineView view)) return null;

            List<ArrowData> dashGroup = null;
            if (_logic.ArrowGroups.TryGetValue(id, out List<ArrowData> group))
            {
                dashGroup = group;
            }

            ApplyTraceToView(id, view, _logic.GetLiveTraceResult(id));
            view.PlayFocusHighlight(false);
            view.PlayEscapeAnimation(onEscapeStart: () => onEscapeStart?.Invoke(dashGroup));

            _activeLines.Remove(id);
            return new ArrowVisualRemovalContext(id, view, dashGroup);
        }

        public void ShowHintVisual(string arrowId)
        {
            if (string.IsNullOrEmpty(arrowId) || _logic == null || !_activeLines.TryGetValue(arrowId, out ArrowLineView view)) return;
            if (view == null || !view.gameObject.activeInHierarchy) return;

            ApplyTraceToView(arrowId, view, _logic.GetLiveTraceResult(arrowId));
            view.PlayHintEffect();
            view.ForceToggleDirectionLine(true, 0f, skipPunchScale: true);
        }

        public void ToggleDirectionLines(bool isOn)
        {
            if (_activeLines.Count == 0) return;

            if (!isOn)
            {
                foreach (ArrowLineView view in _activeLines.Values)
                {
                    if (view == null || !view.gameObject.activeInHierarchy) continue;
                    view.ClearTraceRoute();
                    view.ForceToggleDirectionLine(false, 0f);
                }

                return;
            }

            int activeLineCount = 0;
            foreach (ArrowLineView view in _activeLines.Values)
            {
                if (view != null && view.gameObject.activeInHierarchy) activeLineCount++;
            }

            int batchSize = GridAnimator.CalculateStaggerBatchSize(activeLineCount, DirectionLineToggleDuration,
                DirectionLineToggleDelayFactor, _maxAllowedIntroDuration);
            float currentDelay = 0f;
            int currentBatchCount = 0;

            foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
            {
                ArrowLineView view = kvp.Value;
                if (view == null || !view.gameObject.activeInHierarchy) continue;

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

        public void HandleCameraMoved()
        {
            foreach (ArrowLineView view in _activeLines.Values)
            {
                if (view != null && view.gameObject.activeInHierarchy)
                {
                    view.UpdateDirectionLineIfEnabled();
                }
            }
        }

        public void HandleBoosterTargetModeChanged(bool isSelecting)
        {
            foreach (KeyValuePair<string, ArrowLineView> kvp in _activeLines)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.ToggleTargetSelectionState(isSelecting);
                }
            }
        }

        public void ShowFocusHighlight(string id)
        {
            if (TryGetActiveLine(id, out ArrowLineView view) && view.gameObject.activeInHierarchy)
            {
                view.PlayFocusHighlight(true);
            }
        }

        public void HideFocusHighlight(string id)
        {
            if (TryGetActiveLine(id, out ArrowLineView view) && view.gameObject.activeInHierarchy)
            {
                view.PlayFocusHighlight(false);
            }
        }

        public ArrowLineView GetArrowViewAt(Vector2Int gridPos)
        {
            if (_logic == null) return null;

            ArrowData arrowData = _logic.GetArrow(gridPos.x, gridPos.y);
            if (arrowData == null || string.IsNullOrEmpty(arrowData.ID) || !_activeLines.TryGetValue(arrowData.ID, out ArrowLineView view))
            {
                return null;
            }

            ArrowEndpoint endpoint = _logic.ResolveEndpointFromTap(arrowData.ID, gridPos);
            if (endpoint != null)
            {
                view.SetActiveEndpoint(endpoint.PathIndex);
            }

            ApplyTraceToView(arrowData.ID, view, endpoint != null
                ? _logic.GetLiveTraceResult(arrowData.ID, endpoint)
                : _logic.GetLiveTraceResult(arrowData.ID));
            return view;
        }

        public ArrowLineView GetArrowViewById(string arrowId)
        {
            if (!TryGetActiveLine(arrowId, out ArrowLineView view) || _logic == null) return null;

            ApplyTraceToView(arrowId, view, _logic.GetLiveTraceResult(arrowId));
            return view;
        }

        public void CullArrowView(string arrowId)
        {
            if (!string.IsNullOrEmpty(arrowId))
            {
                _activeLines.Remove(arrowId);
            }
        }

        public bool TryGetActiveLine(string arrowId, out ArrowLineView view)
        {
            view = null;
            return !string.IsNullOrEmpty(arrowId) && _activeLines.TryGetValue(arrowId, out view) && view != null;
        }

        public void ApplyTraceToView(string arrowId, ArrowLineView view, EscapeTraceResult trace)
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

        private static List<ArrowActivationEntry> BuildOrderedEntries(ArrowActivationResult activationResult)
        {
            List<ArrowActivationEntry> orderedEntries = new List<ArrowActivationEntry>(activationResult.Entries);
            if (!activationResult.IsLinkedGroup) return orderedEntries;

            orderedEntries.Sort((left, right) =>
            {
                bool leftIsLead = left != null && left.ArrowId == activationResult.TriggerArrowId;
                bool rightIsLead = right != null && right.ArrowId == activationResult.TriggerArrowId;
                if (leftIsLead && !rightIsLead) return -1;
                if (!leftIsLead && rightIsLead) return 1;

                int arrowCompare = string.CompareOrdinal(left?.ArrowId, right?.ArrowId);
                if (arrowCompare != 0) return arrowCompare;
                return string.CompareOrdinal(left?.EntryKey, right?.EntryKey);
            });

            return orderedEntries;
        }

        private static List<ArrowActivationEntry> CollectSplitEntries(List<ArrowActivationEntry> entries, string arrowId)
        {
            List<ArrowActivationEntry> splitEntries = new List<ArrowActivationEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                ArrowActivationEntry entry = entries[i];
                if (entry != null && entry.IsSplitPart && entry.ArrowId == arrowId)
                {
                    splitEntries.Add(entry);
                }
            }

            return splitEntries;
        }

        private bool HandleSplitArrowEscaped(string arrowId, List<ArrowActivationEntry> splitEntries, float startDelay,
            Action<IReadOnlyList<ArrowData>> onEscapeStart, Action<ArrowVisualRemovalContext> onEscapeComplete,
            List<ArrowVisualRemovalContext> removed)
        {
            if (splitEntries == null || splitEntries.Count == 0) return false;
            if (arrowLinePrefab == null || _arrowRoot == null) return false;
            if (!_activeLines.TryGetValue(arrowId, out ArrowLineView originalLine)) return false;

            _activeLines.Remove(arrowId);
            ArrowVisualRemovalContext originalContext =
                new ArrowVisualRemovalContext(arrowId, originalLine, splitEntries[0].GroupSnapshot);
            onEscapeComplete?.Invoke(originalContext);

            ThemeConfigSO currentTheme = ThemeManager.Instance.CurrentTheme;
            for (int i = 0; i < splitEntries.Count; i++)
            {
                ArrowActivationEntry entry = splitEntries[i];
                if (entry?.VisualModel == null || entry.TraceResult == null) continue;

                ArrowLineView splitView = PoolingManager.Instance.Spawn(arrowLinePrefab, Vector3.zero,
                    Quaternion.identity, _arrowRoot);
                splitView.transform.localPosition = Vector3.zero;
                splitView.Setup(entry.VisualModel, null, _cellSize, currentTheme);
                if (entry.Endpoint != null)
                {
                    splitView.SetActiveEndpoint(entry.Endpoint.PathIndex);
                }

                splitView.SetTraceRoute(entry.TraceResult);
                IReadOnlyList<ArrowData> groupSnapshot = entry.GroupSnapshot;
                ArrowVisualRemovalContext splitContext =
                    new ArrowVisualRemovalContext(entry.ArrowId, splitView, groupSnapshot);
                splitView.PlayEscapeAnimation(startDelay,
                    onEscapeStart: () => onEscapeStart?.Invoke(groupSnapshot));
                removed.Add(splitContext);
            }

            if (originalLine != null && originalLine.gameObject.activeInHierarchy)
            {
                PoolingManager.Instance.Despawn(originalLine.gameObject);
            }

            return true;
        }

        private void DespawnAllArrowViews()
        {
            if (_arrowRoot == null) return;

            for (int i = _arrowRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _arrowRoot.GetChild(i);
                child.DOKill();
                if (child.gameObject.activeInHierarchy)
                {
                    PoolingManager.Instance.Despawn(child.gameObject);
                }
            }
        }
    }
}
