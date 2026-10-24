#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host: F8 puts a weapon test dummy in front of the ship (or takes it away again), so a turret
    // has something to hit before the field rocks take weapon hits.
    public sealed class WeaponDebugTargetSystem : ITickable, IWeaponDebugTargets {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly WeaponDebugTargetConfiguration _weaponDebugTargetConfiguration;
        private readonly WeaponDebugTargetModel _weaponDebugTargetModel;

        public WeaponDebugTargetSystem(
            IShipRunBindingModel shipRunBindingModel,
            WeaponDebugTargetConfiguration weaponDebugTargetConfiguration,
            WeaponDebugTargetModel weaponDebugTargetModel) {
            _shipRunBindingModel = shipRunBindingModel;
            _weaponDebugTargetConfiguration = weaponDebugTargetConfiguration;
            _weaponDebugTargetModel = weaponDebugTargetModel;
        }

        public void Tick() {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || NetworkServer.active == false || keyboard.f8Key.wasPressedThisFrame == false)
                return;

            if (_weaponDebugTargetModel.Target != null)
                DebugDespawnTarget();
            else
                DebugSpawnTarget();
        }

        public WeaponDebugTarget DebugSpawnTarget() {
            DebugDespawnTarget();
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null)
                return null;

            Vector3 offset = _weaponDebugTargetConfiguration.ShipOffset;
            WeaponDebugTarget target = Object.Instantiate(_weaponDebugTargetConfiguration.TargetPrefab, ship.transform.TransformPoint(offset), ship.transform.rotation);
            NetworkServer.Spawn(target.gameObject);
            target.ServerPlace(ship.PoseSync.netIdentity, offset);
            _weaponDebugTargetModel.SetTarget(target);
            return target;
        }

        public void DebugDespawnTarget() {
            WeaponDebugTarget target = _weaponDebugTargetModel.Target;
            if (target != null)
                NetworkServer.Destroy(target.gameObject);

            _weaponDebugTargetModel.ClearTarget();
        }
    }
}
#endif
