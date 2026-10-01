using System.Collections.Generic;

namespace Features.PlayerLifeModule.Scripts {
    public sealed class AllDeadRule : IAllDeadRule {
        public bool AreAllDead(IReadOnlyList<IPlayerLifeStateMachine> players) {
            if (players.Count == 0)
                return false;

            for (int i = 0; i < players.Count; i++) {
                if (players[i].State != PlayerLifeState.Dead)
                    return false;
            }

            return true;
        }
    }
}
