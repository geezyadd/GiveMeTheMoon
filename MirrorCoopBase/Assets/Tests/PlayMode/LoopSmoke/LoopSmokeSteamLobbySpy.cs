#if UNITY_INCLUDE_TESTS
using System;
using System.Threading.Tasks;
using Game.Connection;
using UnityEngine.SceneManagement;
using Zenject;

namespace Tests.PlayMode.LoopSmoke {
    // Stands in for the Steam lobby of a session without Steam, the way the test always runs, and counts the invite
    // overlay requests so a press on an inactive terminal can be checked to do nothing. ISteamLobbyService is bound in a
    // scene context, so the binding is replaced when the scene that installs it loads.
    public sealed class LoopSmokeSteamLobbySpy : ISteamLobbyService, IDisposable {
        public int InviteOverlayRequests { get; private set; }
        public bool IsBound { get; private set; }

        public bool IsAvailable => false;
        public ulong? CurrentLobbyId => null;
        public ulong HostSteamId => throw new InvalidOperationException("The smoke test runs without a Steam lobby.");
        public bool IsOverlayEnabled => false;

        public event Action<ulong> JoinRequested {
            add { }
            remove { }
        }

        public event Action OnInviteStateChanged {
            add { }
            remove { }
        }

        public void BindIntoSceneContexts() {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ArmSceneContextHook();
        }

        public void Dispose() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (SceneContext.ExtraBindingsLateInstallMethod == OnLateInstall)
                SceneContext.ExtraBindingsLateInstallMethod = null;
        }

        public Task CreateFriendsLobbyAsync(int maxPlayers) =>
            throw new InvalidOperationException("The smoke test hosts without Steam.");

        public Task JoinLobbyAsync(ulong lobbyId) =>
            throw new InvalidOperationException("The smoke test joins without Steam.");

        public void LeaveLobby() {
        }

        public void SetLobbyJoinable(bool joinable) {
        }

        public void OpenInviteOverlay() =>
            InviteOverlayRequests++;

        // Zenject clears the hook after every scene context: it is armed again until the binding scene has loaded.
        private void ArmSceneContextHook() =>
            SceneContext.ExtraBindingsLateInstallMethod = OnLateInstall;

        private void OnLateInstall(DiContainer container) {
            if (container.HasBindingId(typeof(ISteamLobbyService), null, InjectSources.Local) == false)
                return;

            container.Unbind<ISteamLobbyService>();
            container.Bind<ISteamLobbyService>().FromInstance(this);
            IsBound = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (IsBound == false)
                ArmSceneContextHook();
        }
    }
}
#endif
