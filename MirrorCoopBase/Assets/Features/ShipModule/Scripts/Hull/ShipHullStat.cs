namespace Features.ShipModule.Scripts.Hull {
    // Every change of the hull is published to the synced ShipStats model right away.
    internal sealed class ShipHullStat : IShipHullStat {
        private readonly ShipStatSheet _stats;
        private readonly IShipModules _modules;

        public float Current => _stats.GetFull(ShipStatType.Hull);
        public float Max => _stats.GetMax(ShipStatType.Hull);
        public float Armor => _stats.GetFull(ShipStatType.Armor);

        internal ShipHullStat(ShipStatSheet stats, IShipModules modules) {
            _stats = stats;
            _modules = modules;
        }

        public void ServerSubtract(float amount) {
            _stats.Subtract(ShipStatType.Hull, amount);
            _modules.ServerPublishStats();
        }

        public void ServerAdd(float amount) {
            _stats.Add(ShipStatType.Hull, amount);
            _modules.ServerPublishStats();
        }

        public void ServerRepair() {
            _stats.Refill(ShipStatType.Hull);
            _modules.ServerPublishStats();
        }
    }
}
