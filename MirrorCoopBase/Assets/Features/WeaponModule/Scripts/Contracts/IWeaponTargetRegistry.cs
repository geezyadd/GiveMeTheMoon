namespace Features.WeaponModule.Contracts {
    // Where a weapon target makes itself known to the server's hit path under its target id. A target registers while
    // it can be hit and unregisters when it leaves, so a hit report on an unknown id is dropped. A family of targets
    // with its own id range registers as one source instead.
    public interface IWeaponTargetRegistry {
        public void Register(ulong targetId, IWeaponTarget target);

        public void Unregister(ulong targetId);

        public void RegisterSource(IWeaponTargetSource source);

        public void UnregisterSource(IWeaponTargetSource source);
    }
}
