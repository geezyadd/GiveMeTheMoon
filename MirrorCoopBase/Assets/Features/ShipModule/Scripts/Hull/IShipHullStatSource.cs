namespace Features.ShipModule.Scripts.Hull {
    public interface IShipHullStatSource {
        public bool TryGetHull(out IShipHullStat hull);
    }
}
