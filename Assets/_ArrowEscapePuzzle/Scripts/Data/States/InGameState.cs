        namespace ArrowGame.Data.States
        {
            public enum InGameState
            {
                None,
                Intro,               
                Playing,             
                Paused,              
                BoosterIntroduction,
                BoosterInstruction,
                WaitingBoosterTarget,
                BoosterExecuting,
                WinPending,          // Thắng đã xác nhận, đang chờ delay trước khi bắt đầu animation
                LosePending,         // Thua đã xác nhận, đang chờ delay trước khi bắt đầu animation
                WinAnimating,
                LoseAnimating,
                Win,                 
                Lose                 
            }
        }
