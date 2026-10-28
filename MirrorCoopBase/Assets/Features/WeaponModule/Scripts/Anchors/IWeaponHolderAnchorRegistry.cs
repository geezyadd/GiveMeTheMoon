using Features.NetworkModelModule.Scripts;
using Features.WeaponModule.Scripts.Parts.Holding;

namespace Features.WeaponModule.Scripts.Anchors {
    // Where a holder puts the weapon it holds in the hand while it holds it, on every peer.
    public interface IWeaponHolderAnchorRegistry {
        public void Register(PlayerKey holder, string weaponId, IWeaponAim aim);
        public void Unregister(PlayerKey holder);
    }
}
