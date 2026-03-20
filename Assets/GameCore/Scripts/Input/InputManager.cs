using System;
using System.Collections.Generic;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameCore.Input
{
    public class InputManager : Singleton<InputManager>
    {   
        public event Action<Vector2> OnTouchMove;
        public event Action<Vector2> OnTouchEnd;
        public event Action<Vector2> OnTouchStart;

        private GameInput _inputActions;
        private Camera _mainCamera;
        private bool _isDragging;

        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private PointerEventData _pointerEventData;

        private Camera MainCamera
        {
            get
            {
                if (_mainCamera == null)
                {
                    _mainCamera = Camera.main;
                }
                return _mainCamera;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != null && Instance != this) 
            {
                return; 
            }
            
            KeepAlive(true);
            _inputActions = new GameInput();
        }

        private void OnEnable()
        {
            if (_inputActions == null) return;

            _inputActions.Enable();
            _inputActions.Touch.TouchContact.started += OnTouchPress;
            _inputActions.Touch.TouchContact.canceled += OnTouchCancel;
            _inputActions.Touch.TouchPosition.performed += OnTouchPosition;
        }

        private void OnDisable()
        {
            if (_inputActions == null) return;

            _inputActions.Touch.TouchContact.started -= OnTouchPress;
            _inputActions.Touch.TouchContact.canceled -= OnTouchCancel;
            _inputActions.Touch.TouchPosition.performed -= OnTouchPosition;
            _inputActions.Disable();
        }

        private void OnDestroy()
        {
            _inputActions?.Dispose();
        }

        private void OnTouchPress(InputAction.CallbackContext ctx)
        {
            if (IsPointerOverUI()) return;
            
            _isDragging = true;
            OnTouchStart?.Invoke(ReadTouchPosition());
        }

        private void OnTouchCancel(InputAction.CallbackContext ctx)
        {
            if (_isDragging)
            {
                _isDragging = false;
                OnTouchEnd?.Invoke(ReadTouchPosition());
            }
        }

        private void OnTouchPosition(InputAction.CallbackContext ctx)
        {
            if (_isDragging)
            {
                OnTouchMove?.Invoke(ctx.ReadValue<Vector2>());
            }
        }

        private Vector2 ReadTouchPosition()
        {
            // [FIX 2] Đọc trực tiếp từ Pointer.current để tránh bị trễ frame của Input System
            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }
            
            return _inputActions != null ? _inputActions.Touch.TouchPosition.ReadValue<Vector2>() : Vector2.zero;
        }

        public Vector3 GetWorldPosition()
        {
            if (MainCamera == null) return Vector3.zero;

            Vector2 screenPos = ReadTouchPosition();
            
            // [FIX 1] Bù trục Z bằng đúng khoảng cách từ Camera tới Grid (thường nằm ở Z = 0)
            float distanceToPlane = Mathf.Abs(MainCamera.transform.position.z);
            Vector3 screenPosWithZ = new Vector3(screenPos.x, screenPos.y, distanceToPlane);
            
            Vector3 worldPos = MainCamera.ScreenToWorldPoint(screenPosWithZ);
            worldPos.z = 0; // Ép phẳng lại về mặt 2D
            
            return worldPos;
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            Vector2 touchPos = ReadTouchPosition();

            _pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = touchPos
            };
            _raycastResults.Clear();

            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

            return _raycastResults.Count > 0;
        }
    }
}