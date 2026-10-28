namespace Features.WeaponModule.Contracts {
    // A weapon target that takes damage (a field rock, the debug dummy). Only the server calls it, after the hit
    // passed the weapon's hit rule.
    public interface IWeaponDamageTarget : IWeaponTarget {
        public void ServerReceiveHit(in WeaponHit hit);
    }
}
