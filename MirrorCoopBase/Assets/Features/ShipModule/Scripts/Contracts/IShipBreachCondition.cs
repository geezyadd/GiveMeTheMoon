using System;

namespace Features.ShipModule.Scripts.Contracts {
    // Every peer: which hull damage points are open breaches now.
    public interface IShipBreachCondition {
        public event Action OnBreachesChanged;

        public int OpenCount { get; }

        public bool IsOpen(string pointId);
    }
}
