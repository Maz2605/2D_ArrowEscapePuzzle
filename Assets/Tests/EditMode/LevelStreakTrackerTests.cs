using ArrowGame.Gameplay.Logic;
using NUnit.Framework;

namespace ArrowGame.Tests.EditMode
{
    public class LevelStreakTrackerTests
    {
        [Test]
        public void ThreeEligibleWins_ActivatesStreakOnThirdWin()
        {
            LevelStreakTracker tracker = new LevelStreakTracker();

            tracker.BeginLevelAttempt(1, 1, false);
            tracker.HandleLevelWin();
            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(1));
            Assert.That(tracker.IsStreakActive, Is.False);

            tracker.BeginLevelAttempt(2, 2, false);
            tracker.HandleLevelWin();
            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(2));
            Assert.That(tracker.IsStreakActive, Is.False);

            tracker.BeginLevelAttempt(3, 3, false);
            tracker.HandleLevelWin();
            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(3));
            Assert.That(tracker.IsStreakActive, Is.True);
        }

        [Test]
        public void Failure_ResetsCurrentStreak()
        {
            LevelStreakTracker tracker = new LevelStreakTracker();

            tracker.BeginLevelAttempt(1, 1, false);
            tracker.HandleLevelWin();
            tracker.BeginLevelAttempt(2, 2, false);
            tracker.HandleLevelWin();

            tracker.HandleLevelFail();

            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(0));
            Assert.That(tracker.IsStreakActive, Is.False);
            Assert.That(tracker.CurrentLevelAttemptIsStreakEligible, Is.False);
        }

        [Test]
        public void ReplayingSameLevelAfterFail_DoesNotContinueStreak()
        {
            LevelStreakTracker tracker = new LevelStreakTracker();

            tracker.BeginLevelAttempt(1, 1, false);
            tracker.HandleLevelWin();
            tracker.BeginLevelAttempt(2, 2, false);
            tracker.HandleLevelWin();
            tracker.BeginLevelAttempt(3, 3, false);
            tracker.HandleLevelFail();

            tracker.BeginLevelAttempt(3, 3, false);

            Assert.That(tracker.CurrentLevelAttemptIsStreakEligible, Is.False);

            tracker.HandleLevelWin();
            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(0));

            tracker.BeginLevelAttempt(4, 4, false);
            Assert.That(tracker.CurrentLevelAttemptIsStreakEligible, Is.True);

            tracker.HandleLevelWin();
            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(1));
        }

        [Test]
        public void NonFrontierOrPreviouslyPlayedLevel_ResetsStreakAndIsNotEligible()
        {
            LevelStreakTracker tracker = new LevelStreakTracker();

            tracker.BeginLevelAttempt(1, 1, false);
            tracker.HandleLevelWin();
            tracker.BeginLevelAttempt(2, 2, false);
            tracker.HandleLevelWin();

            tracker.BeginLevelAttempt(1, 3, true);

            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(0));
            Assert.That(tracker.CurrentLevelAttemptIsStreakEligible, Is.False);
        }

        [Test]
        public void ResetSession_ClearsAttemptHistoryAndCurrentStreak()
        {
            LevelStreakTracker tracker = new LevelStreakTracker();

            tracker.BeginLevelAttempt(1, 1, false);
            tracker.HandleLevelWin();
            tracker.BeginLevelAttempt(2, 2, false);
            tracker.HandleLevelWin();

            tracker.ResetSession();

            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(0));
            Assert.That(tracker.CurrentLevelAttemptIsStreakEligible, Is.False);

            tracker.BeginLevelAttempt(1, 1, false);
            Assert.That(tracker.CurrentLevelAttemptIsStreakEligible, Is.True);
        }

        [Test]
        public void RestorePersistedStreak_PreservesCountAndResetsAttemptHistory()
        {
            LevelStreakTracker tracker = new LevelStreakTracker();

            tracker.BeginLevelAttempt(1, 1, false);
            tracker.HandleLevelWin();
            tracker.BeginLevelAttempt(2, 2, false);
            tracker.HandleLevelWin();

            tracker.RestorePersistedStreak(2);

            Assert.That(tracker.CurrentWinStreak, Is.EqualTo(2));
            Assert.That(tracker.CurrentLevelAttemptIsStreakEligible, Is.False);

            tracker.BeginLevelAttempt(3, 3, false);
            Assert.That(tracker.CurrentLevelAttemptIsStreakEligible, Is.True);
        }
    }
}
