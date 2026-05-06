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
        TapArrowHit,      // Tap trúng mũi tên (dùng cho Camera Shake + Grid Bounce)
        ThemeChanged,
        
        CoinCountTick,     
        CoinCountComplete,
        CameraMoved
    }
}