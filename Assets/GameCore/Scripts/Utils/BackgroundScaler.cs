using UnityEngine;

namespace GameCore.Utils
{
    [ExecuteInEditMode]
    public class BackgroundScaler : MonoBehaviour
    {
        void LateUpdate()
        {
            ScaleToFill();
        }

        void ScaleToFill()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            Camera cam = Camera.main;

            if (sr == null || cam == null) return;

            transform.localScale = Vector3.one;

            float width = sr.sprite.bounds.size.x;
            float height = sr.sprite.bounds.size.y;

            double worldScreenHeight = cam.orthographicSize * 2.0;
            double worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

            double scaleX = worldScreenWidth / width;
            double scaleY = worldScreenHeight / height;

            float targetScale = (float)System.Math.Max(scaleX, scaleY);

            transform.localScale = new Vector3(targetScale, targetScale, 1);
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 10);
        }
    }
}