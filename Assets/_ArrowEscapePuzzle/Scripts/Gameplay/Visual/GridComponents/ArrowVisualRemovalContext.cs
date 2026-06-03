using System.Collections.Generic;
using ArrowGame.Gameplay.Logic;

namespace ArrowGame.Gameplay.Visual.GridComponents
{
    public readonly struct ArrowVisualRemovalContext
    {
        public ArrowVisualRemovalContext(string arrowId, ArrowLineView view, IReadOnlyList<ArrowData> groupSnapshot)
        {
            ArrowId = arrowId;
            View = view;
            GroupSnapshot = groupSnapshot;
        }

        public string ArrowId { get; }
        public ArrowLineView View { get; }
        public IReadOnlyList<ArrowData> GroupSnapshot { get; }
    }
}
