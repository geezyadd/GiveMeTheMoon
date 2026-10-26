namespace Features.WeaponModule.Contracts {
    // Anything a weapon can hit (a field rock, later an enemy). Implemented outside the weapon module; only the server
    // calls it, after the hit passed the weapon's hit rule.
    public interface IWeaponTarget {
        public bool IsAlive { get; }

        public void ServerReceiveHit(in WeaponHit hit);
    }
}
