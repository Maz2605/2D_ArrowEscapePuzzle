using System.Collections.Generic;
using GameCore.Utils.DesignPattern.Singleton;
using UnityEngine;
using UnityEngine.Pool;

namespace GameCore.Utils.DesignPattern.ObjectPooling
{
    public class PoolingManager : Singleton<PoolingManager>
    {
        private readonly Dictionary<int, IObjectPool<GameObject>> _pools = new();

        [Header("Global Settings")] [SerializeField]
        private int defaultCapacity = 10;

        [SerializeField] private int maxSize = 50;
        
        public void Prewarm(GameObject prefab, int count)
        {
            int key = prefab.GetInstanceID();
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = CreatePool(prefab);
            }

            var prewarmedObjects = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
            {
                prewarmedObjects.Add(pool.Get());
            }

            // Trả lại pool để đưa vào trạng thái Inactive sẵn sàng dùng
            foreach (var obj in prewarmedObjects)
            {
                pool.Release(obj);
            }
        }

        private IObjectPool<GameObject> CreatePool(GameObject prefab)
        {
            int key = prefab.GetInstanceID();
            var pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var instance = Instantiate(prefab);
                    var pooledItem = instance.AddComponent<PooledItem>();
                    return instance;
                },
                actionOnGet: null, 
                actionOnRelease: (obj) =>
                {
                    if (obj.TryGetComponent<IPoolable>(out var poolable)) poolable.OnDespawn();
                    obj.SetActive(false);
                },
                actionOnDestroy: Destroy,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            _pools.Add(key, pool);
            return pool;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            int key = prefab.GetInstanceID();
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = CreatePool(prefab);
            }

            var instance = pool.Get();

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(parent);

            instance.GetComponent<PooledItem>().Pool = pool;

            instance.SetActive(true);
            if (instance.TryGetComponent<IPoolable>(out var poolable)) poolable.OnSpawn();

            return instance;
        }


        public GameObject Spawn(GameObject prefab)
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity);
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            var instance = Spawn(prefab.gameObject, position, rotation, parent);
            return instance.GetComponent<T>();
        }

        public T Spawn<T>(T prefab) where T : Component
        {
            return Spawn(prefab, Vector3.zero, Quaternion.identity);
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            if (instance.TryGetComponent<PooledItem>(out var pooledItem))
            {
                pooledItem.Release();
            }
            else
            {
                Destroy(instance);
            }
        }

        public void ClearPools()
        {
            foreach (var pool in _pools.Values)
            {
                pool.Clear();
            }

            _pools.Clear();
        }

    }
}
