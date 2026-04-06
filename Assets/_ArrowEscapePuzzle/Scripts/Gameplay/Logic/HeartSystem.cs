using ArrowGame.Data;
using ArrowGame.Data.Events;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.Gameplay.Logic
{
    public class HeartSystem
    {
        public int CurrentHeart { get; set; }
        private int MaxHearts { get; set; }
        
        private float _lastDamageTime = -999f;
        private readonly float _cooldownDuration;
        public HeartSystem(int maxHearts, float cooldownDuration = 1.0f)
        {
            MaxHearts = maxHearts;
            CurrentHeart = MaxHearts;
            _cooldownDuration = cooldownDuration;
            EventManager<LogicGameEventID>.Post(LogicGameEventID.HeartChanged, CurrentHeart);
        }

        public void RemoveHeart()
        {
            if (Time.time - _lastDamageTime < _cooldownDuration)
            {
                Debug.Log("[HeartSystem]: Cooldown time]");
                return;
            }
            
            CurrentHeart = Mathf.Clamp(CurrentHeart - 1, 0, MaxHearts);
            _lastDamageTime = Time.time;
            EventManager<LogicGameEventID>.Post(LogicGameEventID.HeartChanged, CurrentHeart);
            Debug.Log($"[HeartSystem]: Heart: {CurrentHeart}.");
            if (CurrentHeart == 0)
            {
                EventManager<LogicGameEventID>.Post(LogicGameEventID.LevelFailed);
            }
        }

        public void AddHeart(int amount)
        {
            if (amount <= 0) return;

            CurrentHeart = Mathf.Min(CurrentHeart + amount, MaxHearts);
            
            Debug.Log($"[HeartSystem]: Heart: {CurrentHeart}.");
            
            EventManager<LogicGameEventID>.Post(LogicGameEventID.HeartChanged, CurrentHeart);
        }
    }
}