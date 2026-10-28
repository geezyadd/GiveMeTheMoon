using System;
using Features.FlightFieldModule.Contracts;
using Features.WeaponModule.Contracts;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Ore {
    // Server: every piece of ore of the field as a weapon target, registered once as a source for the ore's id range
    // instead of once per piece, so the server needs no scene object of it.
    public sealed class FieldOreWeaponTargetSystem : IWeaponTargetSource, IInitializable, IDisposable {
        private readonly IWeaponTargetRegistry _weaponTargetRegistry;
        private readonly IFieldOreWeaponIds _fieldOreWeaponIds;
        private readonly FieldOreWeaponTarget _fieldOreWeaponTarget;

        public FieldOreWeaponTargetSystem(IWeaponTargetRegistry weaponTargetRegistry, IFieldOreWeaponIds fieldOreWeaponIds, IInstantiator instantiator) {
            _weaponTargetRegistry = weaponTargetRegistry;
            _fieldOreWeaponIds = fieldOreWeaponIds;
            _fieldOreWeaponTarget = instantiator.Instantiate<FieldOreWeaponTarget>();
        }

        public void Initialize() =>
            _weaponTargetRegistry.RegisterSource(this);

        public void Dispose() =>
            _weaponTargetRegistry.UnregisterSource(this);

        public bool TryGetTarget(ulong targetId, out IWeaponTarget target) {
            target = null;
            if (!_fieldOreWeaponIds.TryGetOreId(targetId, out FieldOreId id))
                return false;

            _fieldOreWeaponTarget.AimAt(id);
            target = _fieldOreWeaponTarget;
            return true;
        }
    }
}
