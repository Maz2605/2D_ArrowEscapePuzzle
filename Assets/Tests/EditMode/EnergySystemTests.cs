using System;
using ArrowGame.Data;
using ArrowGame.Gameplay.Logic;
using NUnit.Framework;

namespace ArrowGame.Tests.EditMode
{
    public class EnergySystemTests
    {
        private static readonly TimeSpan RecoveryInterval = TimeSpan.FromMinutes(30);

        [Test]
        public void NewProfile_StartsWithFullEnergy()
        {
            UserProfile profile = new UserProfile();
            EnergySystem system = new EnergySystem(5, RecoveryInterval);

            system.EnsureInitialized(profile, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            Assert.That(profile.CurrentEnergy, Is.EqualTo(5));
            Assert.That(profile.MaxEnergy, Is.EqualTo(5));
            Assert.That(profile.EnergyRecoveryStartedAtUtcTicks, Is.EqualTo(0));
        }

        [Test]
        public void ConsumingOneEnergy_StartsRecoveryTimer()
        {
            UserProfile profile = new UserProfile();
            EnergySystem system = new EnergySystem(5, RecoveryInterval);
            DateTime nowUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);

            bool consumed = system.TryConsume(profile, nowUtc);

            Assert.That(consumed, Is.True);
            Assert.That(profile.CurrentEnergy, Is.EqualTo(4));
            Assert.That(profile.EnergyRecoveryStartedAtUtcTicks, Is.EqualTo(nowUtc.Ticks));
        }

        [TestCase(30, 1, 1800)]
        [TestCase(60, 2, 1800)]
        [TestCase(150, 5, 0)]
        public void OfflineRecovery_RestoresExpectedEnergy(int elapsedMinutes, int expectedEnergy, int expectedRemainingSeconds)
        {
            UserProfile profile = new UserProfile
            {
                CurrentEnergy = 0,
                MaxEnergy = 5,
                EnergyRecoveryStartedAtUtcTicks = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc).Ticks
            };

            EnergySystem system = new EnergySystem(5, RecoveryInterval);
            DateTime refreshTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc).AddMinutes(elapsedMinutes);

            system.Refresh(profile, refreshTime);
            int remainingSeconds = system.GetRemainingRecoverySeconds(profile, refreshTime);

            Assert.That(profile.CurrentEnergy, Is.EqualTo(expectedEnergy));
            Assert.That(remainingSeconds, Is.EqualTo(expectedRemainingSeconds));
        }

        [Test]
        public void PartialRecovery_KeepsCountdownForNextEnergy()
        {
            DateTime startUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            UserProfile profile = new UserProfile
            {
                CurrentEnergy = 2,
                MaxEnergy = 5,
                EnergyRecoveryStartedAtUtcTicks = startUtc.Ticks
            };

            EnergySystem system = new EnergySystem(5, RecoveryInterval);
            DateTime refreshTime = startUtc.AddMinutes(45);

            system.Refresh(profile, refreshTime);
            int remainingSeconds = system.GetRemainingRecoverySeconds(profile, refreshTime);

            Assert.That(profile.CurrentEnergy, Is.EqualTo(3));
            Assert.That(remainingSeconds, Is.EqualTo(900));
        }

        [Test]
        public void FullEnergy_ClearsRecoveryTimer()
        {
            DateTime startUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
            UserProfile profile = new UserProfile
            {
                CurrentEnergy = 4,
                MaxEnergy = 5,
                EnergyRecoveryStartedAtUtcTicks = startUtc.Ticks
            };

            EnergySystem system = new EnergySystem(5, RecoveryInterval);
            system.Refresh(profile, startUtc.AddMinutes(30));

            Assert.That(profile.CurrentEnergy, Is.EqualTo(5));
            Assert.That(profile.EnergyRecoveryStartedAtUtcTicks, Is.EqualTo(0));
        }
    }
}
