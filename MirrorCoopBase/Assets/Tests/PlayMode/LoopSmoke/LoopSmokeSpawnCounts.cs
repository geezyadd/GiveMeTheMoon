#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using Features.PlayerProfileModule.Scripts;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Counts what a run spawned: scene objects of a type and runtime network objects that are not players.
    public static class LoopSmokeSpawnCounts {
        public static int CountSceneObjects<T>() where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;

        public static int CountRuntimeNonPlayerObjects(IEnumerable<NetworkIdentity> spawned) {
            int count = 0;
            foreach (NetworkIdentity identity in spawned) {
                if (IsRuntimeNonPlayer(identity))
                    count++;
            }

            return count;
        }

        public static bool IsRuntimeNonPlayer(NetworkIdentity identity) =>
            identity != null && identity.sceneId == 0 && identity.GetComponent<PlayerProfile>() == null;

        public static void Log(string moment) =>
            Debug.Log($"[LoopSmoke] {moment}: server spawned {NetworkServer.spawned.Count} "
                      + $"(runtime non-player {CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values)}), "
                      + $"client spawned {NetworkClient.spawned.Count} "
                      + $"(runtime non-player {CountRuntimeNonPlayerObjects(NetworkClient.spawned.Values)})");
    }
}
#endif
