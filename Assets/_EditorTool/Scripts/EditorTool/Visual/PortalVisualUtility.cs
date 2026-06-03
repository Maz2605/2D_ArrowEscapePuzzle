using System.Collections.Generic;
using UnityEngine;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public static class PortalVisualUtility
    {
        private static readonly Dictionary<string, Color> ColorByPortalId = new Dictionary<string, Color>();
        private static readonly HashSet<int> UsedColorKeys = new HashSet<int>();
        private static readonly float[] SaturationSteps = { 0.9f, 0.78f, 0.66f };
        private static readonly float[] ValueSteps = { 0.98f, 0.9f, 0.82f };

        public static string NormalizePortalId(string portalId)
        {
            return string.IsNullOrWhiteSpace(portalId)
                ? "?"
                : portalId.Trim().ToUpperInvariant();
        }

        public static Color GetPortalColor(string portalId)
        {
            string normalizedPortalId = NormalizePortalId(portalId);
            if (ColorByPortalId.TryGetValue(normalizedPortalId, out Color cachedColor))
            {
                return cachedColor;
            }

            uint seed = ComputeHash(normalizedPortalId);
            int hueCount = 72;
            int satCount = SaturationSteps.Length;
            int valCount = ValueSteps.Length;
            int candidateCount = hueCount * satCount * valCount;

            for (int attempt = 0; attempt < candidateCount; attempt++)
            {
                int hueIndex = (int)((seed + (uint)(attempt * 29)) % (uint)hueCount);
                int satIndex = (int)(((seed / 73u) + (uint)(attempt * 5)) % (uint)satCount);
                int valIndex = (int)(((seed / 151u) + (uint)(attempt * 7)) % (uint)valCount);

                float hue = hueIndex / (float)hueCount;
                Color candidate = Color.HSVToRGB(hue, SaturationSteps[satIndex], ValueSteps[valIndex]);
                int colorKey = ToColorKey(candidate);
                if (UsedColorKeys.Contains(colorKey)) continue;

                UsedColorKeys.Add(colorKey);
                ColorByPortalId[normalizedPortalId] = candidate;
                return candidate;
            }

            Color fallback = Color.HSVToRGB((seed % 360u) / 360f, 0.95f, 0.98f);
            ColorByPortalId[normalizedPortalId] = fallback;
            UsedColorKeys.Add(ToColorKey(fallback));
            return fallback;
        }

        private static uint ComputeHash(string normalizedPortalId)
        {
            uint hash = 2166136261u;
            for (int i = 0; i < normalizedPortalId.Length; i++)
            {
                hash ^= normalizedPortalId[i];
                hash *= 16777619u;
            }

            return hash;
        }

        private static int ToColorKey(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255f);
            int g = Mathf.RoundToInt(color.g * 255f);
            int b = Mathf.RoundToInt(color.b * 255f);
            return (r << 16) | (g << 8) | b;
        }
    }
}
