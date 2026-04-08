using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Game.Framework.SceneManagement
{
    public sealed class GameSceneManager : MonoBehaviour
    {
        private const string InstanceName = "[GameSceneManager]";

        private Coroutine loadRoutine;

        public static GameSceneManager Instance { get; private set; }

        public bool IsLoading => loadRoutine != null;
        public float Progress { get; private set; }
        public SceneId? CurrentSceneId { get; private set; }

        public event Action<SceneId> LoadStarted;
        public event Action<SceneId> LoadCompleted;
        public event Action<SceneId, float> LoadProgressChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null)
            {
                return;
            }

            var gameObject = new GameObject(InstanceName);
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<GameSceneManager>();
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
            RefreshCurrentSceneId();
        }

        public bool TryLoad(SceneId sceneId, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"Scene load ignored. Already loading scene: {SceneNames.GetName(sceneId)}");
                return false;
            }

            loadRoutine = StartCoroutine(LoadRoutine(sceneId, loadSceneMode));
            return true;
        }

        public Coroutine Load(SceneId sceneId, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
        {
            if (!TryLoad(sceneId, loadSceneMode))
            {
                return null;
            }

            return loadRoutine;
        }

        public bool TryReloadCurrent()
        {
            RefreshCurrentSceneId();

            if (CurrentSceneId == null)
            {
                Debug.LogWarning($"Scene reload failed. Active scene is not registered: {UnitySceneManager.GetActiveScene().name}");
                return false;
            }

            return TryLoad(CurrentSceneId.Value);
        }

        public bool TryUnloadAdditive(SceneId sceneId)
        {
            if (IsLoading)
            {
                Debug.LogWarning($"Scene unload ignored. Already loading scene: {SceneNames.GetName(sceneId)}");
                return false;
            }

            var sceneName = SceneNames.GetName(sceneId);
            var scene = UnitySceneManager.GetSceneByName(sceneName);

            if (!scene.isLoaded)
            {
                Debug.LogWarning($"Scene unload ignored. Scene is not loaded: {sceneName}");
                return false;
            }

            loadRoutine = StartCoroutine(UnloadRoutine(sceneId));
            return true;
        }

        private IEnumerator LoadRoutine(SceneId sceneId, LoadSceneMode loadSceneMode)
        {
            var sceneName = SceneNames.GetName(sceneId);
            Progress = 0f;
            LoadStarted?.Invoke(sceneId);
            LoadProgressChanged?.Invoke(sceneId, Progress);

            var operation = UnitySceneManager.LoadSceneAsync(sceneName, loadSceneMode);

            if (operation == null)
            {
                Debug.LogError($"Scene load failed. Scene is not registered in build settings: {sceneName}");
                FinishLoading(sceneId);
                yield break;
            }

            while (!operation.isDone)
            {
                Progress = Mathf.Clamp01(operation.progress / 0.9f);
                LoadProgressChanged?.Invoke(sceneId, Progress);
                yield return null;
            }

            Progress = 1f;
            RefreshCurrentSceneId();
            LoadProgressChanged?.Invoke(sceneId, Progress);
            FinishLoading(sceneId);
        }

        private IEnumerator UnloadRoutine(SceneId sceneId)
        {
            var sceneName = SceneNames.GetName(sceneId);
            Progress = 0f;

            var operation = UnitySceneManager.UnloadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogError($"Scene unload failed: {sceneName}");
                FinishLoading(sceneId);
                yield break;
            }

            while (!operation.isDone)
            {
                Progress = Mathf.Clamp01(operation.progress / 0.9f);
                LoadProgressChanged?.Invoke(sceneId, Progress);
                yield return null;
            }

            Progress = 1f;
            RefreshCurrentSceneId();
            LoadProgressChanged?.Invoke(sceneId, Progress);
            FinishLoading(sceneId);
        }

        private void FinishLoading(SceneId sceneId)
        {
            loadRoutine = null;
            LoadCompleted?.Invoke(sceneId);
        }

        private void RefreshCurrentSceneId()
        {
            var activeSceneName = UnitySceneManager.GetActiveScene().name;
            CurrentSceneId = SceneNames.TryGetId(activeSceneName, out var sceneId) ? sceneId : null;
        }
    }
}
