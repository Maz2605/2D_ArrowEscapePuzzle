namespace ArrowGame.Data.Events
{
    public enum LogicGameEventID
    {
        None,
        GameStateChanged,
        LevelLoaded,
        LevelComplete,
        LevelFailed,
        
        ArrowEscaped,
        ArrowBlocked,
        
        HeartChanged,
    }
}