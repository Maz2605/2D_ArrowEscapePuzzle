using UnityEngine;

namespace ShareCore.Scripts
{
    public static class CameraZoomUtility
    {
        private const float MinZoomMultiplier = 0.01f;

        public static float CalculateOrthographicSize(
            float currentOrthographicSize,
            float zoomSteps,
            float zoomStepPercent,
            float minOrthographicSize,
            float maxOrthographicSize)
        {
            if (currentOrthographicSize <= 0f)
            {
                return Mathf.Clamp(minOrthographicSize, minOrthographicSize, maxOrthographicSize);
            }

            if (Mathf.Abs(zoomSteps) <= Mathf.Epsilon)
            {
                return Mathf.Clamp(currentOrthographicSize, minOrthographicSize, maxOrthographicSize);
            }

            float zoomMultiplier = Mathf.Max(MinZoomMultiplier, 1f + zoomStepPercent);
            float targetOrthographicSize = currentOrthographicSize * Mathf.Pow(zoomMultiplier, zoomSteps);
            return Mathf.Clamp(targetOrthographicSize, minOrthographicSize, maxOrthographicSize);
        }

        public static Vector3 CalculatePointerAnchoredPosition(
            Vector3 cameraPosition,
            float currentOrthographicSize,
            float targetOrthographicSize,
            float cameraAspect,
            Vector2 screenPosition,
            Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return cameraPosition;
            }

            Vector2 viewport = new Vector2(screenPosition.x / screenSize.x, screenPosition.y / screenSize.y);
            Vector2 centeredViewport = viewport - new Vector2(0.5f, 0.5f);

            Vector2 worldBefore = ScreenToWorld(cameraPosition, currentOrthographicSize, cameraAspect, centeredViewport);
            Vector2 worldAfter = ScreenToWorld(cameraPosition, targetOrthographicSize, cameraAspect, centeredViewport);
            Vector2 compensation = worldBefore - worldAfter;

            return new Vector3(cameraPosition.x + compensation.x, cameraPosition.y + compensation.y, cameraPosition.z);
        }

        private static Vector2 ScreenToWorld(
            Vector3 cameraPosition,
            float orthographicSize,
            float aspect,
            Vector2 centeredViewport)
        {
            float halfWidth = orthographicSize * aspect;
            float halfHeight = orthographicSize;

            return new Vector2(
                cameraPosition.x + (centeredViewport.x * 2f * halfWidth),
                cameraPosition.y + (centeredViewport.y * 2f * halfHeight));
        }
    }
}
