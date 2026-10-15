using System;
using System.Collections.Generic;
using Mirror;

namespace Game.Connection
{
    // A client asks for its player right after it is accepted, but a player who joins a running map still has to
    // load that map. Its player object is made only once the client reports the map loaded: the body then appears
    // where the map is now (on a ship that may have taken off meanwhile), and nobody sees it stand frozen in the
    // meantime.
    internal sealed class ConnectionLateJoin
    {
        private readonly Action<NetworkConnectionToClient> _addPlayer;
        private readonly HashSet<NetworkConnectionToClient> _mapLoaded = new HashSet<NetworkConnectionToClient>();
        private readonly HashSet<NetworkConnectionToClient> _waitingForMap = new HashSet<NetworkConnectionToClient>();

        public ConnectionLateJoin(Action<NetworkConnectionToClient> addPlayer)
        {
            _addPlayer = addPlayer;
        }

        // Mirror sends a client the server's scene on accept only when the server has one; without it the client
        // loads nothing and never reports.
        public bool TryDeferPlayer(NetworkConnectionToClient conn)
        {
            if (string.IsNullOrEmpty(NetworkManager.networkSceneName) || _mapLoaded.Contains(conn))
                return false;

            _waitingForMap.Add(conn);
            return true;
        }

        public void OnMapLoaded(NetworkConnectionToClient conn)
        {
            _mapLoaded.Add(conn);
            if (_waitingForMap.Remove(conn))
                _addPlayer(conn);
        }

        public void Forget(NetworkConnectionToClient conn)
        {
            _mapLoaded.Remove(conn);
            _waitingForMap.Remove(conn);
        }

        public void Clear()
        {
            _mapLoaded.Clear();
            _waitingForMap.Clear();
        }
    }
}
