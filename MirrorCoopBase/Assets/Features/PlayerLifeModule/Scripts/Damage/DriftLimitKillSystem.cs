using System;
using System.Collections.Generic;
using Features.ShipModule.Scripts.Overboard;
using Mirror;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    // Server: a player who drifted overboard past the limit dies a normal death (and can be bought back at a station).
    public sealed class DriftLimitKillSystem : IInitializable, IDisposable {
        private readonly IShipOverboardEvents _shipOverboardEvents;
        private readonly IPlayerBodyRegistry _playerBodyRegistry;

        public DriftLimitKillSystem(IShipOverboardEvents shipOverboardEvents, IPlayerBodyRegistry playerBodyRegistry) {
            _shipOverboardEvents = shipOverboardEvents;
            _playerBodyRegistry = playerBodyRegistry;
        }

        public void Initialize() =>
            _shipOverboardEvents.OnDriftLimitReached += OnDriftLimitReached;

        public void Dispose() =>
            _shipOverboardEvents.OnDriftLimitReached -= OnDriftLimitReached;

        private void OnDriftLimitReached(NetworkIdentity player) {
            IReadOnlyList<PlayerLifeBody> bodies = _playerBodyRegistry.ServerBodies;
            for (int i = 0; i < bodies.Count; i++) {
                if (bodies[i].netIdentity == player && bodies[i].Damageable.IsDead == false)
                    bodies[i].Damageable.ServerKill(DamageType.Drift);
            }
        }
    }
}
