using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class ArrowMechanicSet
    {
        public bool IsTwoHeadArrow { get; }
        public bool IsLinkedArrow { get; }

        public ArrowMechanicSet(bool isTwoHeadArrow, bool isLinkedArrow)
        {
            IsTwoHeadArrow = isTwoHeadArrow;
            IsLinkedArrow = isLinkedArrow;
        }

        public ArrowEndpoint SelectEndpoint(ArrowModel arrowModel, Vector2Int tappedCell)
        {
            if (arrowModel == null) return null;
            return IsTwoHeadArrow
                ? TwoHeadArrowMechanic.SelectNearestEndpoint(arrowModel, tappedCell)
                : arrowModel.PrimaryEndpoint;
        }

        public IReadOnlyList<ArrowModel> ResolveActivationModels(BoardState board, ArrowModel triggerModel)
        {
            if (triggerModel == null) return new List<ArrowModel>();
            return IsLinkedArrow
                ? LinkedArrowMechanic.ResolveGroupModels(board, triggerModel)
                : new List<ArrowModel> { triggerModel };
        }

        public bool CanPassThroughLinkedArrow(ArrowModel movingModel, ArrowModel blockerModel)
        {
            return IsLinkedArrow && LinkedArrowMechanic.CanPassThrough(movingModel, blockerModel);
        }
    }
}
