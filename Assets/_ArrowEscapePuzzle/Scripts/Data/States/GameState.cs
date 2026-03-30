namespace ArrowGame.Data.States
{
    public enum GameState
    {
        None,
        Loading,
        MainMenu,
        Shop,
        IntroLevel,
        Playing,
        Paused,
        Win,
        Lose,
        
        WaitingBoosterTarget, 
        BoosterExecuting
    }
}