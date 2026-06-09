using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
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
    }
}
