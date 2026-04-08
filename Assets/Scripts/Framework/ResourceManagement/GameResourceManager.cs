using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Framework.ResourceManagement
{
    public sealed class GameResourceManager : MonoBehaviour
    {
        private const string InstanceName = "[GameResourceManager]";

        private readonly Dictionary<ResourceKey, CachedAsset> cachedAssets = new Dictionary<ResourceKey, CachedAsset>();
        private readonly Dictionary<string, CachedLabelAssets> cachedLabels = new Dictionary<string, CachedLabelAssets>();
        private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> instanceHandles = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();

        private AsyncOperationHandle initializationHandle;
        private Task initializationTask;

        public static GameResourceManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null)
            {
                return;
            }

            var gameObject = new GameObject(InstanceName);
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<GameResourceManager>();
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

        public Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return Task.CompletedTask;
            }

            if (initializationTask != null)
            {
                return initializationTask;
            }

            initializationHandle = Addressables.InitializeAsync();
            initializationTask = AwaitInitializationAsync(initializationHandle);
            return initializationTask;
        }

        public async Task<T> LoadAsync<T>(ResourceKey key) where T : UnityEngine.Object
        {
            await InitializeAsync();

            if (cachedAssets.TryGetValue(key, out var cachedAsset))
            {
                cachedAsset.Retain();
                await cachedAsset.Handle.Task;
                return GetCachedResult<T>(key, cachedAsset.Handle);
            }

            var handle = Addressables.LoadAssetAsync<T>(key.Value);
            var cacheEntry = new CachedAsset(handle);
            cachedAssets.Add(key, cacheEntry);

            try
            {
                await handle.Task;
            }
            catch
            {
                cachedAssets.Remove(key);
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                cachedAssets.Remove(key);
                Addressables.Release(handle);
                throw new InvalidOperationException($"Addressable load failed: {key.Value}");
            }

            return handle.Result;
        }

        public async Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Label cannot be null or empty.", nameof(label));
            }

            await InitializeAsync();

            var cacheKey = $"{typeof(T).FullName}:{label}";
            if (cachedLabels.TryGetValue(cacheKey, out var cachedLabel))
            {
                cachedLabel.Retain();
                await cachedLabel.Handle.Task;
                return GetCachedLabelResult<T>(label, cachedLabel.Handle);
            }

            var handle = Addressables.LoadAssetsAsync<T>(label, null);
            var cacheEntry = new CachedLabelAssets(handle);
            cachedLabels.Add(cacheKey, cacheEntry);

            try
            {
                await handle.Task;
            }
            catch
            {
                cachedLabels.Remove(cacheKey);
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                cachedLabels.Remove(cacheKey);
                Addressables.Release(handle);
                throw new InvalidOperationException($"Addressables label load failed: {label}");
            }

            return handle.Result;
        }

        public async Task<GameObject> InstantiateAsync(ResourceKey key, Transform parent = null)
        {
            await InitializeAsync();

            var handle = Addressables.InstantiateAsync(key.Value, parent);

            try
            {
                await handle.Task;
            }
            catch
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw;
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(handle);
                throw new InvalidOperationException($"Addressable instantiate failed: {key.Value}");
            }

            instanceHandles[handle.Result] = handle;
            return handle.Result;
        }

        public bool Release(ResourceKey key)
        {
            if (!cachedAssets.TryGetValue(key, out var cachedAsset))
            {
                return false;
            }

            if (cachedAsset.Release() > 0)
            {
                return true;
            }

            cachedAssets.Remove(key);
            if (cachedAsset.Handle.IsValid())
            {
                Addressables.Release(cachedAsset.Handle);
            }

            return true;
        }

        public bool ReleaseLabel<T>(string label) where T : UnityEngine.Object
        {
            var cacheKey = $"{typeof(T).FullName}:{label}";
            if (!cachedLabels.TryGetValue(cacheKey, out var cachedLabel))
            {
                return false;
            }

            if (cachedLabel.Release() > 0)
            {
                return true;
            }

            cachedLabels.Remove(cacheKey);
            if (cachedLabel.Handle.IsValid())
            {
                Addressables.Release(cachedLabel.Handle);
            }

            return true;
        }

        public bool ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                return false;
            }

            if (!instanceHandles.Remove(instance, out var handle))
            {
                return Addressables.ReleaseInstance(instance);
            }

            if (handle.IsValid())
            {
                Addressables.Release(handle);
                return true;
            }

            return false;
        }

        public void ReleaseAll()
        {
            foreach (var cachedAsset in cachedAssets.Values)
            {
                if (cachedAsset.Handle.IsValid())
                {
                    Addressables.Release(cachedAsset.Handle);
                }
            }

            cachedAssets.Clear();

            foreach (var cachedLabel in cachedLabels.Values)
            {
                if (cachedLabel.Handle.IsValid())
                {
                    Addressables.Release(cachedLabel.Handle);
                }
            }

            cachedLabels.Clear();

            foreach (var handle in instanceHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            instanceHandles.Clear();
        }

        private static async Task AwaitInitializationAsync(AsyncOperationHandle handle)
        {
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException("Addressables initialization failed.");
            }

            IsCurrentInstanceInitialized();
        }

        private static void IsCurrentInstanceInitialized()
        {
            if (Instance != null)
            {
                Instance.IsInitialized = true;
            }
        }

        private static T GetCachedResult<T>(ResourceKey key, AsyncOperationHandle handle) where T : UnityEngine.Object
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"Cached addressable load failed: {key.Value}");
            }

            if (handle.Result is T result)
            {
                return result;
            }

            throw new InvalidCastException($"Cached addressable type mismatch. Key: {key.Value}, Type: {typeof(T).Name}");
        }

        private static IList<T> GetCachedLabelResult<T>(string label, AsyncOperationHandle handle) where T : UnityEngine.Object
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"Cached addressables label load failed: {label}");
            }

            if (handle.Result is IList<T> result)
            {
                return result;
            }

            throw new InvalidCastException($"Cached addressables label type mismatch. Label: {label}, Type: {typeof(T).Name}");
        }

        private sealed class CachedAsset
        {
            public CachedAsset(AsyncOperationHandle handle)
            {
                Handle = handle;
                ReferenceCount = 1;
            }

            public AsyncOperationHandle Handle { get; }
            public int ReferenceCount { get; private set; }

            public void Retain()
            {
                ReferenceCount++;
            }

            public int Release()
            {
                ReferenceCount = Math.Max(0, ReferenceCount - 1);
                return ReferenceCount;
            }
        }

        private sealed class CachedLabelAssets
        {
            public CachedLabelAssets(AsyncOperationHandle handle)
            {
                Handle = handle;
                ReferenceCount = 1;
            }

            public AsyncOperationHandle Handle { get; }
            public int ReferenceCount { get; private set; }

            public void Retain()
            {
                ReferenceCount++;
            }

            public int Release()
            {
                ReferenceCount = Math.Max(0, ReferenceCount - 1);
                return ReferenceCount;
            }
        }
    }
}
