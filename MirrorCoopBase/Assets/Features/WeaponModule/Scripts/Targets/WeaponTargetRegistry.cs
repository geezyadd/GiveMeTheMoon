using System;
using System.Collections.Generic;
using Features.WeaponModule.Contracts;

namespace Features.WeaponModule.Scripts.Targets {
    // The server's targets by id: what a reported hit is applied to.
    public sealed class WeaponTargetRegistry : IWeaponTargetRegistry, IWeaponTargetLookup {
        private const string DUPLICATE_TARGET_FORMAT = "Weapon target id {0} is registered twice.";

        private readonly Dictionary<ulong, IWeaponTarget> _targets = new Dictionary<ulong, IWeaponTarget>();

        public void Register(ulong targetId, IWeaponTarget target) {
            if (_targets.ContainsKey(targetId))
                throw new InvalidOperationException(string.Format(DUPLICATE_TARGET_FORMAT, targetId));

            _targets.Add(targetId, target);
        }

        public void Unregister(ulong targetId) =>
            _targets.Remove(targetId);

        public bool TryGet(ulong targetId, out IWeaponTarget target) =>
            _targets.TryGetValue(targetId, out target);
    }
}
