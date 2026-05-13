using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Scripts.Data;

namespace EditorTool.Scripts.EditorTool.Logic
{
    public class MapValidator
    {
        public static (bool isValid, string errorMsg) ValidateBaseMap(GridSystem grid)
        {
            List<string> allIDs = grid.GetAllArrowIDs();
            List<string> realArrowIDs = new List<string>();

            if (allIDs != null)
            {
                foreach (string id in allIDs)
                {
                    List<UnityEngine.Vector2Int> path = grid.GetArrowPath(id);
                    if (path != null && path.Count > 0)
                    {
                        realArrowIDs.Add(id);
                    }
                }
            }

            if (realArrowIDs.Count == 0)
            {
                return (false, "Bản đồ đang trống! Bạn phải vẽ hoàn thiện ít nhất 1 mũi tên.");
            }

            List<ArrowSaveData> arrowSaveData = grid.GetSaveData();
            Dictionary<string, ArrowSaveData> arrowsById = new Dictionary<string, ArrowSaveData>();
            for (int i = 0; i < arrowSaveData.Count; i++)
            {
                ArrowSaveData arrow = arrowSaveData[i];
                if (arrow != null && !string.IsNullOrWhiteSpace(arrow.ArrowID))
                {
                    arrowsById[arrow.ArrowID] = arrow;
                }
            }

            foreach (string id in realArrowIDs)
            {
                List<UnityEngine.Vector2Int> path = grid.GetArrowPath(id);
                if (path.Count < 2)
                {
                    return (false, $"Mũi tên số {id} chưa hoàn thiện! Chiều dài tối thiểu phải từ 2 ô.");
                }

                if (!arrowsById.TryGetValue(id, out ArrowSaveData arrow) || arrow == null)
                {
                    return (false, $"Không thể đọc metadata của mũi tên {id}.");
                }

                int endpointCount = arrow.Endpoints != null ? arrow.Endpoints.Count : 0;
                if (arrow.TopologyType == ArrowTopologyType.MultiEndpointSharedPath)
                {
                    if (endpointCount != 2)
                    {
                        return (false, $"Mũi tên số {id} đang ở mode 2 đầu nhưng không có đúng 2 endpoints.");
                    }
                }
                else if (endpointCount != 1)
                {
                    return (false, $"Mũi tên số {id} phải có đúng 1 endpoint chính.");
                }

                if (arrow.Endpoints != null)
                {
                    HashSet<int> endpointIndices = new HashSet<int>();
                    for (int i = 0; i < arrow.Endpoints.Count; i++)
                    {
                        ArrowEndpointSaveData endpoint = arrow.Endpoints[i];
                        if (endpoint == null)
                        {
                            return (false, $"Mũi tên số {id} có endpoint bị thiếu dữ liệu.");
                        }

                        if (endpoint.PathIndex != 0 && endpoint.PathIndex != path.Count - 1)
                        {
                            return (false, $"Endpoint của mũi tên số {id} phải nằm ở một trong hai đầu path.");
                        }

                        if (!endpointIndices.Add(endpoint.PathIndex))
                        {
                            return (false, $"Mũi tên số {id} đang trùng endpoint ở cùng một đầu path.");
                        }
                    }
                }
            }

            Dictionary<string, int> portalCounts = new Dictionary<string, int>();
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var specialCell = grid.GetSpecialCellAt(x, y);
                    if (specialCell == null || specialCell.Type != BoardSpecialType.Portal) continue;

                    string portalId = string.IsNullOrWhiteSpace(specialCell.PortalId)
                        ? string.Empty
                        : specialCell.PortalId.Trim();

                    if (string.IsNullOrEmpty(portalId))
                    {
                        return (false, $"Portal tại ô ({x}, {y}) đang thiếu Portal ID.");
                    }

                    if (!portalCounts.ContainsKey(portalId))
                    {
                        portalCounts[portalId] = 0;
                    }

                    portalCounts[portalId]++;
                }
            }

            foreach (KeyValuePair<string, int> portalCount in portalCounts)
            {
                if (portalCount.Value != 2)
                {
                    return (false,
                        $"Portal ID '{portalCount.Key}' phải xuất hiện đúng 2 ô, hiện tại là {portalCount.Value}.");
                }
            }

            return (true, "Bản đồ hợp lệ!");
        }
    }
}
