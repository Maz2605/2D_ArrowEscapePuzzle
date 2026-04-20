using EditorTool.Scripts.Data;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace EditorTool.Scripts.EditorTool.Controller
{
    public class EditorCameraController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Kéo thả Main Camera vào đây")]
        [SerializeField] private Camera targetCamera; 

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 20f;

        [Header("Pan Settings (Di chuyển)")]
        [Tooltip("Sử dụng Chuột Giữa (Middle Mouse) để kéo Camera")]
        [SerializeField] private float panSpeed = 1f;

        [Header("Layout Settings")]
        [Tooltip("Tỷ lệ màn hình dành cho UI BÊN PHẢI (Ví dụ: 0.3 = 30%)")]
        [SerializeField] private float uiWidthRatio = 0.3f; 

        private Vector2 _focusPoint; // Điểm trung tâm mà Camera đang ngắm vào
        private Vector2 _lastMousePos;
        private bool _isPanning;

        private void OnEnable()
        {
            EventManager<EditorEventType>.AddListener<(int, int)>(
                EditorEventType.MapLoadedOrCreated, OnMapLoadedOrCreated);
        }

        private void OnDisable()
        {
            EventManager<EditorEventType>.RemoveListener<(int, int)>(
                EditorEventType.MapLoadedOrCreated, OnMapLoadedOrCreated);
        }

        private void OnMapLoadedOrCreated((int width, int height) mapSize)
        {
            FocusOnMap(mapSize.width, mapSize.height);
        }

        private void Awake()
        {
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void Update()
        {
            if (targetCamera == null) return;
            
            HandleZoom();
            HandlePan();
        }

        // Gọi hàm này 1 lần khi Load/Tạo Map
        public void FocusOnMap(int mapWidth, int mapHeight)
        {
            if (targetCamera == null) return;

            // Khóa mục tiêu vào tâm Map
            _focusPoint = new Vector2((mapWidth - 1) / 2f, (mapHeight - 1) / 2f);
            
            targetCamera.orthographicSize = Mathf.Max(mapHeight / 2f + 2f, 5f);
            
            ApplyCameraPosition();
        }

        private void HandleZoom()
        {
            if (Mouse.current == null) return;

            float scrollValue = Mouse.current.scroll.ReadValue().y;
            
            if (Mathf.Abs(scrollValue) > 0.1f)
            {
                // Không Zoom khi con trỏ đang nằm trên UI Panel
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

                float newSize = targetCamera.orthographicSize - (scrollValue * zoomSpeed * 0.01f);
                targetCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);

                ApplyCameraPosition();
            }
        }

        private void HandlePan()
        {
            if (Mouse.current == null) return;

            // Bấm chuột giữa để bắt đầu kéo (Pan)
            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                _isPanning = true;
                _lastMousePos = Mouse.current.position.ReadValue();
            }

            // Thả chuột giữa để kết thúc
            if (Mouse.current.middleButton.wasReleasedThisFrame)
            {
                _isPanning = false;
            }

            if (_isPanning)
            {
                Vector2 currentMousePos = Mouse.current.position.ReadValue();
                Vector2 deltaPos = currentMousePos - _lastMousePos;

                float orthoSize = targetCamera.orthographicSize;
                float aspect = targetCamera.aspect;
                
                // Quy đổi Pixel trên màn hình sang World Unit
                float worldDeltaX = (deltaPos.x / Screen.width) * (2f * orthoSize * aspect);
                float worldDeltaY = (deltaPos.y / Screen.height) * (2f * orthoSize);

                // Trừ đi khoảng cách di chuyển (Kéo chuột lên -> Camera dịch xuống)
                _focusPoint.x -= worldDeltaX * panSpeed;
                _focusPoint.y -= worldDeltaY * panSpeed;

                _lastMousePos = currentMousePos;

                ApplyCameraPosition();
            }
        }

        private void ApplyCameraPosition()
        {
            float worldScreenWidth = 2f * targetCamera.orthographicSize * targetCamera.aspect;
            float offsetX = worldScreenWidth * (uiWidthRatio / 2f);

            Vector3 targetPos = new Vector3(_focusPoint.x + offsetX, _focusPoint.y, -10f);
            targetCamera.transform.position = targetPos; 
        }
    }
}