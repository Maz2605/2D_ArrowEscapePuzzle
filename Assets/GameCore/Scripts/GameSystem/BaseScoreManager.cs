using System;
using GameCore.Data;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace GameCore.GameSystem
{
    public abstract class BaseScoreManager<TUserData, TEventID> 
        where TUserData : class, IGameUserData, new()    
        where TEventID : struct, Enum
    {
        private readonly string _gameID;
        private readonly TEventID _scoreEventID;
        private readonly TEventID _highScoreEventID;
        
        protected TUserData UserData;
        
        public int CurrentScore { get; protected set; }
        public int HighScore => UserData?.HighScore ?? 0;
        protected BaseScoreManager(string gameID, TEventID scoreEventID, TEventID highScoreEventID) 
        {
            _gameID = gameID;
            _scoreEventID = scoreEventID;
            _highScoreEventID = highScoreEventID;

            LoadData();
        }


        protected void LoadData()
        {
            try
            {
                UserData = SaveSystem.Load<TUserData>(_gameID);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BaseScoreManager] Lỗi load data: {e.Message}. Tạo mới.");
            }

            if (UserData == null)
            {
                UserData = new TUserData();
            }

            CurrentScore = 0;

            ForceUpdateEventUI();
        }

        public void Save()
        {
            SaveSystem.Save(_gameID, UserData);
        }
        public virtual void AddScore(int amount)
        {
            CurrentScore += amount;
            
            EventManager<TEventID>.Post(_scoreEventID, CurrentScore);

            if (CurrentScore > UserData.HighScore)
            {
                UserData.HighScore = CurrentScore;
                EventManager<TEventID>.Post(_highScoreEventID, UserData.HighScore);
                Save();
            }
            
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            EventManager<TEventID>.Post(_scoreEventID, 0);
        }
        
        public void ForceUpdateEventUI()
        {
            if(UserData == null) return;
            
            EventManager<TEventID>.Post(_scoreEventID, CurrentScore);
            EventManager<TEventID>.Post(_highScoreEventID, UserData.HighScore);
        }
    }
}