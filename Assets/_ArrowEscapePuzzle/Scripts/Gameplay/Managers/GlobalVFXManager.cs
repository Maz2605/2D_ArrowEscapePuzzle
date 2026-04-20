using UnityEngine;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Singleton;
using GameCore.Utils.DesignPattern.ObjectPooling;

namespace ArrowGame.Gameplay.Managers
{
    public class GlobalVFXManager : Singleton<GlobalVFXManager>
    {
        public void PlayVFX(GameObject prefab, Vector3 position, Quaternion rotation, float duration)
        {
            if (prefab == null) return;

            GameObject vfx = PoolingManager.Instance.Spawn(prefab, position, rotation);

            DOVirtual.DelayedCall(duration, () => {
                if (vfx != null && vfx.activeInHierarchy)
                    PoolingManager.Instance.Despawn(vfx);
            }).SetLink(vfx);
        }
    }
}