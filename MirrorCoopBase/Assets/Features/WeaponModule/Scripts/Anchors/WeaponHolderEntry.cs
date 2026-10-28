using Features.WeaponModule.Scripts.Parts.Holding;

namespace Features.WeaponModule.Scripts.Anchors {
    // One weapon held in a hand: which weapon and its live aim.
    public readonly struct WeaponHolderEntry {
        public string WeaponId { get; }
        public IWeaponAim Aim { get; }

        public WeaponHolderEntry(string weaponId, IWeaponAim aim) {
            WeaponId = weaponId;
            Aim = aim;
        }
    }
}
