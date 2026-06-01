using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Logic;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "HintBooster", menuName = "ArrowGame/Boosters/Hint")]
    public class HintBoosterSO : BoosterConfigSO
    {
        public override bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty() && gridLogic.GetOneEscapableArrow() != null;
        }
    }
}
