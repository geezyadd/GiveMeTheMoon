namespace Features.ShipModule.Scripts.Contracts {
    // Server: writes the bound ship's synced condition (broken sockets, open breaches). Each add / remove returns
    // whether the list changed.
    public interface IShipConditionWriter {
        public bool ServerAddBrokenSockets(string socketId);
        public bool ServerRemoveBrokenSockets(string socketId);
        public bool ServerAddOpenBreaches(string pointId);
        public bool ServerRemoveOpenBreaches(string pointId);
        public void ServerClearOpenBreaches();
    }
}
