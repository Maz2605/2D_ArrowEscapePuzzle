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
        MysteryBoxOpened,
        
        HeartChanged,
        EnergyChanged,
        EnergyTimerChanged,
        CoinChanged,
        ArrowCountChanged,
        StreakChanged,
        
        BoosterChanged,         
        BoosterTargetSelected,

        LineGuideToggle,
        
        
        RequestLoadLevel
    }
}
