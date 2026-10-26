using System;
using Features.FlightFieldModule.Contracts;
using Features.WeaponModule.Contracts;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Server: every rock of the field as a weapon target, registered once as a source for the rocks' id range instead
    // of once per rock, so the rocks need no scene object on the server.
    public sealed class FieldRockWeaponTargetSystem : IWeaponTargetSource, IInitializable, IDisposable {
        private readonly IWeaponTargetRegistry _weaponTargetRegistry;
        private readonly IFieldRockTargetIds _fieldRockTargetIds;
        private readonly FieldRockWeaponTarget _fieldRockWeaponTarget;

        public FieldRockWeaponTargetSystem(IWeaponTargetRegistry weaponTargetRegistry, IFieldRockTargetIds fieldRockTargetIds, FieldRockWeaponTarget fieldRockWeaponTarget) {
            _weaponTargetRegistry = weaponTargetRegistry;
            _fieldRockTargetIds = fieldRockTargetIds;
            _fieldRockWeaponTarget = fieldRockWeaponTarget;
        }

        public void Initialize() =>
            _weaponTargetRegistry.RegisterSource(this);

        public void Dispose() =>
            _weaponTargetRegistry.UnregisterSource(this);

        public bool TryGetTarget(ulong targetId, out IWeaponTarget target) {
            target = null;
            if (!_fieldRockTargetIds.TryGetRockId(targetId, out FieldRockId id))
                return false;

            _fieldRockWeaponTarget.AimAt(id);
            target = _fieldRockWeaponTarget;
            return true;
        }
    }
}
