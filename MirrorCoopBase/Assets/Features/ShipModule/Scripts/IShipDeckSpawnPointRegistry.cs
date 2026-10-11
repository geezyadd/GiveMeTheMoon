namespace Features.ShipModule.Scripts {
    public interface IShipDeckSpawnPointRegistry {
        public void Add(ShipDeckSpawnPoint point);
        public void Remove(ShipDeckSpawnPoint point);
    }
}
