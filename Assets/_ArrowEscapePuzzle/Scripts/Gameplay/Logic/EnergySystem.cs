using System;
using ArrowGame.Data;

namespace ArrowGame.Gameplay.Logic
{
    public sealed class EnergySystem
    {
        private readonly int _defaultMaxEnergy;
        private readonly long _recoveryIntervalTicks;

        public EnergySystem(int defaultMaxEnergy, TimeSpan recoveryInterval)
        {
            _defaultMaxEnergy = Math.Max(1, defaultMaxEnergy);
            _recoveryIntervalTicks = Math.Max(1L, recoveryInterval.Ticks);
        }

        public void EnsureInitialized(UserProfile profile, DateTime nowUtc)
        {
            if (profile == null) return;

            if (profile.MaxEnergy <= 0)
            {
                profile.MaxEnergy = _defaultMaxEnergy;
            }

            if (profile.CurrentEnergy <= 0 && profile.EnergyRecoveryStartedAtUtcTicks <= 0)
            {
                profile.CurrentEnergy = profile.MaxEnergy;
            }

            profile.CurrentEnergy = Clamp(profile.CurrentEnergy, 0, profile.MaxEnergy);

            if (profile.CurrentEnergy >= profile.MaxEnergy)
            {
                profile.EnergyRecoveryStartedAtUtcTicks = 0;
                return;
            }

            if (profile.EnergyRecoveryStartedAtUtcTicks <= 0)
            {
                profile.EnergyRecoveryStartedAtUtcTicks = nowUtc.Ticks;
            }
        }

        public bool Refresh(UserProfile profile, DateTime nowUtc)
        {
            if (profile == null) return false;

            EnsureInitialized(profile, nowUtc);

            int beforeEnergy = profile.CurrentEnergy;
            long beforeTicks = profile.EnergyRecoveryStartedAtUtcTicks;

            if (profile.CurrentEnergy >= profile.MaxEnergy)
            {
                profile.EnergyRecoveryStartedAtUtcTicks = 0;
                return beforeEnergy != profile.CurrentEnergy || beforeTicks != profile.EnergyRecoveryStartedAtUtcTicks;
            }

            if (profile.EnergyRecoveryStartedAtUtcTicks > nowUtc.Ticks)
            {
                profile.EnergyRecoveryStartedAtUtcTicks = nowUtc.Ticks;
                return true;
            }

            long elapsedTicks = nowUtc.Ticks - profile.EnergyRecoveryStartedAtUtcTicks;
            if (elapsedTicks < _recoveryIntervalTicks)
            {
                return beforeEnergy != profile.CurrentEnergy || beforeTicks != profile.EnergyRecoveryStartedAtUtcTicks;
            }

            long recoveredUnits = elapsedTicks / _recoveryIntervalTicks;
            if (recoveredUnits <= 0)
            {
                return beforeEnergy != profile.CurrentEnergy || beforeTicks != profile.EnergyRecoveryStartedAtUtcTicks;
            }

            int recoveredEnergy = (int)Math.Min(recoveredUnits, profile.MaxEnergy - profile.CurrentEnergy);
            profile.CurrentEnergy = Clamp(profile.CurrentEnergy + recoveredEnergy, 0, profile.MaxEnergy);

            if (profile.CurrentEnergy >= profile.MaxEnergy)
            {
                profile.EnergyRecoveryStartedAtUtcTicks = 0;
            }
            else
            {
                profile.EnergyRecoveryStartedAtUtcTicks += recoveredEnergy * _recoveryIntervalTicks;
            }

            return beforeEnergy != profile.CurrentEnergy || beforeTicks != profile.EnergyRecoveryStartedAtUtcTicks;
        }

        public bool TryConsume(UserProfile profile, DateTime nowUtc)
        {
            if (profile == null) return false;

            Refresh(profile, nowUtc);
            if (profile.CurrentEnergy <= 0) return false;

            profile.CurrentEnergy = Clamp(profile.CurrentEnergy - 1, 0, profile.MaxEnergy);
            if (profile.CurrentEnergy >= profile.MaxEnergy)
            {
                profile.EnergyRecoveryStartedAtUtcTicks = 0;
            }
            else if (profile.EnergyRecoveryStartedAtUtcTicks <= 0)
            {
                profile.EnergyRecoveryStartedAtUtcTicks = nowUtc.Ticks;
            }

            return true;
        }

        public int GetRemainingRecoverySeconds(UserProfile profile, DateTime nowUtc)
        {
            if (profile == null) return 0;

            Refresh(profile, nowUtc);
            if (profile.CurrentEnergy >= profile.MaxEnergy) return 0;

            long elapsedTicks = Math.Max(0L, nowUtc.Ticks - profile.EnergyRecoveryStartedAtUtcTicks);
            long remainingTicks = _recoveryIntervalTicks - (elapsedTicks % _recoveryIntervalTicks);
            if (remainingTicks == _recoveryIntervalTicks && elapsedTicks > 0)
            {
                remainingTicks = _recoveryIntervalTicks;
            }

            return (int)Math.Ceiling(remainingTicks / (double)TimeSpan.TicksPerSecond);
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
