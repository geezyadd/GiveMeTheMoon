namespace Features.WeaponModule.Contracts {
    // A family of weapon targets that answers for its own range of target ids (the rocks of a flight field), so it
    // registers once instead of once per target. It must answer false at once for an id outside its range.
    public interface IWeaponTargetSource {
        public bool TryGetTarget(ulong targetId, out IWeaponTarget target);
    }
}
