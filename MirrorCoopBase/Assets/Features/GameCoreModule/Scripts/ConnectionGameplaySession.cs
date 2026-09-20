using Game.Connection;

namespace Features.GameCoreModule.Scripts {
    public sealed class ConnectionGameplaySession : IGameplaySession {
        public void CleanupGameplay() {
            ConnectionNetworkManager.ClearSpawn();
        }

        public void RestartGameplay() {
            ConnectionNetworkManager.ClearSpawn();
        }
    }
}
