namespace ArrowGame.Data.Events
{
    public enum VisualEventID
    {
        None,
        ArrowWrongImpact,
        ArrowEscaped,
        ArrowPassedGridPosition,
        WinAnimationComplete,
        GridIntroComplete,
        DifficultyIntroComplete,
        IntroAnimationComplete,
        LoseAnimationComplete,
        
        PlayTapAuraVFX,
        TapArrowHit,      // Tap trúng mũi tên (dùng cho Camera Shake + Grid Bounce)
        ThemeChanged,
        
        CoinCountTick,     
        CoinCountComplete,
        CameraMoved,
        SpecialCellRejection,
        
        // Khi đóng BoosterIntroductionPopup: icon bay từ popup về slot BottomHUD
        PlayBoosterUnlockAnimation,
        LosePopupShown
    }
}
