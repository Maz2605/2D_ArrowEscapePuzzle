using UnityEngine;

namespace ArrowGame.Gameplay.Managers
{
    public enum ZoomInputSource
    {
        MouseWheel = 0,
        Pinch = 1
    }

    public readonly struct ZoomInputData
    {
        public ZoomInputData(float delta, Vector2 screenPosition, ZoomInputSource source, bool usePointerAnchor)
        {
            Delta = delta;
            ScreenPosition = screenPosition;
            Source = source;
            UsePointerAnchor = usePointerAnchor;
        }

        public float Delta { get; }
        public Vector2 ScreenPosition { get; }
        public ZoomInputSource Source { get; }
        public bool UsePointerAnchor { get; }
    }
}
