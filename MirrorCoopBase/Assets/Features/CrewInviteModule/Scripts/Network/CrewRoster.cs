using Features.CrewInviteModule.Scripts.Generated;
using Features.GameCoreModule.Contracts;
using Game.Connection;
using Mirror;
using Zenject;

namespace Features.CrewInviteModule.Scripts {
    // Server: keeps the shared crew size and join window up to date for every player's invite terminal.
    public sealed class CrewRoster : CrewRosterBridge {
        private IConnectionJoinPolicy _connectionJoinPolicy;
        private ISessionJoinGate _sessionJoinGate;
        private ConnectionNetworkEvents _connectionNetworkEvents;

        [Inject]
        private void InjectDependencies(
            IConnectionJoinPolicy connectionJoinPolicy,
            ISessionJoinGate sessionJoinGate,
            ConnectionNetworkEvents connectionNetworkEvents) {
            _connectionJoinPolicy = connectionJoinPolicy;
            _sessionJoinGate = sessionJoinGate;
            _connectionNetworkEvents = connectionNetworkEvents;
        }

        public override void OnStartServer() {
            base.OnStartServer();
            _sessionJoinGate.OnWindowChanged += OnSessionChanged;
            _connectionNetworkEvents.MapLoadStarted += OnSessionChanged;
            _connectionNetworkEvents.MapReady += OnSessionChanged;
            _connectionNetworkEvents.ServerPlayersChanged += OnSessionChanged;
            WriteRoster();
        }

        public override void OnStopServer() {
            _sessionJoinGate.OnWindowChanged -= OnSessionChanged;
            _connectionNetworkEvents.MapLoadStarted -= OnSessionChanged;
            _connectionNetworkEvents.MapReady -= OnSessionChanged;
            _connectionNetworkEvents.ServerPlayersChanged -= OnSessionChanged;
            base.OnStopServer();
        }

        [Server]
        private void WriteRoster() =>
            ServerSetState(new CrewRosterState {
                PlayerCount = _connectionJoinPolicy.PlayerCount,
                PlayerSlots = _connectionJoinPolicy.PlayerSlots,
                IsJoinWindowOpen = _connectionJoinPolicy.IsJoinWindowOpen
            });

        private void OnSessionChanged() =>
            WriteRoster();
    }
}
