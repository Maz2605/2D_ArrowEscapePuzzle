using System.Collections;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;

namespace GameCore.Utils
{
    public class AutoDespawn : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1.0f;

        private void OnEnable()
        {
            StartCoroutine(DespawnRoutine());
        }

        private IEnumerator DespawnRoutine()
        {
            yield return new WaitForSeconds(lifetime);
            PoolingManager.Instance.Despawn(this.gameObject);
        }
    }
}
