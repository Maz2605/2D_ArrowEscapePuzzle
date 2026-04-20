namespace GameCore.Data
{
    public enum UISoundType
    {
        None = 0,           // Im lặng

        // --- BUTTON ---
        ClickNormal,
        ClickBack,
        ClickConfirm,
        ClickCancel,

        // --- POPUP (Open/Close) ---
        PopupOpenStandard,  // Mở bảng thường
        PopupCloseStandard, // Đóng bảng thường
        PopupOpenWin,       // Mở bảng Win (hoành tráng)
        PopupOpenAlert,     // Mở bảng Cảnh báo (Ting!)
        
        // --- TOAST ---
        ToastInfo,          // Tin nhắn thường
        ToastSuccess,       // Thành công (Ting ting)
        ToastError,         // Lỗi (Buzz!)

        // --- GAME EVENTS ---
        LevelStart,
        PurchaseSuccess,
        
        Custom
    }
}