using System;
using UnityEngine;

namespace ArrowGame.Data.VFX
{
    [Serializable]
    public struct VFXConfig
    {
        public GameObject prefab;
        public float offsetDistance; 
        public float duration;       
    }

    public struct TapVFXPayload
    {
        public Vector3 WorldPosition;
        public Vector2 ScreenPosition;
    }
    
    /// <summary>
    /// Payload cho event PlayBoosterUnlockAnimation.
    /// Clone icon đã được spawn sẵn bởi UIStateController, bay từ vị trí hiện tại về slot BottomHUD.
    /// </summary>
    public struct BoosterUnlockAnimPayload
    {
        /// Loại booster cần hiển thị unlock animation
        public ArrowGame.Data.Booster.BoosterType BoosterType;
        
        /// Sprite icon (dùng làm fallback nếu ExistingClone null)
        public Sprite Icon;
        
        /// Vị trí screen (pixels) điểm xuất phát (chỉ dùng khi ExistingClone null)
        public Vector2 StartScreenPos;
        
        /// Clone GameObject đã được spawn sẵn và đang đứng ở vị trí đúng.
        /// Nếu null, BottomHUD sẽ tự tạo clone từ Icon + StartScreenPos.
        public GameObject ExistingClone;
        
        /// Callback được gọi sau khi animation hoàn tất.
        public Action OnAnimComplete;
    }
}
