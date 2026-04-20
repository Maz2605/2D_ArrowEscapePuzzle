using UnityEngine;
using System.IO;

namespace EditorTool.Scripts.EditorTool.Visual
{
    public class ReferenceImageController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private SpriteRenderer displayRenderer;
        [SerializeField] private float defaultZ = 1f; 

        private void Awake()
        {
            if (displayRenderer == null)
            {
                displayRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public void LoadReferenceImage()
        {
            if (displayRenderer == null) return;

#if UNITY_EDITOR
            string path = UnityEditor.EditorUtility.OpenFilePanel("Chọn ảnh làm mẫu", "", "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path)) return;

            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            
            if (tex.LoadImage(fileData))
            {
                Sprite newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                displayRenderer.sprite = newSprite;
                
                // Reset về mặc định mỗi khi load ảnh mới
                displayRenderer.transform.position = new Vector3(0, 0, defaultZ);
                displayRenderer.transform.localScale = Vector3.one;
            }
#endif
        }

        public void SetOpacity(float alpha)
        {
            if (displayRenderer == null) return;
            Color c = displayRenderer.color;
            c.a = alpha;
            displayRenderer.color = c;
        }

        // --- CÁC HÀM MỚI ĐỂ CĂN CHỈNH ---

        public void SetScale(float scale)
        {
            if (displayRenderer == null) return;
            displayRenderer.transform.localScale = Vector3.one * scale;
        }

        public void SetPositionX(float x)
        {
            if (displayRenderer == null) return;
            Vector3 pos = displayRenderer.transform.position;
            pos.x = x;
            displayRenderer.transform.position = pos;
        }

        public void SetPositionY(float y)
        {
            if (displayRenderer == null) return;
            Vector3 pos = displayRenderer.transform.position;
            pos.y = y;
            displayRenderer.transform.position = pos;
        }
    }
}