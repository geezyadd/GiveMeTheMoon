#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Mirror;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Features.ShipModule.Scripts.Debug {
    // Debug only, on the host: the debug window's weapon actions put a weapon test dummy in front of the ship or take
    // it away again, so a turret has something to hit that counts the hits.
    public sealed class WeaponDebugTargetSystem : IInitializable, IDisposable, IWeaponDebugTargets {
        private const string SPAWN_LABEL = "Spawn the weapon test dummy";
        private const string SPAWN_DESCRIPTION = "Puts a swaying dummy in front of the ship that counts and flashes the hits the server accepted (a new one replaces the old one).";
        private const string DESPAWN_LABEL = "Remove the weapon test dummy";
        private const string DESPAWN_DESCRIPTION = "Takes the weapon test dummy away.";
        private const string NO_TARGET_REASON = "No test dummy spawned.";

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly WeaponDebugTargetConfiguration _weaponDebugTargetConfiguration;
        private readonly WeaponDebugTargetModel _weaponDebugTargetModel;
        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly List<IDisposable> _registrations = new();

        public WeaponDebugTargetSystem(
            IShipRunBindingModel shipRunBindingModel,
            WeaponDebugTargetConfiguration weaponDebugTargetConfiguration,
            WeaponDebugTargetModel weaponDebugTargetModel,
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions) {
            _shipRunBindingModel = shipRunBindingModel;
            _weaponDebugTargetConfiguration = weaponDebugTargetConfiguration;
            _weaponDebugTargetModel = weaponDebugTargetModel;
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
        }

        public void Initialize() {
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.WEAPONS, SPAWN_LABEL, SPAWN_DESCRIPTION, _shipDebugActionConditions.ReadHostWithShipReason, SpawnTarget)));
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.WEAPONS, DESPAWN_LABEL, DESPAWN_DESCRIPTION, ReadDespawnUnavailableReason, DebugDespawnTarget)));
        }

        public void Dispose() {
            foreach (IDisposable registration in _registrations)
                registration.Dispose();

            _registrations.Clear();
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

        private void SpawnTarget() =>
            DebugSpawnTarget();

        private string ReadDespawnUnavailableReason() {
            string reason = _shipDebugActionConditions.ReadHostReason();
            if (reason != null)
                return reason;

            return _weaponDebugTargetModel.Target == null ? NO_TARGET_REASON : null;
        }
    }
}
#endif
