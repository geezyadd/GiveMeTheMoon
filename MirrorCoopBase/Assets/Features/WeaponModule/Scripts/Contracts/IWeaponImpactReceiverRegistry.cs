namespace Features.WeaponModule.Contracts {
    // Where bodies that react to shots make themselves known on their peer while they can be struck.
    public interface IWeaponImpactReceiverRegistry {
        public void Add(IWeaponImpactReceiver receiver);

        public void Remove(IWeaponImpactReceiver receiver);
    }
}
