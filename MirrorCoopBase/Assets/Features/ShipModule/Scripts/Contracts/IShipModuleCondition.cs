using System;

namespace Features.ShipModule.Scripts.Contracts {
    // Every peer: whether the module in a socket works. A broken module stays installed but its stats and its function
    // (a turret's fire, a sonar's picture) are off until it is repaired. Read it on OnConditionChanged, not every frame.
    public interface IShipModuleCondition {
        public event Action OnConditionChanged;

        public bool IsWorking(string socketId);
    }
}
