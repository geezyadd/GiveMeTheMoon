namespace Features.WeaponModule.Contracts {
    // A weapon target that a rope can hook and pull (field ore). Only targets that can be pulled implement it; a pull
    // on any other target does nothing. Only the server calls it, after the hit passed the weapon's hit rule.
    public interface IWeaponPullTarget {
        public void ServerReceivePull(in WeaponHit hit, in WeaponPull pull);
    }
}
