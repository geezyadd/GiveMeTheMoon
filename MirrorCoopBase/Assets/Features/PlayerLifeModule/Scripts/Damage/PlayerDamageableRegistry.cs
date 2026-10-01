using System.Collections.Generic;

namespace Features.PlayerLifeModule.Scripts {
    public sealed class PlayerDamageableRegistry {
        private readonly List<PlayerDamageable> _players = new List<PlayerDamageable>();

        public IReadOnlyList<PlayerDamageable> Players =>
            _players;

        public void Register(PlayerDamageable player) {
            if (_players.Contains(player))
                return;

            _players.Add(player);
        }

        public void Unregister(PlayerDamageable player) {
            _players.Remove(player);
        }

        public void ServerRestoreAll() {
            for (int i = _players.Count - 1; i >= 0; i--) {
                PlayerDamageable player = _players[i];
                if (player == null) {
                    _players.RemoveAt(i);
                    continue;
                }

                player.ServerRestore();
            }
        }
    }
}
