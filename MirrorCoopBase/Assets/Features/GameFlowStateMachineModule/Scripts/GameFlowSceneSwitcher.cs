using System;
using System.Threading.Tasks;
using Features.SceneLoaderModule.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.GameFlowStateMachineModule.Scripts {
    public sealed class GameFlowSceneSwitcher {
        private readonly ISceneLoaderService _sceneLoaderService;
        private readonly GameFlowStateSceneMapper _sceneMapper;

        public GameFlowSceneSwitcher(
            ISceneLoaderService sceneLoaderService,
            GameFlowStateSceneMapper sceneMapper) {
            _sceneLoaderService = sceneLoaderService;
            _sceneMapper = sceneMapper;
        }

        public async Task SwitchAsync(Type previousStateType, Type nextStateType) {
            if (_sceneMapper.TryGetScene(previousStateType, out string sceneToUnload))
                await _sceneLoaderService.UnloadSceneAsync(sceneToUnload);

            if (_sceneMapper.TryGetScene(nextStateType, out string sceneToLoad) == false)
                return;

            await _sceneLoaderService.LoadSceneAsync(sceneToLoad, false);

            Scene scene = SceneManager.GetSceneByName(sceneToLoad);
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.SetActiveScene(scene);
        }
    }
}
