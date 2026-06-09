using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class ArrowActivationEntry
    {
        public string ArrowId { get; }
        public ArrowEndpoint Endpoint { get; }
        public EscapeTraceResult TraceResult { get; }
        public IReadOnlyList<ArrowData> GroupSnapshot { get; }

        public ArrowActivationEntry(string arrowId, ArrowEndpoint endpoint, EscapeTraceResult traceResult,
            IReadOnlyList<ArrowData> groupSnapshot)
        {
            ArrowId = arrowId;
            Endpoint = endpoint;
            TraceResult = traceResult;
            GroupSnapshot = groupSnapshot ?? new List<ArrowData>();
        }

        public ArrowData GetHeadSnapshot()
        {
            if (Endpoint == null || GroupSnapshot == null) return null;

            for (int i = 0; i < GroupSnapshot.Count; i++)
            {
                ArrowData cell = GroupSnapshot[i];
                if (cell.X == Endpoint.Position.x && cell.Y == Endpoint.Position.y)
                {
                    return cell;
                }
            }

            return GroupSnapshot.Count > 0 ? GroupSnapshot[GroupSnapshot.Count - 1] : null;
        }
    }

    public sealed class ArrowActivationResult
    {
        private readonly List<ArrowActivationEntry> _entries;

        public string TriggerArrowId { get; }
        public Vector2Int TapCell { get; }
        public string LinkGroupId { get; }
        public bool AllSucceeded { get; }
        public IReadOnlyList<ArrowActivationEntry> Entries => _entries;
        public bool IsLinkedGroup => _entries.Count > 1 && !string.IsNullOrEmpty(LinkGroupId);

        public ArrowActivationResult(string triggerArrowId, Vector2Int tapCell, string linkGroupId,
            IEnumerable<ArrowActivationEntry> entries, bool allSucceeded)
        {
            TriggerArrowId = triggerArrowId ?? string.Empty;
            TapCell = tapCell;
            LinkGroupId = linkGroupId ?? string.Empty;
            _entries = entries != null ? new List<ArrowActivationEntry>(entries) : new List<ArrowActivationEntry>();
            AllSucceeded = allSucceeded;
        }

        public ArrowActivationEntry GetEntry(string arrowId)
        {
            return _entries.FirstOrDefault(entry => entry.ArrowId == arrowId);
        }

        public ArrowActivationEntry GetFirstBlockedEntry()
        {
            return _entries.FirstOrDefault(entry => entry.TraceResult != null && !entry.TraceResult.CanEscape);
        }
    }
}
