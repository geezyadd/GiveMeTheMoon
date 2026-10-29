using System;
using Features.ParcelModule.Scripts.Configuration;
using Game.Connection;
using Mirror;
using Zenject;

namespace Features.ParcelModule.Scripts.Systems {
    // Registers the parcel crate and the delivery stand with the Mirror client on every client start (Mirror clears its
    // prefabs on shutdown), so they come from the parcel configuration, not from the network manager's spawn prefabs in
    // the scene.
    public sealed class ParcelPrefabSystem : IInitializable, IDisposable {
        private readonly ConnectionNetworkEvents _connectionNetworkEvents;
        private readonly ParcelConfiguration _parcelConfiguration;

        public ParcelPrefabSystem(ConnectionNetworkEvents connectionNetworkEvents, ParcelConfiguration parcelConfiguration) {
            _connectionNetworkEvents = connectionNetworkEvents;
            _parcelConfiguration = parcelConfiguration;
        }

        public void Initialize() =>
            _connectionNetworkEvents.ClientStarted += OnClientStarted;

        public void Dispose() =>
            _connectionNetworkEvents.ClientStarted -= OnClientStarted;

        private void OnClientStarted() {
            Register(_parcelConfiguration.CratePrefab.Identity);
            Register(_parcelConfiguration.StandPrefab.Identity);
        }

        // A prefab asset is never awoken: its identity comes from the serialized reference, not netIdentity.
        private static void Register(NetworkIdentity identity) {
            if (NetworkClient.prefabs.ContainsKey(identity.assetId) == false)
                NetworkClient.RegisterPrefab(identity.gameObject);
        }
    }
}
