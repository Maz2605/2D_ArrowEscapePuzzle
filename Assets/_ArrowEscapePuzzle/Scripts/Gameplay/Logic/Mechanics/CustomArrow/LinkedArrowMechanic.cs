using System;
using System.Collections.Generic;

namespace ArrowGame.Gameplay.Logic
{
    public static class LinkedArrowMechanic
    {
        public static IReadOnlyList<ArrowModel> ResolveGroupModels(BoardState board, ArrowModel triggerModel)
        {
            List<ArrowModel> activationModels = new List<ArrowModel>();
            if (board == null || triggerModel == null) return activationModels;

            if (string.IsNullOrEmpty(triggerModel.LinkGroupId))
            {
                activationModels.Add(triggerModel);
                return activationModels;
            }

            foreach (ArrowModel arrowModel in board.ArrowModels.Values)
            {
                if (arrowModel == null || arrowModel.LinkGroupId != triggerModel.LinkGroupId) continue;
                activationModels.Add(arrowModel);
            }

            activationModels.Sort((a, b) =>
            {
                if (a.ArrowId == triggerModel.ArrowId) return -1;
                if (b.ArrowId == triggerModel.ArrowId) return 1;
                return string.CompareOrdinal(a.ArrowId, b.ArrowId);
            });

            return activationModels;
        }

        public static bool CanPassThrough(ArrowModel movingModel, ArrowModel blockerModel)
        {
            return movingModel != null &&
                   blockerModel != null &&
                   !string.IsNullOrEmpty(movingModel.LinkGroupId) &&
                   movingModel.LinkGroupId == blockerModel.LinkGroupId;
        }
    }
}
