using UnityEngine;

namespace EditorTool.Scripts.Data
{
    public static class EditorConstants
    {
        private static readonly Color[] BaseArrowColors = new Color[]
        {
            new Color(0.9f, 0.2f, 0.2f), // 1: Đỏ
            new Color(0.2f, 0.6f, 1.0f), // 2: Xanh dương
            new Color(0.2f, 0.8f, 0.2f), // 3: Xanh lá
            new Color(1.0f, 0.8f, 0.1f), // 4: Vàng
            new Color(0.6f, 0.2f, 0.8f), // 5: Tím
            new Color(1.0f, 0.5f, 0.0f), // 6: Cam
            new Color(0.0f, 0.8f, 0.8f), // 7: Cyan
            new Color(0.9f, 0.4f, 0.6f)  // 8: Hồng
        };

        public static Color GetArrowColor(string arrowID)
        {
            if (int.TryParse(arrowID, out int idNumber))
            {
                int index = idNumber - 1;
                
                if (index >= 0 && index < BaseArrowColors.Length)
                {
                    return BaseArrowColors[index];
                }
                
                float goldenRatioConjugate = 0.618033988749895f;
                float h = (index * goldenRatioConjugate) % 1f;
                
                float s = 0.65f + (index % 3) * 0.1f; // Dao động 0.65 -> 0.85
                float v = 0.85f + (index % 2) * 0.1f; // Dao động 0.85 -> 0.95

                return Color.HSVToRGB(h, s, v);
            }
            
            return Color.white;
        }
    }
}