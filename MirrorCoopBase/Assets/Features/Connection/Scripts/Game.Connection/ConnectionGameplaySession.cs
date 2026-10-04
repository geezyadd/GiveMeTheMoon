using Features.GameCoreModule.Contracts;

namespace Game.Connection
{
    public sealed class ConnectionGameplaySession : IGameplaySession
    {
        private readonly ConnectionSpawnModel _spawn;

        public ConnectionGameplaySession(ConnectionSpawnModel spawn)
        {
            _spawn = spawn;
        }

        public void CleanupGameplay()
        {
            _spawn.Clear();
        }

        // Host / Join load the lobby, whose spawn point sets the spawn, before the session state is entered: clearing it
        // here sent every later joiner to the world origin. CleanupGameplay and stopping the server / client clear it.
        public void RestartGameplay()
        {
        }
    }
}
