#if UNITY_INCLUDE_TESTS
using Zenject;

namespace Tests.PlayMode.LoopSmoke {
    // What the loop test hands every registered check: the host's session, the flight helper, the field check and the
    // Steam lobby stand-in. One context for the whole test.
    public sealed class LoopSmokeContext {
        public LoopSmokeSession Session { get; }
        public LoopSmokeSteamLobbySpy SteamLobby { get; }
        public LoopSmokeFieldCheck FieldCheck { get; private set; }
        public LoopSmokeFlight Flight { get; private set; }

        public LoopSmokeContext(LoopSmokeSession session, LoopSmokeSteamLobbySpy steamLobby) {
            Session = session;
            SteamLobby = steamLobby;
        }

        // The field check reads the game's configurations: made once the first run's GameScene stands.
        public void CreateFlight() {
            FieldCheck = new LoopSmokeFieldCheck(ProjectContext.Instance.Container);
            Flight = new LoopSmokeFlight(Session, FieldCheck);
        }
    }
}
#endif
