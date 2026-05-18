    using UnityEngine;

namespace ArrowGame.Utils
{
    public static class CameraUtils
    {
        // Tính khoảng cách từ startPoint đến mép màn hình theo direction (Orthographic)
        public static float GetDistanceToEdge(Camera cam, Vector3 startPoint, Vector3 direction)
        {
            if (cam == null) return 20f;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 camPos = cam.transform.position;

            float minX = camPos.x - halfWidth;
            float maxX = camPos.x + halfWidth;
            float minY = camPos.y - halfHeight;
            float maxY = camPos.y + halfHeight;

            if (direction.x > 0.5f) return Mathf.Max(0f, maxX - startPoint.x);
            if (direction.x < -0.5f) return Mathf.Max(0f, startPoint.x - minX);
            if (direction.y > 0.5f) return Mathf.Max(0f, maxY - startPoint.y);
            if (direction.y < -0.5f) return Mathf.Max(0f, startPoint.y - minY);

            return 0f;
        }
    }
}