using Features.WeaponModule.Contracts;

namespace Features.WeaponModule.Editor.Tests {
    // A target source that answers for one id only.
    public sealed class SingleIdTargetSource : IWeaponTargetSource {
        private readonly ulong _targetId;
        private readonly IWeaponTarget _target;

        public SingleIdTargetSource(ulong targetId, IWeaponTarget target) {
            _targetId = targetId;
            _target = target;
        }

        public bool TryGetTarget(ulong targetId, out IWeaponTarget target) {
            target = targetId == _targetId ? _target : null;
            return target != null;
        }
    }
}
