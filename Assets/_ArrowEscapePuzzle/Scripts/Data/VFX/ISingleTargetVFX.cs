using ArrowGame.Gameplay.Visual;
using UnityEngine;

namespace ArrowGame.Data.VFX
{
    public interface ISingleTargetVFX
    {
        void PlayVisual(ArrowLineView arrowView, Vector3 spawnPos, string arrowId, float duration);
    }
}