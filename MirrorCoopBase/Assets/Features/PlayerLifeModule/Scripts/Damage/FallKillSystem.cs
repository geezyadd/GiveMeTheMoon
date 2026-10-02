using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Mirror;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    public sealed class FallKillSystem : ITickable {
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly IShipFloorReferenceProvider _shipFloorReferenceProvider;
        private readonly IFallKillRule _fallKillRule;

        public FallKillSystem(
            IPlayerBodyRegistry playerBodyRegistry,
            IShipFloorReferenceProvider shipFloorReferenceProvider,
            IFallKillRule fallKillRule) {
            _playerBodyRegistry = playerBodyRegistry;
            _shipFloorReferenceProvider = shipFloorReferenceProvider;
            _fallKillRule = fallKillRule;
        }

        public void Tick() {
            if (NetworkServer.active == false)
                return;

            if (_shipFloorReferenceProvider.TryGetWalkableFloorY(out bool isFlying, out float floorY) == false)
                return;

            IReadOnlyList<PlayerLifeBody> bodies = _playerBodyRegistry.ServerBodies;
            for (int i = 0; i < bodies.Count; i++) {
                PlayerLifeBody body = bodies[i];
                if (body.Damageable.IsDead)
                    continue;

                if (_fallKillRule.IsBelowKillHeight(body.transform.position.y, floorY, isFlying) == false)
                    continue;

                body.Damageable.ServerKill(DamageType.Fall);
            }
        }
    }
}
