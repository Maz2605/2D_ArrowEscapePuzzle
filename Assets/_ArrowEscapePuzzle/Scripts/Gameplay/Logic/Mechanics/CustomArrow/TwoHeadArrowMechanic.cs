using System.Collections.Generic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class TwoHeadSplitPart
    {
        public string EntryKey { get; }
        public ArrowModel Model { get; }
        public ArrowEndpoint Endpoint { get; }
        public IReadOnlyList<ArrowData> Snapshot { get; }
        public bool IsSelectedPart { get; }

        public TwoHeadSplitPart(string entryKey, ArrowModel model, ArrowEndpoint endpoint,
            IReadOnlyList<ArrowData> snapshot, bool isSelectedPart)
        {
            EntryKey = entryKey ?? string.Empty;
            Model = model;
            Endpoint = endpoint;
            Snapshot = snapshot ?? new List<ArrowData>();
            IsSelectedPart = isSelectedPart;
        }
    }

    public sealed class TwoHeadSplitActivation
    {
        private readonly List<TwoHeadSplitPart> _parts;

        public IReadOnlyList<TwoHeadSplitPart> Parts => _parts;

        public TwoHeadSplitActivation(IEnumerable<TwoHeadSplitPart> parts)
        {
            _parts = parts != null ? new List<TwoHeadSplitPart>(parts) : new List<TwoHeadSplitPart>();
        }
    }

    public static class TwoHeadArrowMechanic
    {
        public static ArrowEndpoint SelectNearestEndpoint(ArrowModel arrowModel, Vector2Int tappedCell)
        {
            IReadOnlyList<ArrowEndpoint> endpoints = arrowModel?.Endpoints;
            if (endpoints == null || endpoints.Count == 0) return null;
            if (endpoints.Count == 1) return endpoints[0];

            ArrowEndpoint bestEndpoint = null;
            float bestDistance = float.MaxValue;
            bool bestIsPrimary = false;

            for (int i = 0; i < endpoints.Count; i++)
            {
                ArrowEndpoint endpoint = endpoints[i];
                float distance = Vector2Int.Distance(endpoint.Position, tappedCell);
                bool isPrimary = endpoint.IsPrimary;
                if (distance < bestDistance - 0.001f ||
                    (Mathf.Abs(distance - bestDistance) <= 0.001f && isPrimary && !bestIsPrimary))
                {
                    bestEndpoint = endpoint;
                    bestDistance = distance;
                    bestIsPrimary = isPrimary;
                }
            }

            return bestEndpoint ?? endpoints[0];
        }

        public static bool TryBuildSplitActivation(ArrowModel arrowModel, ArrowEndpoint selectedEndpoint,
            out TwoHeadSplitActivation activation)
        {
            activation = null;
            if (arrowModel == null || arrowModel.Path == null || arrowModel.Endpoints == null ||
                arrowModel.Path.Count == 0 || arrowModel.Endpoints.Count != 2)
            {
                return false;
            }

            selectedEndpoint ??= arrowModel.PrimaryEndpoint;
            if (selectedEndpoint == null) return false;

            int pathCount = arrowModel.Path.Count;
            ArrowEndpoint firstEndpoint = arrowModel.GetEndpointAtPathIndex(0);
            ArrowEndpoint secondEndpoint = arrowModel.GetEndpointAtPathIndex(pathCount - 1);
            if (firstEndpoint == null || secondEndpoint == null) return false;
            int selectedIndex = selectedEndpoint.PathIndex;
            int firstHalfEndExclusive;
            if (pathCount % 2 == 0)
            {
                firstHalfEndExclusive = pathCount / 2;
            }
            else
            {
                int middleIndex = pathCount / 2;
                firstHalfEndExclusive = selectedIndex == 0 ? middleIndex + 1 : middleIndex;
            }

            firstHalfEndExclusive = Mathf.Clamp(firstHalfEndExclusive, 1, pathCount - 1);
            List<TwoHeadSplitPart> parts = new List<TwoHeadSplitPart>(2)
            {
                BuildPart(arrowModel, firstEndpoint, 0, firstHalfEndExclusive,
                    selectedEndpoint.PathIndex == firstEndpoint.PathIndex),
                BuildPart(arrowModel, secondEndpoint, firstHalfEndExclusive, pathCount,
                    selectedEndpoint.PathIndex == secondEndpoint.PathIndex)
            };
            parts.Sort((left, right) => right.IsSelectedPart.CompareTo(left.IsSelectedPart));

            activation = new TwoHeadSplitActivation(parts);
            return true;
        }

        private static TwoHeadSplitPart BuildPart(ArrowModel originalModel, ArrowEndpoint originalEndpoint,
            int startInclusive, int endExclusive, bool isSelectedPart)
        {
            List<Vector2Int> splitPath = new List<Vector2Int>(endExclusive - startInclusive);
            for (int i = startInclusive; i < endExclusive; i++)
            {
                splitPath.Add(originalModel.Path[i]);
            }

            int localEndpointIndex = originalEndpoint.PathIndex == 0 ? 0 : splitPath.Count - 1;
            ArrowEndpoint splitEndpoint = new ArrowEndpoint(localEndpointIndex, originalEndpoint.Position,
                originalEndpoint.ExitDirection, true);
            ArrowMechanicSet mechanics = ArrowMechanicFactory.Create(ArrowTopologyType.SingleHeadSingleTail,
                originalModel.LinkGroupId);
            ArrowModel splitModel = new ArrowModel(originalModel.ArrowId, splitPath,
                new List<ArrowEndpoint> { splitEndpoint }, ArrowTopologyType.SingleHeadSingleTail,
                originalModel.LinkGroupId, mechanics);
            List<ArrowData> snapshot = BuildSnapshot(splitModel);
            string entryKey = $"{originalModel.ArrowId}|split:{originalEndpoint.PathIndex}";
            return new TwoHeadSplitPart(entryKey, splitModel, splitEndpoint, snapshot, isSelectedPart);
        }

        private static List<ArrowData> BuildSnapshot(ArrowModel splitModel)
        {
            List<ArrowData> snapshot = new List<ArrowData>();
            if (splitModel == null || splitModel.Path == null) return snapshot;

            List<CellType> cellTypes = ArrowCellTypeBuilder.BuildLegacyCellTypes(splitModel);
            for (int i = 0; i < splitModel.Path.Count; i++)
            {
                Vector2Int position = splitModel.Path[i];
                CellType cellType = i < cellTypes.Count ? cellTypes[i] : CellType.ArrowBodyHorizontal;
                snapshot.Add(new ArrowData(splitModel.ArrowId, position.x, position.y, cellType));
            }

            return snapshot;
        }
    }
}
