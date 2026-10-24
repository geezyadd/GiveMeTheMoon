using System;

namespace Features.GameCoreModule.Contracts {
    // Server only. OnDamaged gets the hit and the hull left after it; OnDestroyed fires once when the hull reaches 0.
    public interface IShipHullEvents {
        public event Action<ShipDamage, float> OnDamaged;
        public event Action OnDestroyed;
    }
}
