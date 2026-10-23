namespace Features.ShipModule.Scripts.Hull {
    // The bound ship's hull on the server: the Hull stat (current and max) and the Armor stat of its stat sheet.
    public interface IShipHullStat {
        public float Current { get; }
        public float Max { get; }
        public float Armor { get; }
        public void ServerSubtract(float amount);
        public void ServerRepair();
    }
}
