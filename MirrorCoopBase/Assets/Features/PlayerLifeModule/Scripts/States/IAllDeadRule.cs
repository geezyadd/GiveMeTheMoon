using System.Collections.Generic;

namespace Features.PlayerLifeModule.Scripts {
    public interface IAllDeadRule {
        bool AreAllDead(IReadOnlyList<PlayerLifeStateMachine> players);
    }
}
