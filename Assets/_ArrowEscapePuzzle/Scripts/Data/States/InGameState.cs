namespace ArrowGame.Data.States
{
    /// <summary>
    /// Trạng thái cấp Gameplay (micro-state): chỉ hoạt động khi GameState == InGame.
    /// Quản lý mọi pha diễn ra trong suốt một màn chơi.
    /// </summary>
    public enum InGameState
    {
        None,
        Intro,               
        Playing,             
        Paused,              
        WaitingBoosterTarget,
        BoosterExecuting,
        WinAnimating,
        LoseAnimating,
        Win,                 
        Lose                 
    }
}
