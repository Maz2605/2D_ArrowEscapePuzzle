using System.Collections.Generic;
using ShareCore.Data;
using ShareCore.Scripts.Data;

namespace EditorTool.Scripts.EditorTool.Logic
{
    public class MapValidationResult
    {
        public bool isValid;
        public string errorMsg;
        public List<string> errorArrowIds = new List<string>();
        public List<UnityEngine.Vector2Int> errorCellPositions = new List<UnityEngine.Vector2Int>();
    }

    public class MapValidator
    {
        public static MapValidationResult ValidateBaseMap(GridSystem grid)
        {
            MapValidationResult result = new MapValidationResult();
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
                result.isValid = false;
                result.errorMsg = "Bản đồ đang trống! Bạn phải vẽ hoàn thiện ít nhất 1 mũi tên.";
                return result;
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
                    result.isValid = false;
                    result.errorMsg = $"Mũi tên số {id} chưa hoàn thiện! Chiều dài tối thiểu phải từ 2 ô.";
                    result.errorArrowIds.Add(id);
                    return result;
                }

                if (!arrowsById.TryGetValue(id, out ArrowSaveData arrow) || arrow == null)
                {
                    result.isValid = false;
                    result.errorMsg = $"Không thể đọc metadata của mũi tên {id}.";
                    result.errorArrowIds.Add(id);
                    return result;
                }

                int endpointCount = arrow.Endpoints != null ? arrow.Endpoints.Count : 0;
                if (arrow.TopologyType == ArrowTopologyType.MultiEndpointSharedPath)
                {
                    if (endpointCount != 2)
                    {
                        result.isValid = false;
                        result.errorMsg = $"Mũi tên số {id} đang ở mode 2 đầu nhưng không có đúng 2 endpoints.";
                        result.errorArrowIds.Add(id);
                        return result;
                    }
                }
                else if (endpointCount != 1)
                {
                    result.isValid = false;
                    result.errorMsg = $"Mũi tên số {id} phải có đúng 1 endpoint chính.";
                    result.errorArrowIds.Add(id);
                    return result;
                }

                if (arrow.Endpoints != null)
                {
                    HashSet<int> endpointIndices = new HashSet<int>();
                    for (int i = 0; i < arrow.Endpoints.Count; i++)
                    {
                        ArrowEndpointSaveData endpoint = arrow.Endpoints[i];
                        if (endpoint == null)
                        {
                            result.isValid = false;
                            result.errorMsg = $"Mũi tên số {id} có endpoint bị thiếu dữ liệu.";
                            result.errorArrowIds.Add(id);
                            return result;
                        }

                        if (endpoint.PathIndex != 0 && endpoint.PathIndex != path.Count - 1)
                        {
                            result.isValid = false;
                            result.errorMsg = $"Endpoint của mũi tên số {id} phải nằm ở một trong hai đầu path.";
                            result.errorArrowIds.Add(id);
                            return result;
                        }

                        if (!endpointIndices.Add(endpoint.PathIndex))
                        {
                            result.isValid = false;
                            result.errorMsg = $"Mũi tên số {id} đang trùng endpoint ở cùng một đầu path.";
                            result.errorArrowIds.Add(id);
                            return result;
                        }
                    }
                }
            }

            Dictionary<string, int> portalCounts = new Dictionary<string, int>();
            Dictionary<string, List<UnityEngine.Vector2Int>> portalPositions = new Dictionary<string, List<UnityEngine.Vector2Int>>();
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var specialCell = grid.GetSpecialCellAt(x, y);
                    if (specialCell == null || specialCell.Type != BoardSpecialType.Portal) continue;

                    string portalId = string.IsNullOrWhiteSpace(specialCell.PortalId)
                        ? string.Empty
                        : specialCell.PortalId.Trim();

                    UnityEngine.Vector2Int pos = new UnityEngine.Vector2Int(x, y);
                    if (string.IsNullOrEmpty(portalId))
                    {
                        result.isValid = false;
                        result.errorMsg = $"Portal tại ô ({x}, {y}) đang thiếu Portal ID.";
                        result.errorCellPositions.Add(pos);
                        return result;
                    }

                    if (!portalCounts.ContainsKey(portalId))
                    {
                        portalCounts[portalId] = 0;
                        portalPositions[portalId] = new List<UnityEngine.Vector2Int>();
                    }

                    portalCounts[portalId]++;
                    portalPositions[portalId].Add(pos);
                }
            }

            foreach (KeyValuePair<string, int> portalCount in portalCounts)
            {
                if (portalCount.Value != 2)
                {
                    result.isValid = false;
                    result.errorMsg = $"Portal ID '{portalCount.Key}' phải xuất hiện đúng 2 ô, hiện tại là {portalCount.Value}.";
                    if (portalPositions.TryGetValue(portalCount.Key, out var positions))
                    {
                        result.errorCellPositions.AddRange(positions);
                    }
                    return result;
                }
            }

            result.isValid = true;
            result.errorMsg = "Bản đồ hợp lệ!";
            return result;
        }

        /// <summary>
        /// Phân tích xem map có bị "level chết" không —
        /// tức là tồn tại nhóm mũi tên chặn nhau vòng tròn, không thể thoát ra theo bất kỳ thứ tự nào.
        /// Nên gọi SAU khi ValidateBaseMap() đã thành công.
        /// </summary>
        /// <returns>
        /// isSolvable = true  → map giải được bình thường, message là thứ tự gợi ý thoát<br/>
        /// isSolvable = false → map chết; message mô tả nhóm deadlock; result chứa chi tiết
        /// </returns>
        public static (bool isSolvable, string message, DeadlockAnalysisResult result)
            CheckDeadlock(GridSystem grid)
        {
            DeadlockAnalysisResult analysis = DeadlockAnalyzer.Analyze(grid);
            return (analysis.IsSolvable, analysis.Summary, analysis);
        }
    }
}
