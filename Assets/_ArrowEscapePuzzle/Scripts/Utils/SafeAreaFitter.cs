using UnityEngine;

/// <summary>
/// Tự động co RectTransform sao cho vừa khít với vùng an toàn (Safe Area) của thiết bị.
/// Tránh UI bị lẹm vào Tai thỏ (Notch), Dynamic Island, hoặc thanh điều hướng ảo.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways] 
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
    private Canvas _canvas;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        ApplySafeArea();
    }

    /// <summary>
    /// Bắt sự kiện khi màn hình xoay hoặc thay đổi kích thước.
    /// Tối ưu hiệu năng: Thay thế hoàn toàn cho việc gọi trong Update().
    /// </summary>
    private void OnRectTransformDimensionsChange()
    {
        ApplySafeArea();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
        {
            ApplySafeArea();
        }
    }
#endif

    private void ApplySafeArea()
    {
        if (_rectTransform == null) return;

        Rect safeArea = Screen.safeArea;

        if (safeArea == _lastSafeArea) return;
        _lastSafeArea = safeArea;

        if (_canvas == null)
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) return;
        }

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rectTransform.anchorMin = anchorMin;
        _rectTransform.anchorMax = anchorMax;
    }
}