namespace Features.ShipModule.Scripts.Damage {
    // Server: the one way modules break and get repaired and breaches open and close on the run's ship.
    public interface IShipConditionService {
        public bool ServerBreakModule(string socketId);
        public bool ServerRepairModule(string socketId);
        public bool ServerOpenBreach(string pointId);
        public bool ServerRepairBreach(string pointId);
        // The free repair on landing: everything fixed at once, no repair events and no hull HP for the breaches.
        public void ServerRepairAll();
    }
}
