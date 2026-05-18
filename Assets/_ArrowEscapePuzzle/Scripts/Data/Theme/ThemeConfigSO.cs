using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Data.Theme
{
    [CreateAssetMenu(fileName = "ThemeConfig_", menuName = "ArrowGame/Theme/Theme Config")]
    public class ThemeConfigSO : ScriptableObject
    {
        public string themeId; 
        
        [Header("Global Background")]
        // Background phải tối để tôn Neon lên, TUYỆT ĐỐI KHÔNG dùng HDR ở đây.
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f);

        [Header("Arrow Color Strategy")]
        public bool isRandomArrowColor = false; 
        
        [ColorUsage(true, true)] // Kích hoạt HDR cho màu mặc định
        public Color arrowDefaultColor = Color.white; 
        
        [Tooltip("Danh sách các màu sẽ được phát ngẫu nhiên cho mũi tên (nếu bật Random)")]
        [ColorUsage(true, true)] // Kích hoạt HDR cho toàn bộ danh sách Palette
        public List<Color> arrowColorPalette = new List<Color>();

        [Header("Shared Arrow States")]
        [ColorUsage(true, true)] // Lóe sáng đỏ rực khi bị block
        public Color arrowBlockedColor = Color.red;
        
        // Trạng thái Lose thường là xám mờ (chìm xuống), nên không cần HDR phát sáng
        public Color arrowLoseColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); 

        [Header("UI Palette")]
        // UI không dùng HDR để tránh việc chữ và nút bấm bị nhòe / lóa sáng
        public Color panelBackground = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        public Color primaryButton = new Color(0.2f, 0.6f, 1f);
        public Color secondaryButton = new Color(0.3f, 0.3f, 0.3f);
        public Color iconPrimary = Color.white;
        public Color textPrimary = Color.white;
        public Color navigationBarBackground = new Color(0.8f, 0.8f, 0.8f);
        public Color navigationBarHover = new Color(0.8f, 0.8f, 0.8f);
        
        [Header("Grid Palette")]
        // Nền Grid cũng cần chìm xuống để làm nền cho mũi tên
        public Color gridEmptyCell = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Redirect Cell Palette")]
        public bool isRandomRedirectColor = false;
        
        [ColorUsage(true, true)]
        public Color redirectDefaultColor = Color.white;
        
        [ColorUsage(true, true)]
        public List<Color> redirectColorPalette = new List<Color>();

        [Header("Blocker Counter Palette")]
        [ColorUsage(true, true)]
        public Color blockerCounterColor = Color.gray;

        [Header("Portal Palette")]
        [ColorUsage(true, true)] // Cổng Portal chắc chắn phải sáng rực rỡ
        public Color portalDefaultColor = Color.magenta;

        public Color GetColorByType(ThemeColorType type)
        {
            return type switch
            {
                ThemeColorType.Background => backgroundColor,
                ThemeColorType.PanelBackground => panelBackground,
                ThemeColorType.PrimaryButton => primaryButton,
                ThemeColorType.SecondaryButton => secondaryButton,
                ThemeColorType.IconPrimary => iconPrimary,
                ThemeColorType.TextPrimary => textPrimary,
                ThemeColorType.GridEmptyCell => gridEmptyCell,
                ThemeColorType.NavigationBarBackground => navigationBarBackground,
                ThemeColorType.NavigationBarHoverBackground => navigationBarHover,
                ThemeColorType.BlockerCounter => blockerCounterColor,
                ThemeColorType.Redirect => redirectDefaultColor,
                ThemeColorType.Portal => portalDefaultColor,
                _ => Color.white
            };
        }
    }
}