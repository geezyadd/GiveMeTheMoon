using Features.WeaponModule.Contracts;

namespace Features.WeaponModule.Scripts.Targets {
    public interface IWeaponTargetLookup {
        public bool TryGet(ulong targetId, out IWeaponTarget target);
    }
}
