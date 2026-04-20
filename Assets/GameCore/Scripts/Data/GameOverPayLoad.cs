using System.Collections.Generic;

namespace GameCore.Data
{
    public struct GameOverPayLoad
    {
        public int FinalScore;
        public List<int> TopScores;

        public GameOverPayLoad(int finalScore, List<int> topScores)
        {
            FinalScore = finalScore;
            TopScores = topScores;
        }
    }
}