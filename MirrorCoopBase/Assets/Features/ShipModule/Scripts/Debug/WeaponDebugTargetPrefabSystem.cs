#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Game.Connection;
using Mirror;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Registers the weapon test dummy with the Mirror client on every client start (Mirror clears its prefabs on
    // shutdown), so it never ships in the network manager's spawn prefabs.
    public sealed class WeaponDebugTargetPrefabSystem : IInitializable, IDisposable {
        private readonly ConnectionNetworkEvents _connectionNetworkEvents;
        private readonly WeaponDebugTargetConfiguration _weaponDebugTargetConfiguration;

        public WeaponDebugTargetPrefabSystem(ConnectionNetworkEvents connectionNetworkEvents, WeaponDebugTargetConfiguration weaponDebugTargetConfiguration) {
            _connectionNetworkEvents = connectionNetworkEvents;
            _weaponDebugTargetConfiguration = weaponDebugTargetConfiguration;
        }

        public void Initialize() =>
            _connectionNetworkEvents.ClientStarted += OnClientStarted;

        public void Dispose() =>
            _connectionNetworkEvents.ClientStarted -= OnClientStarted;

        private void OnClientStarted() {
            // A prefab asset is never awoken, so its netIdentity is not set yet.
            NetworkIdentity identity = _weaponDebugTargetConfiguration.TargetPrefab.GetComponent<NetworkIdentity>();
            if (NetworkClient.prefabs.ContainsKey(identity.assetId) == false)
                NetworkClient.RegisterPrefab(identity.gameObject);
        }
    }
}
#endif
