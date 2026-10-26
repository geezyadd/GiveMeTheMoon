namespace Features.WeaponModule.Contracts {
    // A component next to a collider a weapon ray can strike: which weapon target the collider belongs to.
    public interface IWeaponTargetCollider {
        public ulong TargetId { get; }
    }
}
