using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Scripts.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public static class ArrowModelFactory
    {
        public static bool TryCreate(ArrowSaveData saveData, out ArrowModel model, out string error)
        {
            model = null;

            if (saveData == null)
            {
                error = "Arrow data is null.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(saveData.ArrowID))
            {
                error = "Arrow id is missing.";
                return false;
            }

            List<Vector2Int> path = saveData.Path != null ? new List<Vector2Int>(saveData.Path) : new List<Vector2Int>();
            if (!TryValidatePath(path, out error))
            {
                error = $"Arrow {saveData.ArrowID}: {error}";
                return false;
            }

            List<ArrowEndpointSaveData> endpointSaves = BuildNormalizedEndpointSaves(saveData, path);
            if (!TryValidateEndpoints(saveData.ArrowID, path, endpointSaves, out error))
            {
                return false;
            }

            List<ArrowEndpoint> endpoints = BuildRuntimeEndpoints(path, endpointSaves);
            ArrowTopologyType topologyType = saveData.TopologyType;
            if (endpoints.Count > 1 && topologyType == ArrowTopologyType.SingleHeadSingleTail)
            {
                topologyType = ArrowTopologyType.MultiEndpointSharedPath;
            }

            model = new ArrowModel(saveData.ArrowID, path, endpoints, topologyType, saveData.LinkGroupId);
            error = null;
            return true;
        }

        private static bool TryValidatePath(List<Vector2Int> path, out string error)
        {
            if (path == null || path.Count == 0)
            {
                error = "path is empty.";
                return false;
            }

            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            for (int i = 0; i < path.Count; i++)
            {
                if (!visited.Add(path[i]))
                {
                    error = $"path contains duplicate position {path[i]}.";
                    return false;
                }

                if (i == 0) continue;

                Vector2Int delta = path[i] - path[i - 1];
                if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
                {
                    error = $"path is not contiguous between {path[i - 1]} and {path[i]}.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static List<ArrowEndpointSaveData> BuildNormalizedEndpointSaves(ArrowSaveData saveData,
            IReadOnlyList<Vector2Int> path)
        {
            if (saveData.Endpoints != null && saveData.Endpoints.Count > 0)
            {
                List<ArrowEndpointSaveData> explicitEndpoints = new List<ArrowEndpointSaveData>(saveData.Endpoints.Count);
                bool hasPrimary = false;

                for (int i = 0; i < saveData.Endpoints.Count; i++)
                {
                    ArrowEndpointSaveData endpoint = saveData.Endpoints[i];
                    if (endpoint == null) continue;

                    bool isPrimary = endpoint.IsPrimary && !hasPrimary;
                    hasPrimary |= endpoint.IsPrimary;
                    explicitEndpoints.Add(new ArrowEndpointSaveData(endpoint.PathIndex, endpoint.ExitDirection, isPrimary));
                }

                if (explicitEndpoints.Count > 0 && !hasPrimary)
                {
                    explicitEndpoints[0].IsPrimary = true;
                }

                return explicitEndpoints;
            }

            int primaryHeadIndex = ResolveLegacyPrimaryHeadIndex(saveData.IsHeadFirst, path.Count);
            Direction4 exitDirection = BuildEndpointDirectionFromPath(path, primaryHeadIndex);
            return new List<ArrowEndpointSaveData>
            {
                new ArrowEndpointSaveData(primaryHeadIndex, exitDirection, true)
            };
        }

        private static bool TryValidateEndpoints(string arrowId, IReadOnlyList<Vector2Int> path,
            List<ArrowEndpointSaveData> endpoints, out string error)
        {
            if (endpoints == null || endpoints.Count == 0)
            {
                error = $"Arrow {arrowId}: endpoint list is empty.";
                return false;
            }

            HashSet<int> usedIndices = new HashSet<int>();
            for (int i = 0; i < endpoints.Count; i++)
            {
                ArrowEndpointSaveData endpoint = endpoints[i];
                if (endpoint == null)
                {
                    error = $"Arrow {arrowId}: endpoint {i} is null.";
                    return false;
                }

                if (endpoint.PathIndex < 0 || endpoint.PathIndex >= path.Count)
                {
                    error = $"Arrow {arrowId}: endpoint pathIndex {endpoint.PathIndex} is out of range.";
                    return false;
                }

                if (endpoint.PathIndex != 0 && endpoint.PathIndex != path.Count - 1)
                {
                    error = $"Arrow {arrowId}: endpoint pathIndex {endpoint.PathIndex} is not at a path end.";
                    return false;
                }

                if (!usedIndices.Add(endpoint.PathIndex))
                {
                    error = $"Arrow {arrowId}: duplicate endpoint at pathIndex {endpoint.PathIndex}.";
                    return false;
                }

                Direction4 expectedDirection = BuildEndpointDirectionFromPath(path, endpoint.PathIndex);
                if (endpoint.ExitDirection != expectedDirection)
                {
                    error =
                        $"Arrow {arrowId}: endpoint direction at pathIndex {endpoint.PathIndex} does not match path geometry.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static List<ArrowEndpoint> BuildRuntimeEndpoints(IReadOnlyList<Vector2Int> path,
            List<ArrowEndpointSaveData> endpoints)
        {
            List<ArrowEndpoint> runtimeEndpoints = new List<ArrowEndpoint>(endpoints.Count);
            for (int i = 0; i < endpoints.Count; i++)
            {
                ArrowEndpointSaveData endpoint = endpoints[i];
                runtimeEndpoints.Add(new ArrowEndpoint(endpoint.PathIndex, path[endpoint.PathIndex],
                    endpoint.ExitDirection, endpoint.IsPrimary));
            }

            return runtimeEndpoints;
        }

        private static int ResolveLegacyPrimaryHeadIndex(bool isHeadFirst, int pathCount)
        {
            if (pathCount <= 0) return 0;
            return isHeadFirst ? 0 : pathCount - 1;
        }

        private static Direction4 BuildEndpointDirectionFromPath(IReadOnlyList<Vector2Int> path, int endpointIndex)
        {
            if (path == null || path.Count <= 1) return Direction4.Up;

            Vector2Int current = path[endpointIndex];
            int neighborIndex = endpointIndex == 0 ? 1 : path.Count - 2;
            Vector2Int direction = current - path[neighborIndex];
            return Direction4Extensions.FromVector(direction);
        }
    }
}
