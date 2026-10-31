using System;
using Features.WeaponModule.Contracts;
using Zenject;

namespace Features.PointOfInterestModule.Scripts.Weapons {
    // Server: every reward container as a weapon target, registered once as a source for the containers' id range;
    // the look of a point on each peer carries the collider a shot strikes.
    public sealed class PointOfInterestWeaponTargetSystem : IWeaponTargetSource, IInitializable, IDisposable {
        private readonly IWeaponTargetRegistry _weaponTargetRegistry;
        private readonly IPointOfInterestWeaponIds _pointOfInterestWeaponIds;
        private readonly PointOfInterestWeaponTarget _pointOfInterestWeaponTarget;

        public PointOfInterestWeaponTargetSystem(IWeaponTargetRegistry weaponTargetRegistry, IPointOfInterestWeaponIds pointOfInterestWeaponIds,
                                                 PointOfInterestWeaponTarget pointOfInterestWeaponTarget) {
            _weaponTargetRegistry = weaponTargetRegistry;
            _pointOfInterestWeaponIds = pointOfInterestWeaponIds;
            _pointOfInterestWeaponTarget = pointOfInterestWeaponTarget;
        }

        public void Initialize() =>
            _weaponTargetRegistry.RegisterSource(this);

        public void Dispose() =>
            _weaponTargetRegistry.UnregisterSource(this);

        public bool TryGetTarget(ulong targetId, out IWeaponTarget target) {
            target = null;
            if (!_pointOfInterestWeaponIds.TryGetPointId(targetId, out int pointId))
                return false;

            _pointOfInterestWeaponTarget.AimAt(pointId);
            target = _pointOfInterestWeaponTarget;
            return true;
        }
    }
}
