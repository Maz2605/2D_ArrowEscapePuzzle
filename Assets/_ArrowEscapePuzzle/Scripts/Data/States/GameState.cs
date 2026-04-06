namespace ArrowGame.Data.States
{
    /// <summary>
    /// Trạng thái cấp App (macro-state): quản lý màn hình nào đang hiển thị.
    /// Khi đang ở InGame, chi tiết trạng thái gameplay được quản lý bởi InGameState.
    /// </summary>
    public enum GameState
    {
        None,
        Loading,
        MainMenu,
        Shop,
        InGame   // Đại diện cho toàn bộ quá trình ở trong một màn chơi
    }
}