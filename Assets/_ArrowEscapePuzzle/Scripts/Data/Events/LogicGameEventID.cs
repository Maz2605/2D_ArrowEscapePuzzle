namespace ArrowGame.Data.Events
{
    public enum LogicGameEventID
    {
        None,
        GameStateChanged,
        InGameStateChanged,  
        
        LevelLoaded,
        LevelComplete,
        LevelFailed,
        
        ArrowEscaped,
        ArrowBlocked,
        
        HeartChanged,
        CoinChanged,
        ArrowCountChanged,
        
        BoosterChanged,         
        BoosterTargetSelected,

        LineGuideToggle,
        
        
        RequestLoadLevel
    }
}