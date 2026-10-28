using System;
using Features.WeaponModule.Contracts;
using Zenject;

namespace Features.BugModule.Scripts.Health {
    // Server: every bug as a weapon target, registered once as a source for the bugs' id range, so a bug needs no
    // scene object on the server; the look of a bug on each peer carries the collider a shot strikes.
    public sealed class BugWeaponTargetSystem : IWeaponTargetSource, IInitializable, IDisposable {
        private readonly IWeaponTargetRegistry _weaponTargetRegistry;
        private readonly IBugWeaponIds _bugWeaponIds;
        private readonly BugWeaponTarget _bugWeaponTarget;

        public BugWeaponTargetSystem(IWeaponTargetRegistry weaponTargetRegistry, IBugWeaponIds bugWeaponIds, BugWeaponTarget bugWeaponTarget) {
            _weaponTargetRegistry = weaponTargetRegistry;
            _bugWeaponIds = bugWeaponIds;
            _bugWeaponTarget = bugWeaponTarget;
        }

        public void Initialize() =>
            _weaponTargetRegistry.RegisterSource(this);

        public void Dispose() =>
            _weaponTargetRegistry.UnregisterSource(this);

        public bool TryGetTarget(ulong targetId, out IWeaponTarget target) {
            target = null;
            if (_bugWeaponIds.TryGetBugId(targetId, out int bugId) == false)
                return false;

            _bugWeaponTarget.AimAt(bugId);
            target = _bugWeaponTarget;
            return true;
        }
    }
}
