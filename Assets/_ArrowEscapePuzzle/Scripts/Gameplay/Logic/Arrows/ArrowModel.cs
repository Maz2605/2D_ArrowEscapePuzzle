using System.Collections.Generic;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class ArrowModel
    {
        private readonly List<Vector2Int> _path;
        private readonly List<ArrowEndpoint> _endpoints;

        public string ArrowId { get; }
        public ArrowTopologyType TopologyType { get; }
        public string LinkGroupId { get; }
        public ArrowMechanicSet Mechanics { get; }
        public IReadOnlyList<Vector2Int> Path => _path;
        public IReadOnlyList<ArrowEndpoint> Endpoints => _endpoints;

        public ArrowEndpoint PrimaryEndpoint
        {
            get
            {
                for (int i = 0; i < _endpoints.Count; i++)
                {
                    if (_endpoints[i].IsPrimary) return _endpoints[i];
                }

                return _endpoints.Count > 0 ? _endpoints[0] : null;
            }
        }

        public ArrowModel(string arrowId, List<Vector2Int> path, List<ArrowEndpoint> endpoints,
            ArrowTopologyType topologyType, string linkGroupId, ArrowMechanicSet mechanics = null)
        {
            ArrowId = arrowId;
            TopologyType = topologyType;
            LinkGroupId = linkGroupId ?? string.Empty;
            Mechanics = mechanics ?? ArrowMechanicFactory.Create(topologyType, LinkGroupId);
            _path = path != null ? new List<Vector2Int>(path) : new List<Vector2Int>();
            _endpoints = endpoints != null ? new List<ArrowEndpoint>(endpoints) : new List<ArrowEndpoint>();
        }

        public ArrowEndpoint GetEndpointByKey(string endpointKey)
        {
            if (string.IsNullOrEmpty(endpointKey)) return null;

            for (int i = 0; i < _endpoints.Count; i++)
            {
                ArrowEndpoint endpoint = _endpoints[i];
                if (endpoint.EndpointKey == endpointKey) return endpoint;
            }

            return null;
        }

        public ArrowEndpoint GetEndpointAtPathIndex(int pathIndex)
        {
            for (int i = 0; i < _endpoints.Count; i++)
            {
                ArrowEndpoint endpoint = _endpoints[i];
                if (endpoint.PathIndex == pathIndex) return endpoint;
            }

            return null;
        }
    }
}
