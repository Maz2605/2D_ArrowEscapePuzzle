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
        ArrowForceRemove,
        SpecialCellChanged,
        SpecialCellDestroyed,
        
        HeartChanged,
        CoinChanged,
        ArrowCountChanged,
        
        BoosterChanged,         
        BoosterTargetSelected,

        LineGuideToggle,
        
        
        RequestLoadLevel
    }
}