using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Logic;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    [CreateAssetMenu(fileName = "HintBooster", menuName = "ArrowGame/Boosters/Hint")]
    public class HintBoosterSO : BoosterConfigSO
    {
        [Header("Hint Zoom Settings")]
        [Tooltip("The orthographic size camera will zoom to when focusing on the arrow.")]
        public float hintZoomSize = 6.5f;

        public override bool CanUse(GridSystem gridLogic)
        {
            return !gridLogic.IsBoardEmpty() && gridLogic.GetOneEscapableArrow() != null;
        }
    }
}
