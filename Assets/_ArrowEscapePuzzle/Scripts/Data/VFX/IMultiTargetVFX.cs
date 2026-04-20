using System.Collections.Generic;
using ArrowGame.Gameplay.Visual;

namespace ArrowGame.Data.VFX
{
    public interface IMultiTargetVFX
    {
        void PlayMultiVisual(List<ArrowLineView> targets, float totalDuration);
    }
}