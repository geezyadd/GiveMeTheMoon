using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Game.Connection
{
    // Loads the menu and unloads every session scene except the menu and the persistent scene.
    internal sealed class ConnectionMenuReturn
    {
        private readonly ConnectionSceneLoading _sceneLoading;
        private readonly ConnectionSceneNames _sceneNames;

        public ConnectionMenuReturn(ConnectionSceneLoading sceneLoading, ConnectionSceneNames sceneNames)
        {
            _sceneLoading = sceneLoading;
            _sceneNames = sceneNames;
        }

        public async Task ReturnAsync()
        {
            await _sceneLoading.LoadAndActivateAsync(_sceneNames.Menu);
            await _sceneLoading.UnloadIfLoadedAsync(_sceneNames.Lobby);
            await _sceneLoading.UnloadIfLoadedAsync(_sceneNames.Game);

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() == false)
                    continue;
                if (scene.name == _sceneNames.Menu || scene.name == _sceneNames.Persistent || scene.name == "DontDestroyOnLoad")
                    continue;

                await _sceneLoading.UnloadIfLoadedAsync(scene.name);
            }
        }
    }
}
