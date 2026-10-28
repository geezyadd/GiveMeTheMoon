using System;

namespace Game.Connection
{
    // Network session lifecycle events; only ConnectionNetworkManager raises them.
    public sealed class ConnectionNetworkEvents
    {
        public event Action ServerStarted;
        public event Action ClientStarted;
        public event Action ServerStopped;
        public event Action ClientStopped;
        public event Action MapLoadStarted;
        public event Action MapReady;
        public event Action<string> MapUnloading;
        public event Action ServerPlayersChanged;
        public event Action ReturnedToMenu;
        // The session ended without the local player's Leave (a rejected join, the host quit, a lost connection), once
        // the menu scene is back.
        public event Action SessionDropped;

        internal void RaiseServerStarted() => ServerStarted?.Invoke();
        internal void RaiseClientStarted() => ClientStarted?.Invoke();
        internal void RaiseServerStopped() => ServerStopped?.Invoke();
        internal void RaiseClientStopped() => ClientStopped?.Invoke();
        internal void RaiseMapLoadStarted() => MapLoadStarted?.Invoke();
        internal void RaiseMapReady() => MapReady?.Invoke();
        internal void RaiseMapUnloading(string sceneName) => MapUnloading?.Invoke(sceneName);
        internal void RaiseServerPlayersChanged() => ServerPlayersChanged?.Invoke();
        internal void RaiseReturnedToMenu() => ReturnedToMenu?.Invoke();
        internal void RaiseSessionDropped() => SessionDropped?.Invoke();
    }
}
