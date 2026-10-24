using System;

namespace Features.ShipModule.Scripts.Damage {
    public sealed class ShipDamageEventBus : IShipDamageEvents, IShipDamageEventPublisher {
        public event Action<string> OnModuleBroken;
        public event Action<string> OnHullBreached;
        public event Action<ShipRepairKind, string> OnRepaired;

        public void PublishModuleBroken(string socketId) =>
            OnModuleBroken?.Invoke(socketId);

        public void PublishHullBreached(string pointId) =>
            OnHullBreached?.Invoke(pointId);

        public void PublishRepaired(ShipRepairKind kind, string id) =>
            OnRepaired?.Invoke(kind, id);
    }
}
