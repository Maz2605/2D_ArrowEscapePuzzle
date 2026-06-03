using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Logic;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "LineGuideBooster", menuName = "ArrowGame/Boosters/LineGuide")]
    public class LineGuideBooster : BoosterConfigSO
    {
        public override bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty();
        }
    }
}
