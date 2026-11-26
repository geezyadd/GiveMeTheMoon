using System;
using Features.ExplosionModule.Network;
using Game.Connection;
using Zenject;

namespace Features.GameCoreModule.Scripts {
    // Registers the explosion channel's client handler whenever a client starts: Mirror clears client handlers when a
    // client stops.
    public sealed class ExplosionNetworkChannelSystem : IInitializable, IDisposable {
        private readonly ConnectionNetworkEvents _connectionNetworkEvents;
        private readonly IExplosionNetworkChannel _explosionNetworkChannel;

        public ExplosionNetworkChannelSystem(ConnectionNetworkEvents connectionNetworkEvents, IExplosionNetworkChannel explosionNetworkChannel) {
            _connectionNetworkEvents = connectionNetworkEvents;
            _explosionNetworkChannel = explosionNetworkChannel;
        }

        public void Initialize() =>
            _connectionNetworkEvents.ClientStarted += OnClientStarted;

        public void Dispose() =>
            _connectionNetworkEvents.ClientStarted -= OnClientStarted;

        private void OnClientStarted() =>
            _explosionNetworkChannel.RegisterForClient();
    }
}
