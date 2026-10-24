namespace Features.ShipModule.Scripts.Damage {
    public interface IShipDamageEventPublisher {
        public void PublishModuleBroken(string socketId);
        public void PublishHullBreached(string pointId);
        public void PublishRepaired(ShipRepairKind kind, string id);
    }
}
