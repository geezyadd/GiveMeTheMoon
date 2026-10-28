namespace Features.WeaponModule.Contracts {
    // Anything a weapon can hit (a field rock, field ore, later an enemy). Implemented outside the weapon module; what
    // a hit does to it is a role it implements on top (IWeaponDamageTarget, IWeaponPullTarget).
    public interface IWeaponTarget {
        public bool IsAlive { get; }
    }
}
