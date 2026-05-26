using System.Collections.Generic;

namespace ArrowGame.Gameplay.Logic
{
    public class LevelStreakTracker
    {
        public const int ActivationThreshold = 3;

        private readonly HashSet<int> _attemptedLevels = new HashSet<int>();

        public int CurrentWinStreak { get; private set; }
        public bool IsStreakActive => CurrentWinStreak >= ActivationThreshold;
        public bool CurrentLevelAttemptIsStreakEligible { get; private set; }

        public void BeginLevelAttempt(int levelIndex, int frontierLevelIndex, bool hasPlayedLevel)
        {
            bool isFrontierLevel = levelIndex == frontierLevelIndex;
            bool isFirstAttemptInSession = _attemptedLevels.Add(levelIndex);

            CurrentLevelAttemptIsStreakEligible = isFrontierLevel && !hasPlayedLevel && isFirstAttemptInSession;

            if (!CurrentLevelAttemptIsStreakEligible)
            {
                CurrentWinStreak = 0;
            }
        }

        public void HandleLevelWin()
        {
            if (CurrentLevelAttemptIsStreakEligible)
            {
                CurrentWinStreak++;
            }

            CurrentLevelAttemptIsStreakEligible = false;
        }

        public void HandleLevelFail()
        {
            CurrentWinStreak = 0;
            CurrentLevelAttemptIsStreakEligible = false;
        }

        public void ResetSession()
        {
            CurrentWinStreak = 0;
            CurrentLevelAttemptIsStreakEligible = false;
            _attemptedLevels.Clear();
        }

        public void RestorePersistedStreak(int streakValue)
        {
            CurrentWinStreak = streakValue < 0 ? 0 : streakValue;
            CurrentLevelAttemptIsStreakEligible = false;
            _attemptedLevels.Clear();
        }
    }
}
