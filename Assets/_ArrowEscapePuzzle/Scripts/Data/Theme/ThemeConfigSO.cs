using System.Collections.Generic;
using UnityEngine;

namespace ArrowGame.Data.Theme
{
    [CreateAssetMenu(fileName = "ThemeConfig_", menuName = "ArrowGame/Theme/Theme Config")]
    public class ThemeConfigSO : ScriptableObject
    {
        public string themeId; // Ví dụ: "light" hoặc "dark"
        
        [Header("Global Background")]
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f);

        [Header("Arrow Color Strategy")]
        public bool isRandomArrowColor = false; 
        public Color arrowDefaultColor = Color.white; 
        
        [Tooltip("Danh sách các màu sẽ được phát ngẫu nhiên cho mũi tên (nếu bật Random)")]
        public List<Color> arrowColorPalette = new List<Color>();

        [Header("Shared Arrow States")]
        public Color arrowBlockedColor = Color.red;
        public Color arrowLoseColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Màu xám mờ đi

        [Header("UI Palette")]
        public Color panelBackground = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        public Color primaryButton = new Color(0.2f, 0.6f, 1f);
        public Color secondaryButton = new Color(0.3f, 0.3f, 0.3f);
        public Color iconPrimary = Color.white;
        public Color textPrimary = Color.white;
        
        [Header("Grid Palette")]
        public Color gridEmptyCell = new Color(0.3f, 0.3f, 0.3f, 0.5f);

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
                _ => Color.white
            };
        }
    }
}