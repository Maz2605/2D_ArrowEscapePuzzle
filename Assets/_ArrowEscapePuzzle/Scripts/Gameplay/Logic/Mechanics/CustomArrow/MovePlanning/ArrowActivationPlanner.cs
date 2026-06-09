using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class ArrowActivationPlanner
    {
        private readonly BoardState _state;
        private readonly ArrowEscapeTracer _tracer;

        public ArrowActivationPlanner(BoardState state, ArrowEscapeTracer tracer)
        {
            _state = state;
            _tracer = tracer;
        }

        public ArrowActivationResult TryMoveArrowGroup(string arrowId, Vector2Int tappedCell)
        {
            ArrowModel triggerModel = _state.GetArrowModel(arrowId);
            if (triggerModel == null) return null;

            List<ArrowModel> activationModels = GetActivationModels(triggerModel, tappedCell);
            if (activationModels.Count == 0) return null;
            List<ArrowActivationEntry> entries = new List<ArrowActivationEntry>(activationModels.Count);
            string activationGroupKey = BuildActivationGroupKey(triggerModel, tappedCell);
            bool allSucceeded = true;

            for (int i = 0; i < activationModels.Count; i++)
            {
                ArrowModel arrowModel = activationModels[i];
                ArrowEndpoint endpoint = arrowModel.ArrowId == arrowId
                    ? ResolveEndpointFromTap(arrowModel.ArrowId, tappedCell)
                    : arrowModel.PrimaryEndpoint;
                if (endpoint == null)
                {
                    allSucceeded = false;
                    continue;
                }

                if (arrowModel.Mechanics.IsTwoHeadArrow &&
                    TwoHeadArrowMechanic.TryBuildSplitActivation(arrowModel, endpoint, out TwoHeadSplitActivation splitActivation))
                {
                    AppendSplitEntries(splitActivation, activationGroupKey, entries, ref allSucceeded);
                    continue;
                }

                AppendSingleEntry(arrowModel, endpoint, activationGroupKey, entries, ref allSucceeded);
            }

            return new ArrowActivationResult(arrowId, tappedCell, triggerModel.LinkGroupId, entries, allSucceeded);
        }

        public ArrowEndpoint ResolveEndpointFromTap(string arrowId, Vector2Int tappedCell)
        {
            ArrowModel arrowModel = _state.GetArrowModel(arrowId);
            return arrowModel?.Mechanics.SelectEndpoint(arrowModel, tappedCell);
        }

        public List<ArrowModel> GetActivationModels(ArrowModel triggerModel)
        {
            return GetActivationModels(triggerModel, Vector2Int.zero);
        }

        public List<ArrowModel> GetActivationModels(ArrowModel triggerModel, Vector2Int tappedCell)
        {
            if (triggerModel == null) return new List<ArrowModel>();

            IReadOnlyList<ArrowModel> models = triggerModel.Mechanics.ResolveActivationModels(_state, triggerModel);
            return models != null ? new List<ArrowModel>(models) : new List<ArrowModel>();
        }

        private List<ArrowData> CreateArrowGroupSnapshot(string arrowId)
        {
            List<ArrowData> snapshot = new List<ArrowData>();
            if (!_state.ArrowGroups.TryGetValue(arrowId, out List<ArrowData> group) || group == null)
            {
                return snapshot;
            }

            for (int i = 0; i < group.Count; i++)
            {
                ArrowData cell = group[i];
                snapshot.Add(new ArrowData(cell.ID, cell.X, cell.Y, cell.Type));
            }

            return snapshot;
        }

        private void AppendSingleEntry(ArrowModel arrowModel, ArrowEndpoint endpoint, string activationGroupKey,
            List<ArrowActivationEntry> entries, ref bool allSucceeded)
        {
            EscapeTraceResult traceResult = _tracer.TraceEscapeRoute(arrowModel.ArrowId, endpoint, true, activationGroupKey);
            if (!traceResult.CanEscape)
            {
                allSucceeded = false;
            }

            entries.Add(new ArrowActivationEntry(arrowModel.ArrowId, endpoint, traceResult,
                CreateArrowGroupSnapshot(arrowModel.ArrowId)));
        }

        private void AppendSplitEntries(TwoHeadSplitActivation splitActivation, string activationGroupKey,
            List<ArrowActivationEntry> entries, ref bool allSucceeded)
        {
            IReadOnlyList<TwoHeadSplitPart> parts = splitActivation.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                TwoHeadSplitPart part = parts[i];
                if (part == null || part.Model == null || part.Endpoint == null)
                {
                    allSucceeded = false;
                    continue;
                }

                EscapeTraceResult traceResult = _tracer.TraceEscapeRoute(part.Model, part.Endpoint, false,
                    activationGroupKey);
                if (!traceResult.CanEscape)
                {
                    allSucceeded = false;
                }

                entries.Add(new ArrowActivationEntry(part.Model.ArrowId, part.Endpoint, traceResult, part.Snapshot,
                    part.EntryKey, part.Model, true, part.IsSelectedPart));
            }
        }

        private static string BuildActivationGroupKey(ArrowModel triggerModel, Vector2Int tappedCell)
        {
            string groupId = string.IsNullOrEmpty(triggerModel?.LinkGroupId)
                ? triggerModel?.ArrowId ?? string.Empty
                : triggerModel.LinkGroupId;
            return $"{groupId}@{tappedCell.x}:{tappedCell.y}";
        }
    }
}
