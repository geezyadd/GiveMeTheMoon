using Features.NetworkModelModule.Scripts;

namespace Features.WeaponModule.Contracts {
    // Every peer: the weapon a player holds in the hand now and where it is, by the player's key, so a rope can be
    // drawn from it and the server can break a rope whose holder let go of the weapon or went too far.
    public interface IWeaponHolderAnchors {
        public bool TryGetAnchor(PlayerKey holder, out WeaponHolderAnchor anchor);
    }
}
