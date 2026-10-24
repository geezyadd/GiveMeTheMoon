#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host: F8 puts a weapon test dummy in front of the ship (or takes it away again), so a turret
    // has something to hit before the field rocks take weapon hits.
    public sealed class WeaponDebugTargetSystem : ITickable, IWeaponDebugTargets {
        // Ahead of the ship, above the deck and a little to the left, where the turret socket stands.
        private static readonly Vector3 _shipOffset = new Vector3(-2.6f, 7f, 24f);

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly ShipStationCatalog _shipStationCatalog;

        public WeaponDebugTarget Target { get; private set; }

        public WeaponDebugTargetSystem(IShipRunBindingModel shipRunBindingModel, ShipStationCatalog shipStationCatalog) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipStationCatalog = shipStationCatalog;
        }

        public void Tick() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || NetworkServer.active == false || keyboard.f8Key.wasPressedThisFrame == false)
                return;

            if (Target != null)
                DebugDespawnTarget();
            else
                DebugSpawnTarget();
        }

        public WeaponDebugTarget DebugSpawnTarget() {
            DebugDespawnTarget();
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return null;

            GameObject spawned = Object.Instantiate(_shipStationCatalog.WeaponDebugTargetPrefab, ship.transform.TransformPoint(_shipOffset), ship.transform.rotation);
            NetworkServer.Spawn(spawned);
            Target = spawned.GetComponent<WeaponDebugTarget>();
            Target.ServerPlace(ship.PoseSync.netIdentity, _shipOffset);
            return Target;
        }

        public void DebugDespawnTarget() {
            if (Target != null)
                NetworkServer.Destroy(Target.gameObject);

            Target = null;
        }
    }
}
#endif
