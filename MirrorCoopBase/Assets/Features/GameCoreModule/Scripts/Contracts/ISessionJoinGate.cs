using System;

namespace Features.GameCoreModule.Contracts {
    // The running map's side of "may a player come in now": the session layer asks it without knowing about the ship.
    public interface ISessionJoinGate {
        public RunJoinWindow Window { get; }

        public event Action OnWindowChanged;
    }
}
