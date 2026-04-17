namespace ArrowGame.Data.Events
{
    public enum VisualEventID
    {
        None,
        ArrowWrongImpact,
        ArrowEscaped,
        
        WinAnimationComplete,
        IntroAnimationComplete,
        LoseAnimationComplete,
        
        //Booster
        ShowHintVisual,
        ShowDirectionLines,
        BoosterTargetModeChanged,
        PlayBoosterVFX,
        PlayChainBoosterVFX,
        PlayDashEscape,
        ShowFocusHighlight, 
        HideFocusHighlight, 
        DarkenScreen,
        
        PlayTapAuraVFX,
        ThemeChanged,
        
        CoinCountTick,     
        CoinCountComplete
    }
}