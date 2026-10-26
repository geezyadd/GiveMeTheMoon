using System;
using System.Collections.Generic;
using Features.WeaponModule.Contracts;

namespace Features.WeaponModule.Scripts.Targets {
    // The server's targets by id: what a reported hit is applied to. A target registered by its own id comes first,
    // then the sources in the order they registered.
    public sealed class WeaponTargetRegistry : IWeaponTargetRegistry, IWeaponTargetLookup {
        private const string DUPLICATE_TARGET_FORMAT = "Weapon target id {0} is registered twice.";
        private const string DUPLICATE_SOURCE_FORMAT = "Weapon target source {0} is registered twice.";

        private readonly Dictionary<ulong, IWeaponTarget> _targets = new Dictionary<ulong, IWeaponTarget>();
        private readonly List<IWeaponTargetSource> _sources = new List<IWeaponTargetSource>();

        public void Register(ulong targetId, IWeaponTarget target) {
            if (_targets.ContainsKey(targetId))
                throw new InvalidOperationException(string.Format(DUPLICATE_TARGET_FORMAT, targetId));

            _targets.Add(targetId, target);
        }

        public void Unregister(ulong targetId) =>
            _targets.Remove(targetId);

        public void RegisterSource(IWeaponTargetSource source) {
            if (_sources.Contains(source))
                throw new InvalidOperationException(string.Format(DUPLICATE_SOURCE_FORMAT, source));

            _sources.Add(source);
        }

        public void UnregisterSource(IWeaponTargetSource source) =>
            _sources.Remove(source);

        public bool TryGet(ulong targetId, out IWeaponTarget target) {
            if (_targets.TryGetValue(targetId, out target))
                return true;

            foreach (IWeaponTargetSource source in _sources) {
                if (source.TryGetTarget(targetId, out target))
                    return true;
            }

            target = null;
            return false;
        }
    }
}
