using System;
using Features.FlightFieldModule.Contracts;
using Features.WeaponModule.Contracts;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Targets {
    // Server: every target balloon of the field as a weapon target, registered once as a source for the balloons' id
    // range instead of once per balloon, so the balloons need no scene object on the server.
    public sealed class FieldTargetWeaponTargetSystem : IWeaponTargetSource, IInitializable, IDisposable {
        private readonly IWeaponTargetRegistry _weaponTargetRegistry;
        private readonly IFieldTargetWeaponIds _fieldTargetWeaponIds;
        private readonly FieldTargetWeaponTarget _fieldTargetWeaponTarget;

        public FieldTargetWeaponTargetSystem(IWeaponTargetRegistry weaponTargetRegistry, IFieldTargetWeaponIds fieldTargetWeaponIds, FieldTargetWeaponTarget fieldTargetWeaponTarget) {
            _weaponTargetRegistry = weaponTargetRegistry;
            _fieldTargetWeaponIds = fieldTargetWeaponIds;
            _fieldTargetWeaponTarget = fieldTargetWeaponTarget;
        }

        public void Initialize() =>
            _weaponTargetRegistry.RegisterSource(this);

        public void Dispose() =>
            _weaponTargetRegistry.UnregisterSource(this);

        public bool TryGetTarget(ulong targetId, out IWeaponTarget target) {
            target = null;
            if (!_fieldTargetWeaponIds.TryGetBalloonId(targetId, out FieldTargetId id))
                return false;

            _fieldTargetWeaponTarget.AimAt(id);
            target = _fieldTargetWeaponTarget;
            return true;
        }
    }
}
