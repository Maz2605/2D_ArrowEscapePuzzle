using System.Collections.Generic;

namespace EditorTool.Scripts.EditorTool.Logic
{
    public class MapValidator
    {
        public static (bool isValid, string errorMsg) ValidateBaseMap(GridSystem grid)
        {
            List<string> arrowIDs = grid.GetAllArrowIDs();

            if (arrowIDs == null || arrowIDs.Count == 0)
            {
                return (false, "Bản đồ đang trống! Bạn phải vẽ ít nhất 1 mũi tên.");
            }

            foreach (string id in arrowIDs)
            {
                var path = grid.GetArrowPath(id);
                
                if (path != null && path.Count == 1) 
                {
                    return (false, $"Mũi tên số {id} quá ngắn (Chỉ có 1 ô)! \n\nChiều dài tối thiểu phải từ 2 ô trở lên để có đủ Đầu và Đuôi.");
                }
            }

            return (true, "Bản đồ hợp lệ 100%!");
        }
    }
}