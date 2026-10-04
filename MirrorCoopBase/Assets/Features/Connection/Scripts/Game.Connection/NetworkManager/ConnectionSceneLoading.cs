using System.Collections;
using System.Threading.Tasks;
using Features.SceneLoaderModule.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Connection
{
    // Addressable scene load and unload, as tasks and as coroutines for the network message handlers.
    internal sealed class ConnectionSceneLoading
    {
        private readonly ISceneLoaderService _sceneLoader;

        public ConnectionSceneLoading(ISceneLoaderService sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public async Task LoadAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            if (_sceneLoader == null)
            {
                Debug.LogError("ConnectionSceneLoading: ISceneLoaderService is missing.");
                return;
            }

            await _sceneLoader.LoadSceneAsync(sceneName, false);
        }

        public async Task LoadAndActivateAsync(string sceneName)
        {
            await LoadAsync(sceneName);

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid())
                SceneManager.SetActiveScene(scene);
        }

        public async Task UnloadIfLoadedAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || _sceneLoader == null)
                return;

            await _sceneLoader.UnloadSceneAsync(sceneName);
        }

        public IEnumerator Load(string sceneName)
        {
            yield return AwaitTask(LoadAsync(sceneName));
        }

        public IEnumerator UnloadIfLoaded(string sceneName)
        {
            yield return AwaitTask(UnloadIfLoadedAsync(sceneName));
        }

        static IEnumerator AwaitTask(Task task)
        {
            while (task != null && task.IsCompleted == false)
                yield return null;

            if (task is { IsFaulted: true })
                Debug.LogException(task.Exception?.InnerException ?? task.Exception);
        }
    }
}
