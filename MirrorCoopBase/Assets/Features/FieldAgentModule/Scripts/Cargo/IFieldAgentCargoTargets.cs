using System.Collections.Generic;

namespace Features.FieldAgentModule.Scripts.Cargo {
    // Server: the cargo targets on board now, for the agents that choose what to go for.
    public interface IFieldAgentCargoTargets {
        public IReadOnlyList<IFieldAgentCargoTarget> Items { get; }
    }
}
