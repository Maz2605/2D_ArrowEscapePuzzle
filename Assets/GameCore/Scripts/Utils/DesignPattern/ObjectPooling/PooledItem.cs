using UnityEngine;
using UnityEngine.Pool;

namespace GameCore.Utils.DesignPattern.ObjectPooling
{
    public class PooledItem : MonoBehaviour
    {
        public IObjectPool<GameObject> Pool { get; set; }

        public void Release()
        {
            if (Pool != null)
            {
                Pool.Release(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}