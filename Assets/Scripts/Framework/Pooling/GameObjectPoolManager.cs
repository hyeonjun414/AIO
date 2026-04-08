using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Framework.ResourceManagement;
using UnityEngine;

namespace Game.Framework.Pooling
{
    public sealed class GameObjectPoolManager : MonoBehaviour
    {
        private const string InstanceName = "[GameObjectPoolManager]";

        private readonly Dictionary<ResourceKey, PoolState> pools = new Dictionary<ResourceKey, PoolState>();
        private readonly Dictionary<GameObject, PoolState> instanceToPool = new Dictionary<GameObject, PoolState>();

        public static GameObjectPoolManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null)
            {
                return;
            }

            var gameObject = new GameObject(InstanceName);
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<GameObjectPoolManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task PrewarmAsync(ResourceKey key, int count)
        {
            if (count <= 0)
            {
                return;
            }

            var pool = GetOrCreatePool(key);

            for (var i = 0; i < count; i++)
            {
                var instance = await GameResourceManager.Instance.InstantiateAsync(key, pool.Root);
                instance.SetActive(false);
                pool.Inactive.Enqueue(instance);
                instanceToPool[instance] = pool;
            }
        }

        public async Task<GameObject> SpawnAsync(ResourceKey key, Transform parent = null)
        {
            var instance = await SpawnAsync(key, Vector3.zero, Quaternion.identity, parent);
            return instance;
        }

        public async Task<GameObject> SpawnAsync(ResourceKey key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var pool = GetOrCreatePool(key);
            GameObject instance;

            if (pool.Inactive.Count > 0)
            {
                instance = pool.Inactive.Dequeue();
            }
            else
            {
                instance = await GameResourceManager.Instance.InstantiateAsync(key, pool.Root);
                instanceToPool[instance] = pool;
            }

            pool.Active.Add(instance);
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public bool Despawn(GameObject instance)
        {
            if (instance == null || !instanceToPool.TryGetValue(instance, out var pool))
            {
                return false;
            }

            if (!pool.Active.Remove(instance))
            {
                return false;
            }

            instance.SetActive(false);
            instance.transform.SetParent(pool.Root, false);
            pool.Inactive.Enqueue(instance);
            return true;
        }

        public bool ReleasePool(ResourceKey key)
        {
            if (!pools.Remove(key, out var pool))
            {
                return false;
            }

            foreach (var instance in pool.Active)
            {
                instanceToPool.Remove(instance);
                GameResourceManager.Instance.ReleaseInstance(instance);
            }

            while (pool.Inactive.Count > 0)
            {
                var instance = pool.Inactive.Dequeue();
                instanceToPool.Remove(instance);
                GameResourceManager.Instance.ReleaseInstance(instance);
            }

            Destroy(pool.Root.gameObject);
            return true;
        }

        public void ReleaseAll()
        {
            var keys = new List<ResourceKey>(pools.Keys);
            foreach (var key in keys)
            {
                ReleasePool(key);
            }
        }

        private PoolState GetOrCreatePool(ResourceKey key)
        {
            if (pools.TryGetValue(key, out var pool))
            {
                return pool;
            }

            var root = new GameObject($"Pool - {key.Value}").transform;
            root.SetParent(transform, false);

            pool = new PoolState(key, root);
            pools.Add(key, pool);
            return pool;
        }

        private sealed class PoolState
        {
            public PoolState(ResourceKey key, Transform root)
            {
                Key = key;
                Root = root;
            }

            public ResourceKey Key { get; }
            public Transform Root { get; }
            public Queue<GameObject> Inactive { get; } = new Queue<GameObject>();
            public HashSet<GameObject> Active { get; } = new HashSet<GameObject>();
        }
    }
}
