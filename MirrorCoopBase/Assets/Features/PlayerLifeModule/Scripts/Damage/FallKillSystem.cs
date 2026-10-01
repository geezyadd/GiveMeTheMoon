using System.Collections.Generic;
using Features.GameCoreModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    public sealed class FallKillSystem : ITickable, IGameplaySession {
        private readonly PlayerDamageableRegistry _playerDamageableRegistry;
        private readonly ShipRunService _shipRunService;
        private readonly PlayerDamageConfiguration _playerDamageConfiguration;

        public FallKillSystem(
            PlayerDamageableRegistry playerDamageableRegistry,
            ShipRunService shipRunService,
            PlayerDamageConfiguration playerDamageConfiguration) {
            _playerDamageableRegistry = playerDamageableRegistry;
            _shipRunService = shipRunService;
            _playerDamageConfiguration = playerDamageConfiguration;
        }

        public void Tick() {
            if (NetworkServer.active == false)
                return;

            if (_shipRunService.TryGetFallReference(out bool inFlight, out float referenceY) == false)
                return;

            float depth = inFlight
                ? _playerDamageConfiguration.KillDepthBelowDeckInFlight
                : _playerDamageConfiguration.KillDepthBelowStation;
            float killY = referenceY - depth;
            IReadOnlyList<PlayerDamageable> players = _playerDamageableRegistry.Players;
            for (int i = 0; i < players.Count; i++) {
                PlayerDamageable player = players[i];
                if (player == null || player.IsDead)
                    continue;

                if (player.transform.position.y >= killY)
                    continue;

                player.ServerKill(DamageType.Fall);
            }
        }

        public void CleanupGameplay() =>
            _playerDamageableRegistry.ServerRestoreAll();

        public void RestartGameplay() =>
            _playerDamageableRegistry.ServerRestoreAll();
    }
}
