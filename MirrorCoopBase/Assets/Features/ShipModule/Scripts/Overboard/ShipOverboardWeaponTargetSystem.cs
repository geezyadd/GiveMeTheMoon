using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts.Contracts;
using Features.WeaponModule.Contracts;
using Zenject;

namespace Features.ShipModule.Scripts.Overboard {
    // Server: every drifting player as a weapon target a rope can hook, registered once as a source for the players'
    // id range.
    public sealed class ShipOverboardWeaponTargetSystem : IWeaponTargetSource, IInitializable, IDisposable {
        private readonly IWeaponTargetRegistry _weaponTargetRegistry;
        private readonly IShipOverboardWeaponIds _shipOverboardWeaponIds;
        private readonly IReadOnlyComponentRegistry<ShipOverboardRider> _overboardRiders;
        private readonly ShipOverboardWeaponTarget _shipOverboardWeaponTarget;

        public ShipOverboardWeaponTargetSystem(
            IWeaponTargetRegistry weaponTargetRegistry,
            IShipOverboardWeaponIds shipOverboardWeaponIds,
            IReadOnlyComponentRegistry<ShipOverboardRider> overboardRiders,
            IInstantiator instantiator) {
            _weaponTargetRegistry = weaponTargetRegistry;
            _shipOverboardWeaponIds = shipOverboardWeaponIds;
            _overboardRiders = overboardRiders;
            _shipOverboardWeaponTarget = instantiator.Instantiate<ShipOverboardWeaponTarget>();
        }

        public void Initialize() =>
            _weaponTargetRegistry.RegisterSource(this);

        public void Dispose() =>
            _weaponTargetRegistry.UnregisterSource(this);

        public bool TryGetTarget(ulong targetId, out IWeaponTarget target) {
            target = null;
            if (_shipOverboardWeaponIds.TryGetPlayerNetId(targetId, out uint playerNetId) == false || TryFindOverboard(playerNetId, out ShipOverboardRider overboardRider) == false)
                return false;

            _shipOverboardWeaponTarget.AimAt(overboardRider);
            target = _shipOverboardWeaponTarget;
            return true;
        }

        private bool TryFindOverboard(uint playerNetId, out ShipOverboardRider overboardRider) {
            IReadOnlyList<ShipOverboardRider> riders = _overboardRiders.Items;
            for (int i = 0; i < riders.Count; i++) {
                overboardRider = riders[i];
                if (overboardRider != null && overboardRider.netId == playerNetId && overboardRider.IsOverboard)
                    return true;
            }

            overboardRider = null;
            return false;
        }
    }
}
