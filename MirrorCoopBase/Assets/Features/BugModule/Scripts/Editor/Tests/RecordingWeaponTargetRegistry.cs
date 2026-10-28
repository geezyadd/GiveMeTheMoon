using System.Collections.Generic;
using Features.WeaponModule.Contracts;

namespace Features.BugModule.Scripts.Editor.Tests {
    // Records the target sources registered with the weapon hit path.
    public sealed class RecordingWeaponTargetRegistry : IWeaponTargetRegistry {
        public List<IWeaponTargetSource> Sources { get; } = new();
        public List<ulong> TargetIds { get; } = new();

        public void Register(ulong targetId, IWeaponTarget target) =>
            TargetIds.Add(targetId);

        public void Unregister(ulong targetId) =>
            TargetIds.Remove(targetId);

        public void RegisterSource(IWeaponTargetSource source) =>
            Sources.Add(source);

        public void UnregisterSource(IWeaponTargetSource source) =>
            Sources.Remove(source);
    }
}
