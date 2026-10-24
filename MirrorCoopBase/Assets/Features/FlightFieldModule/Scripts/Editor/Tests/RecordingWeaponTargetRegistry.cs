using System.Collections.Generic;
using Features.WeaponModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The weapon module's target registry as the field sees it: it only records who registered.
    public sealed class RecordingWeaponTargetRegistry : IWeaponTargetRegistry {
        private readonly Dictionary<ulong, IWeaponTarget> _targets = new();
        private readonly List<IWeaponTargetSource> _sources = new();

        public IReadOnlyDictionary<ulong, IWeaponTarget> Targets => _targets;
        public IReadOnlyList<IWeaponTargetSource> Sources => _sources;

        public void Register(ulong targetId, IWeaponTarget target) =>
            _targets.Add(targetId, target);

        public void Unregister(ulong targetId) =>
            _targets.Remove(targetId);

        public void RegisterSource(IWeaponTargetSource source) =>
            _sources.Add(source);

        public void UnregisterSource(IWeaponTargetSource source) =>
            _sources.Remove(source);
    }
}
