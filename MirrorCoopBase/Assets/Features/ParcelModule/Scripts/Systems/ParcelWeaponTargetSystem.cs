using System;
using Features.ParcelModule.Scripts.Server;
using Features.ParcelModule.Scripts.Services;
using Features.WeaponModule.Contracts;
using Zenject;

namespace Features.ParcelModule.Scripts.Systems {
    // Server: every parcel crate as a weapon target, registered once as a source for the parcels' id range.
    public sealed class ParcelWeaponTargetSystem : IWeaponTargetSource, IInitializable, IDisposable {
        private readonly IWeaponTargetRegistry _weaponTargetRegistry;
        private readonly IParcelWeaponIds _parcelWeaponIds;
        private readonly ParcelPullTarget _parcelPullTarget;

        public ParcelWeaponTargetSystem(IWeaponTargetRegistry weaponTargetRegistry, IParcelWeaponIds parcelWeaponIds, IInstantiator instantiator) {
            _weaponTargetRegistry = weaponTargetRegistry;
            _parcelWeaponIds = parcelWeaponIds;
            _parcelPullTarget = instantiator.Instantiate<ParcelPullTarget>();
        }

        public void Initialize() =>
            _weaponTargetRegistry.RegisterSource(this);

        public void Dispose() =>
            _weaponTargetRegistry.UnregisterSource(this);

        public bool TryGetTarget(ulong targetId, out IWeaponTarget target) {
            target = null;
            if (_parcelWeaponIds.TryGetCrateNetId(targetId, out uint netId) == false)
                return false;

            _parcelPullTarget.AimAt(netId);
            target = _parcelPullTarget;
            return true;
        }
    }
}
