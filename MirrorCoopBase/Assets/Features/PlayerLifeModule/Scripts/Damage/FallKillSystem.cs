using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Overboard;
using Mirror;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    public sealed class FallKillSystem : ITickable {
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly IShipFloorReferenceProvider _shipFloorReferenceProvider;
        private readonly IFallKillRule _fallKillRule;
        private readonly IShipOverboardQuery _shipOverboardQuery;

        public FallKillSystem(
            IPlayerBodyRegistry playerBodyRegistry,
            IShipFloorReferenceProvider shipFloorReferenceProvider,
            IFallKillRule fallKillRule,
            IShipOverboardQuery shipOverboardQuery) {
            _playerBodyRegistry = playerBodyRegistry;
            _shipFloorReferenceProvider = shipFloorReferenceProvider;
            _fallKillRule = fallKillRule;
            _shipOverboardQuery = shipOverboardQuery;
        }

        public void Tick() {
            if (NetworkServer.active == false)
                return;

            if (_shipFloorReferenceProvider.TryGetWalkableFloorY(out bool isFlying, out float floorY) == false)
                return;

            IReadOnlyList<PlayerLifeBody> bodies = _playerBodyRegistry.ServerBodies;
            for (int i = 0; i < bodies.Count; i++) {
                PlayerLifeBody body = bodies[i];
                // A drifting player does not fall; the ship may climb away from it. Its own limit kills it.
                if (body.Damageable.IsDead || _shipOverboardQuery.IsOverboard(body.netIdentity))
                    continue;

                if (_fallKillRule.IsBelowKillHeight(body.transform.position.y, floorY, isFlying) == false)
                    continue;

                body.Damageable.ServerKill(DamageType.Fall);
            }
        }
    }
}
