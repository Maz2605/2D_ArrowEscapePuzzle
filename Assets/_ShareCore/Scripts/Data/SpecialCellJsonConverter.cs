using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShareCore.Data;

namespace ShareCore.Scripts.Data
{
    /// <summary>
    /// Custom JsonConverter cho SpecialCellSaveData.
    /// Khi đọc JSON, nếu trường "type" là "MysteryBox" thì khởi tạo MysteryBoxSaveData,
    /// ngược lại khởi tạo SpecialCellSaveData mặc định.
    /// </summary>
    public class SpecialCellJsonConverter : JsonConverter<SpecialCellSaveData>
    {
        public override SpecialCellSaveData ReadJson(JsonReader reader, Type objectType,
            SpecialCellSaveData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            JObject jsonObject = JObject.Load(reader);

            // Đọc trường "type" để phân biệt loại ô đặc biệt
            string typeString = jsonObject["type"]?.Value<string>() ?? string.Empty;

            SpecialCellSaveData target;

            if (string.Equals(typeString, BoardSpecialType.MysteryBox.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                target = new MysteryBoxSaveData();
            }
            else
            {
                target = new SpecialCellSaveData();
            }

            // Dùng serializer mặc định để điền các trường dữ liệu còn lại
            using JsonReader objectReader = jsonObject.CreateReader();
            serializer.Populate(objectReader, target);

            return target;
        }

        // Không ghi đè hàm WriteJson, dùng bộ ghi mặc định để tránh lặp vô hạn
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, SpecialCellSaveData value,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
