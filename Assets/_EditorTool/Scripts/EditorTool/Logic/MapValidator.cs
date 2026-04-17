using System.Collections.Generic;
using UnityEngine;

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
                    var path = grid.GetArrowPath(id);
                    if (path != null && path.Count > 0)
                    {
                        realArrowIDs.Add(id);
                    }
                }
            }

            // 2. KIỂM TRA MAP TRỐNG: Đếm trên danh sách mũi tên thật
            if (realArrowIDs.Count == 0)
            {
                return (false, "Bản đồ đang trống! Bạn phải vẽ hoàn thiện ít nhất 1 mũi tên.");
            }

            // 3. KIỂM TRA TỪNG MŨI TÊN THẬT
            foreach (string id in realArrowIDs)
            {
                var path = grid.GetArrowPath(id);

                // Nếu mũi tên thật nhưng chỉ có 1 chấm (người dùng click 1 nhát rồi bỏ đó)
                if (path.Count < 2)
                {
                    return (false, $"Mũi tên số {id} chưa hoàn thiện! Chiều dài tối thiểu phải từ 2 ô.");
                }

                // // --- KIỂM TRA LỖI CON RẮN (SELF-TOUCHING) ---
                // // (Tôi vẫn giữ nguyên comment đoạn này theo code cũ của bạn)
                // for (int i = 0; i < path.Count; i++)
                // {
                //     Vector2Int current = path[i];
                //     Vector2Int[] neighbors = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                //
                //     foreach (var dir in neighbors)
                //     {
                //         Vector2Int neighborPos = current + dir;
                //         if (!grid.IsValidPosition(neighborPos.x, neighborPos.y)) continue;
                //
                //         var cellAtPos = grid.GetCell(neighborPos.x, neighborPos.y);
                //         if (cellAtPos.arrowID == id)
                //         {
                //             // Nếu ô cạnh bên có cùng ID, nó BẮT BUỘC phải là node liền kề trong List
                //             bool isSequential = false;
                //             if (i > 0 && path[i - 1] == neighborPos) isSequential = true;
                //             if (i < path.Count - 1 && path[i + 1] == neighborPos) isSequential = true;
                //
                //             if (!isSequential)
                //             {
                //                 return (false, $"LỖI CON RẮN: Mũi tên {id} tự chạm vào chính nó tại ô ({neighborPos.x}, {neighborPos.y}).");
                //             }
                //         }
                //     }
                // }
            }

            return (true, "Bản đồ hợp lệ!");
        }
    }
}