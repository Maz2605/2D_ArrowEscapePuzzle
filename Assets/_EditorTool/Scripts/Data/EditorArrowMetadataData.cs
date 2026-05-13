using System;
using ShareCore.Data;

namespace EditorTool.Scripts.Data
{
    [Serializable]
    public class EditorArrowMetadataData
    {
        public string ArrowId;
        public int PrimaryEndpointPathIndex;
        public bool HasSecondaryEndpoint;
        public int SecondaryEndpointPathIndex;
        public string LinkGroupId;
        public ArrowTopologyType TopologyType;

        public EditorArrowMetadataData()
        {
            LinkGroupId = string.Empty;
            TopologyType = ArrowTopologyType.SingleHeadSingleTail;
        }

        public EditorArrowMetadataData Clone()
        {
            return new EditorArrowMetadataData
            {
                ArrowId = ArrowId,
                PrimaryEndpointPathIndex = PrimaryEndpointPathIndex,
                HasSecondaryEndpoint = HasSecondaryEndpoint,
                SecondaryEndpointPathIndex = SecondaryEndpointPathIndex,
                LinkGroupId = LinkGroupId ?? string.Empty,
                TopologyType = TopologyType
            };
        }
    }
}
