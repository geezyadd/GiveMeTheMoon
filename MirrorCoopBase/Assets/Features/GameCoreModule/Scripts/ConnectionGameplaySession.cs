using Features.GameCoreModule.Contracts;
using Game.Connection;

namespace Features.GameCoreModule.Scripts {
    public sealed class ConnectionGameplaySession : IGameplaySession {
        public void CleanupGameplay() {
            ConnectionNetworkManager.ClearSpawn();
        }

        // Host / Join load the lobby, whose spawn point sets the spawn, before the session state is entered: clearing it
        // here sent every later joiner to the world origin. CleanupGameplay and stopping the server / client clear it.
        public void RestartGameplay() {
        }
    }
}
