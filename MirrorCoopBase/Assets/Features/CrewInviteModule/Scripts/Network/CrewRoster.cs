using Features.CrewInviteModule.Data.Generated;
using Game.Connection;
using Mirror;
using Zenject;

namespace Features.CrewInviteModule.Scripts.Network {
    public sealed class CrewRoster : CrewRosterBridge {
        private IConnectionJoinPolicy _connectionJoinPolicy;

        [Inject]
        private void InjectDependencies(IConnectionJoinPolicy connectionJoinPolicy) =>
            _connectionJoinPolicy = connectionJoinPolicy;

        public override void OnStartServer() {
            base.OnStartServer();
            _connectionJoinPolicy.OnJoinStateChanged += OnJoinStateChanged;
            WriteRoster();
        }

        public override void OnStopServer() {
            _connectionJoinPolicy.OnJoinStateChanged -= OnJoinStateChanged;
            base.OnStopServer();
        }

        [Server]
        private void WriteRoster() =>
            ServerSetState(new CrewRosterState {
                PlayerCount = _connectionJoinPolicy.PlayerCount,
                PlayerSlots = _connectionJoinPolicy.PlayerSlots,
                IsJoinWindowOpen = _connectionJoinPolicy.IsJoinWindowOpen
            });

        private void OnJoinStateChanged() =>
            WriteRoster();
    }
}
