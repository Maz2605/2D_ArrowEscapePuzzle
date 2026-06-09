using ShareCore.Data;

namespace ArrowGame.Gameplay.Logic
{
    public static class ArrowMechanicFactory
    {
        public static ArrowMechanicSet Create(ArrowTopologyType topologyType, string linkGroupId)
        {
            bool isTwoHeadArrow = topologyType == ArrowTopologyType.MultiEndpointSharedPath;
            bool isLinkedArrow = !string.IsNullOrEmpty(linkGroupId);
            return new ArrowMechanicSet(isTwoHeadArrow, isLinkedArrow);
        }
    }
}
