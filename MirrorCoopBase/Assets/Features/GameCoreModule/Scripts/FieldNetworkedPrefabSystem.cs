using System;
using Features.FlightFieldModule.Scripts.Network;
using Game.Connection;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    // Registers the flight field's networked prefabs whenever a client starts: Mirror has registered the network
    // manager's spawn prefabs by then and no spawn message has arrived yet.
    public sealed class FieldNetworkedPrefabSystem : IInitializable, IDisposable {
        private readonly ConnectionNetworkEvents _connectionNetworkEvents;
        private readonly IFieldNetworkedPrefabRegistration _fieldNetworkedPrefabRegistration;

        public FieldNetworkedPrefabSystem(ConnectionNetworkEvents connectionNetworkEvents, IFieldNetworkedPrefabRegistration fieldNetworkedPrefabRegistration) {
            _connectionNetworkEvents = connectionNetworkEvents;
            _fieldNetworkedPrefabRegistration = fieldNetworkedPrefabRegistration;
        }

        public void Initialize() =>
            _connectionNetworkEvents.ClientStarted += OnClientStarted;

        public void Dispose() =>
            _connectionNetworkEvents.ClientStarted -= OnClientStarted;

        private void OnClientStarted() =>
            _fieldNetworkedPrefabRegistration.RegisterForClient();
    }
}
