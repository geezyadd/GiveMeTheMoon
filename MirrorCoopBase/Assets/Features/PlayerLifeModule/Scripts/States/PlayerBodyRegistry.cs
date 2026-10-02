using System;
using System.Collections.Generic;

namespace Features.PlayerLifeModule.Scripts {
    // Spawned player bodies on this peer: the server list drives the life rules, the client list drives the query.
    public sealed class PlayerBodyRegistry : IPlayerBodyRegistry {
        private readonly List<PlayerLifeBody> _serverBodies = new();
        private readonly List<PlayerLifeBody> _clientBodies = new();

        public IReadOnlyList<PlayerLifeBody> ServerBodies =>
            _serverBodies;

        public IReadOnlyList<PlayerLifeBody> ClientBodies =>
            _clientBodies;

        public PlayerLifeBody LocalBody { get; private set; }

        public event Action<PlayerLifeBody> OnServerBodyAdded;
        public event Action<PlayerLifeBody> OnServerBodyRemoved;
        public event Action OnClientBodiesChanged;

        public void AddServer(PlayerLifeBody body) {
            if (_serverBodies.Contains(body))
                return;

            _serverBodies.Add(body);
            OnServerBodyAdded?.Invoke(body);
        }

        public void RemoveServer(PlayerLifeBody body) {
            if (_serverBodies.Remove(body) == false)
                return;

            OnServerBodyRemoved?.Invoke(body);
        }

        public void AddClient(PlayerLifeBody body, bool isLocal) {
            bool added = _clientBodies.Contains(body) == false;
            if (added)
                _clientBodies.Add(body);

            bool localChanged = isLocal && LocalBody != body;
            if (localChanged)
                LocalBody = body;

            if (added || localChanged)
                OnClientBodiesChanged?.Invoke();
        }

        public void RemoveClient(PlayerLifeBody body) {
            if (_clientBodies.Remove(body) == false)
                return;

            if (LocalBody == body)
                LocalBody = null;

            OnClientBodiesChanged?.Invoke();
        }
    }
}
