using System.Collections.Generic;
using Game.Connection;
using Mirror;
using UnityEngine;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public sealed class SpectatorTargetService : ISpectatorTargetService {
        private const string SPECTATING_PREFIX = "Spectating ";
        private const string SPECTATING_HINT = "   [A] / [D]";
        private const string NO_TARGETS_LABEL = "Everyone is dead";
        private const string PLAYER_PREFIX = "Player ";

        public void CollectTargets(IReadOnlyList<Transform> alivePlayerTargets, List<Transform> result) {
            result.Clear();
            for (int i = 0; i < alivePlayerTargets.Count; i++) {
                Transform target = alivePlayerTargets[i];
                if (target == null || BelongsToLocalPlayer(target))
                    continue;

                result.Add(target);
            }
        }

        public int ResolveIndex(IReadOnlyList<Transform> targets, Transform current, int currentIndex) {
            int kept = IndexOf(targets, current);
            if (kept >= 0)
                return kept;

            return currentIndex < targets.Count ? currentIndex : 0;
        }

        public int Step(int index, int direction, int count) {
            int next = (index + direction) % count;
            return next < 0 ? next + count : next;
        }

        public string BuildTargetLabel(Transform target, int index) =>
            SPECTATING_PREFIX + ResolveDisplayName(target, index) + SPECTATING_HINT;

        public string BuildNoTargetsLabel() =>
            NO_TARGETS_LABEL;

        private static bool BelongsToLocalPlayer(Transform target) {
            NetworkIdentity identity = target.GetComponentInParent<NetworkIdentity>();
            return identity != null && identity.isLocalPlayer;
        }

        private static string ResolveDisplayName(Transform target, int index) =>
            TryGetSyncedName(target, out string playerName)
                ? playerName
                : PLAYER_PREFIX + (index + 1).ToString();

        private static bool TryGetSyncedName(Transform target, out string playerName) {
            playerName = null;
            ConnectionPlayerName playerNameSource = target.GetComponentInParent<ConnectionPlayerName>();
            if (playerNameSource == null || string.IsNullOrWhiteSpace(playerNameSource.DisplayName))
                return false;

            playerName = playerNameSource.DisplayName;
            return true;
        }

        private static int IndexOf(IReadOnlyList<Transform> targets, Transform target) {
            if (target == null)
                return -1;

            for (int i = 0; i < targets.Count; i++) {
                if (targets[i] == target)
                    return i;
            }

            return -1;
        }
    }
}
