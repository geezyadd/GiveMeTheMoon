using Features.WeaponModule.Scripts.Network;

namespace Features.WeaponModule.Scripts.Core {
    // The local player's weapon: what they hold and what it can do.
    public interface IWeaponService {
        public bool HasWeapon { get; }
        public IWeapon Current { get; }
        public WeaponHolderSync Holder { get; }

        public void Equip(IWeapon weapon, WeaponHolderSync holder);

        // Only the holder that equipped the current weapon takes it away.
        public void Unequip(WeaponHolderSync holder);

        public bool TryGetCapability<TCapability>(out TCapability capability) where TCapability : class;
    }
}
