using System;
using System.Collections.Generic;
using UnityEngine;
namespace Features.PlayerLifeModule.Scripts {
    public interface IPlayerLifeQuery {
        PlayerLifeState LocalState { get; }                 // state of the local player on this peer
        event Action<PlayerLifeState> OnLocalStateChanged;
        IReadOnlyList<Transform> AlivePlayerTargets { get; } // follow targets of players that are Alive (any peer)
        event Action OnAlivePlayersChanged;
    }
}
