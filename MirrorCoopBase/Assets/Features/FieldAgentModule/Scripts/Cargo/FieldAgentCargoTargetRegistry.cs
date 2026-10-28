using System.Collections.Generic;

namespace Features.FieldAgentModule.Scripts.Cargo {
    // Server: the cargo targets on board in the order they joined; a target joins once.
    public sealed class FieldAgentCargoTargetRegistry : IFieldAgentCargoTargetRegistry, IFieldAgentCargoTargets {
        private readonly List<IFieldAgentCargoTarget> _items = new();

        public IReadOnlyList<IFieldAgentCargoTarget> Items => _items;

        public void Add(IFieldAgentCargoTarget cargoTarget) {
            if (_items.Contains(cargoTarget) == false)
                _items.Add(cargoTarget);
        }

        public void Remove(IFieldAgentCargoTarget cargoTarget) =>
            _items.Remove(cargoTarget);
    }
}
