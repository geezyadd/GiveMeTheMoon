namespace Features.GameCoreModule.Contracts {
    // The one server entry for damage to the run's ship: armor is applied here, the result goes to the synced stats.
    public interface IShipHull {
        public void ServerApplyDamage(ShipDamage damage);
    }
}
