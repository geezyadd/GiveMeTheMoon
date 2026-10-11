using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerLifeModule.Scripts.Generated;
using PlayerLifeState = Features.PlayerLifeModule.Scripts.PlayerLifeState;

namespace Features.ShopModule.Scripts.Core {
    // Reads the synced PlayerLife records, so a player who joined or rejoined right now sees the same dead crew.
    public sealed class DeadCrewService : IDeadCrewService {
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly IReadOnlyPlayerLifeRegistry _playerLifeRegistry;

        public DeadCrewService(IPlayerBodyRegistry playerBodyRegistry, IReadOnlyPlayerLifeRegistry playerLifeRegistry) {
            _playerBodyRegistry = playerBodyRegistry;
            _playerLifeRegistry = playerLifeRegistry;
        }

        // The records have no list of keys: the player bodies spawned on this client name the players in the session.
        public IReadOnlyList<PlayerKey> GetDeadOnlineCrew() {
            List<PlayerKey> deadCrew = new();
            IReadOnlyList<PlayerLifeBody> bodies = _playerBodyRegistry.ClientBodies;
            for (int i = 0; i < bodies.Count; i++) {
                PlayerKey key = bodies[i].Key;
                if (IsOnline(key) && IsDead(key))
                    deadCrew.Add(key);
            }

            return deadCrew;
        }

        public bool IsOnline(PlayerKey key) =>
            _playerLifeRegistry.IsOnline(key);

        public bool IsDead(PlayerKey key) =>
            _playerLifeRegistry.TryGet(key, out IReadOnlyPlayerLifeModel life) && life.LifeState == PlayerLifeState.Dead;
    }
}
