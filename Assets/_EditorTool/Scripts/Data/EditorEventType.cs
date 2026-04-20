namespace EditorTool.Scripts.Data
{
    /// <summary>
    /// Chỉ chứa các system-level events (dùng EventManager toàn cục).
    /// UI tool actions KHÔNG dùng EventManager — dùng C# Action callback trực tiếp.
    /// </summary>
    public enum EditorEventType
    {
        ArrowSelected,       // payload: string arrowID
        MapLoadedOrCreated,  // payload: (int width, int height)
        PhaseChanged,        // payload: MakerPhase
    }
}
