using System.Collections.Generic;
using ShareCore.Data;

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

            foreach (string id in realArrowIDs)
            {
                List<UnityEngine.Vector2Int> path = grid.GetArrowPath(id);
                if (path.Count < 2)
                {
                    return (false, $"Mũi tên số {id} chưa hoàn thiện! Chiều dài tối thiểu phải từ 2 ô.");
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
