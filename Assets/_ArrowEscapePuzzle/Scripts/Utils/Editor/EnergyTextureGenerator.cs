using UnityEngine;
using UnityEditor;
using System.IO;

public class SpiralTextureGenerator : EditorWindow
{
    // Đổi lại menu path cho gọn gàng và phân cấp rõ ràng
    [MenuItem("Tools/Arrow Escape/Generate Spiral Texture")]
    public static void GenerateTexture()
    {
        int size = 256;
        // Tối ưu memory: Dùng R8 (Single Channel) thay vì RGBA32 vì đây chỉ là mask trắng đen
        Texture2D tex = new Texture2D(size, size, TextureFormat.R8, false); 

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;

                float diagonal = Mathf.Repeat(u + v, 1f);
                float glowValue = (Mathf.Sin(diagonal * Mathf.PI * 2f) + 1f) * 0.5f;

                Color pixelColor = new Color(glowValue, glowValue, glowValue, 1f);
                tex.SetPixel(x, y, pixelColor);
            }
        }

        tex.Apply();
        byte[] bytes = tex.EncodeToPNG();

        // --- XỬ LÝ ĐƯỜNG DẪN BỀN VỮNG ---
        string relativeFolderPath = "_ArrowEscapePuzzle/Art";
        string absoluteFolderPath = Path.Combine(Application.dataPath, relativeFolderPath);

        // Đảm bảo thư mục luôn tồn tại trước khi ghi file
        if (!Directory.Exists(absoluteFolderPath))
        {
            Directory.CreateDirectory(absoluteFolderPath);
            Debug.Log($"[Arrow Escape Tool] Đã tự động tạo thư mục: {relativeFolderPath}");
        }

        string filePath = Path.Combine(absoluteFolderPath, "SpiralTexture_Seamless.png");
        File.WriteAllBytes(filePath, bytes);
        // ---------------------------------

        AssetDatabase.Refresh(); 
        Debug.Log($"[Arrow Escape Tool] Đã gen thành công Texture tại: Assets/{relativeFolderPath}/SpiralTexture_Seamless.png");
    }
}