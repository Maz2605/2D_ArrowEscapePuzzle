using Newtonsoft.Json;
using ShareCore.Data;
using UnityEngine;

namespace ShareCore.Scripts.Data
{
    /// <summary>
    /// Dữ liệu cho ô Mystery Box.
    /// Kế thừa từ SpecialCellSaveData và bổ sung thuộc tính WrappedCell
    /// để chứa ô đặc biệt ẩn bên trong (Portal, Redirect, v.v.) hoặc null nếu hộp trống.
    /// </summary>
    public class MysteryBoxSaveData : SpecialCellSaveData
    {
        [JsonProperty("wrappedCell")]
        public SpecialCellSaveData WrappedCell;

        public MysteryBoxSaveData()
        {
            Type = BoardSpecialType.MysteryBox;
        }

        public MysteryBoxSaveData(Vector2Int position, string lockGroupId,
            SpecialCellSaveData wrappedCell = null)
        {
            Position = position;
            Type = BoardSpecialType.MysteryBox;
            Id = lockGroupId ?? string.Empty;
            WrappedCell = wrappedCell;
        }
    }
}
