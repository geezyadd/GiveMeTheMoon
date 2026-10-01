using System;
using System.Collections.Generic;

namespace Features.PlayerLifeModule.Scripts {
    public interface IPlayerBodyRegistry {
        IReadOnlyList<PlayerLifeBody> ServerBodies { get; }
        IReadOnlyList<PlayerLifeBody> ClientBodies { get; }
        PlayerLifeBody LocalBody { get; }

        event Action<PlayerLifeBody> OnServerBodyAdded;
        event Action<PlayerLifeBody> OnServerBodyRemoved;
        event Action OnClientBodiesChanged;

        void AddServer(PlayerLifeBody body);
        void RemoveServer(PlayerLifeBody body);
        void AddClient(PlayerLifeBody body, bool isLocal);
        void RemoveClient(PlayerLifeBody body);
    }
}
