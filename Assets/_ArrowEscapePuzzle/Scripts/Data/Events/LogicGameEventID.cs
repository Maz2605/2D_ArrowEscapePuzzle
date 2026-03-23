namespace ArrowGame.Data.Events
{
    public enum LogicGameEventID
    {
        None,
        LevelLoaded,
        LevelComplete,
        LevelFailed,
        
        ArrowEscaped,
        ArrowBlocked,
        
        HeartChanged,
    }
}