using Features.MvpModule;
using Game.Connection;
using Mirror;

namespace Features.LobbyModule.Scripts {
    public sealed class StartGamePresenter : PresenterBehaviour<StartGameViewBase> {
        private readonly IConnectionSessionService _connectionSession;

        public StartGamePresenter(IConnectionSessionService connectionSession) {
            _connectionSession = connectionSession;
        }

        protected override void OnViewSet() {
            bool isHost = NetworkServer.active;
            View.SetVisible(isHost);
            View.SetInteractable(isHost);
            View.OnStartClicked += OnStartClicked;
            _connectionSession.OnMapReady += OnMapReady;
        }

        protected override void OnDisposed() {
            View.OnStartClicked -= OnStartClicked;
            _connectionSession.OnMapReady -= OnMapReady;
        }

        // Back in the lobby after a run (ReturnToLobby): the host can start the next one.
        void OnMapReady() {
            bool canStart = NetworkServer.active && _connectionSession.IsInLobby;
            View.SetVisible(canStart);
            View.SetInteractable(canStart);
        }

        void OnStartClicked() {
            if (NetworkServer.active == false)
                return;

            View.SetInteractable(false);
            View.SetVisible(false);
            _connectionSession.StartGame();
        }
    }
}
