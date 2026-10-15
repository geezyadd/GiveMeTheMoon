#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.GameCoreModule.Contracts;
using Features.GameFlowStateMachineModule.Scripts;
using Features.GameFlowStateMachineModule.Scripts.States;
using Features.LobbyModule.Scripts;
using Features.MenuModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // The host's way into and out of a game: menu -> Host -> lobby -> Start -> GameScene, back to the lobby when all
    // die, and Leave to the menu. Every wait records the run phases it passes through.
    public sealed class LoopSmokeSession {
        private const float MENU_TIMEOUT_SECONDS = 60f;
        private const float LOBBY_TIMEOUT_SECONDS = 30f;
        private const float GAME_TIMEOUT_SECONDS = 60f;
        private const float LEAVE_TIMEOUT_SECONDS = 30f;
        // Covers PlayerLifeConfiguration.AllDeadReturnDelay plus the lobby load and the GameScene unload.
        private const float RETURN_TO_LOBBY_TIMEOUT_SECONDS = 30f;

        private readonly List<ShipRunPhase> _phases = new List<ShipRunPhase>();
        private readonly LoopSmokeErrorLog _errorLog;

        public ShipBase Ship { get; private set; }
        public IReadOnlyShipRunModel Model { get; private set; }
        public IReadOnlyList<ShipRunPhase> Phases =>
            _phases;

        public LoopSmokeSession(LoopSmokeErrorLog errorLog) =>
            _errorLog = errorLog;

        public IEnumerator EnterLobbyAsHostCoroutine() {
            SceneManager.LoadScene(SceneNames.Bootstrap);

            yield return WaitForCoroutine(IsMenuReady, MENU_TIMEOUT_SECONDS, "the menu with a bound Host button");
            Object.FindAnyObjectByType<MenuSessionViewBase>().OnHostClicked.Invoke();

            yield return WaitForCoroutine(IsLobbyReady, LOBBY_TIMEOUT_SECONDS, "the lobby with an authenticated host client and a bound Start button");
        }

        public IEnumerator StartGameCoroutine() {
            Object.FindAnyObjectByType<StartGameViewBase>().OnStartClicked.Invoke();
            yield return WaitForCoroutine(IsGameReady, GAME_TIMEOUT_SECONDS, "GameScene with the ship, the local player and the Build phase");
        }

        // The real way back: PlayerLifeSystem's all-dead rule sends everyone to the lobby.
        public IEnumerator ReturnToLobbyByAllDeadCoroutine() {
            NetworkClient.localPlayer.GetComponent<IDamageable>().ServerKill(DamageType.Generic);
            yield return WaitForCoroutine(IsBackInLobby, RETURN_TO_LOBBY_TIMEOUT_SECONDS, "the lobby after everyone died, with GameScene unloaded");
            Ship = null;
            Model = null;
        }

        public IEnumerator LeaveToMenuCoroutine() {
            Object.FindAnyObjectByType<LeaveSessionViewBase>().OnLeaveClicked.Invoke();
            yield return WaitForCoroutine(IsBackInMenu, LEAVE_TIMEOUT_SECONDS, "the menu after Leave");
        }

        public IEnumerator WaitForCoroutine(Func<bool> condition, float timeoutSeconds, string target) {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (true) {
                RecordPhase();
                if (condition())
                    yield break;

                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {timeoutSeconds} s waiting for {target}. Phases: {string.Join(" -> ", _phases)}. Errors:\n{_errorLog.Describe()}");

                yield return null;
            }
        }

        public void StartPhaseRecord() {
            _phases.Clear();
            RecordPhase();
        }

        private void RecordPhase() {
            if (Model == null)
                return;

            if (_phases.Count == 0 || _phases[_phases.Count - 1] != Model.Phase)
                _phases.Add(Model.Phase);
        }

        private static bool IsMenuReady() {
            MenuSessionViewBase menu = Object.FindAnyObjectByType<MenuSessionViewBase>();
            return menu != null && menu.OnHostClicked != null;
        }

        // Start sends a scene change to every client; the host's own client drops it until it is authenticated.
        private static bool IsLobbyReady() {
            StartGameViewBase start = Object.FindAnyObjectByType<StartGameViewBase>();
            bool isHostClientReady = NetworkServer.active && NetworkClient.ready && NetworkClient.connection.isAuthenticated;
            return isHostClientReady && start != null && start.OnStartClicked != null;
        }

        private bool IsGameReady() {
            if (NetworkClient.localPlayer == null || Object.FindAnyObjectByType<ShipRunDirector>() == null)
                return false;

            ConnectionNetworkManager manager = ResolveNetworkManager();
            if (manager != null && manager.IsMapLoaded == false)
                return false;

            Ship = Object.FindAnyObjectByType<ShipBase>();
            Model = ProjectContext.Instance.Container.Resolve<IReadOnlyShipRunModel>();
            return Ship != null && Model.Phase == ShipRunPhase.Build;
        }

        private static bool IsBackInLobby() {
            ConnectionNetworkManager manager = ResolveNetworkManager();
            if (manager == null || manager.IsMapLoaded == false || manager.IsChangingMap)
                return false;

            Scene game = SceneManager.GetSceneByName(manager.GameSceneName);
            bool isGameUnloaded = game.IsValid() == false || game.isLoaded == false;
            return isGameUnloaded && SceneManager.GetActiveScene().name == manager.LobbySceneName && NetworkClient.localPlayer != null;
        }

        private static bool IsBackInMenu() {
            if (NetworkServer.active || NetworkClient.active || IsMenuReady() == false)
                return false;

            IGameFlowStateMachineService flow = LoopSmokeSceneContexts.Resolve<IGameFlowStateMachineService>();
            return flow.IsTransitioning == false && flow.CurrentStateType == typeof(MenuGameFlowState);
        }

        private static ConnectionNetworkManager ResolveNetworkManager() =>
            ProjectContext.Instance.Container.Resolve<ConnectionSessionModel>().NetworkManager;
    }
}
#endif
