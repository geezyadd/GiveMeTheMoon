#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Mirror;
using Tests.PlayMode.LoopSmoke;

namespace Tests.PlayMode.LoopSoak {
    // Runtime network objects that are not players, split by kind. Items are the free station drops (and the start
    // station's items): they are never removed, so they grow by design and are counted apart from the rest.
    public readonly struct LoopSoakSpawnCensus {
        public int ServerSpawned { get; }
        public int ClientSpawned { get; }
        public int Items { get; }
        public int Pads { get; }
        public int ServerOther { get; }
        public int ClientOther { get; }

        private LoopSoakSpawnCensus(int serverSpawned, int clientSpawned, int items, int pads, int serverOther, int clientOther) {
            ServerSpawned = serverSpawned;
            ClientSpawned = clientSpawned;
            Items = items;
            Pads = pads;
            ServerOther = serverOther;
            ClientOther = clientOther;
        }

        public static LoopSoakSpawnCensus Create() {
            int items = CountRuntime<ShipItem>(NetworkServer.spawned.Values);
            int pads = CountRuntime<ShipLandingPad>(NetworkServer.spawned.Values);
            int serverRuntime = LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values);
            int clientItems = CountRuntime<ShipItem>(NetworkClient.spawned.Values);
            int clientPads = CountRuntime<ShipLandingPad>(NetworkClient.spawned.Values);
            int clientRuntime = LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkClient.spawned.Values);
            return new LoopSoakSpawnCensus(
                NetworkServer.spawned.Count,
                NetworkClient.spawned.Count,
                items,
                pads,
                serverRuntime - items - pads,
                clientRuntime - clientItems - clientPads);
        }

        private static int CountRuntime<T>(IEnumerable<NetworkIdentity> spawned) {
            int count = 0;
            foreach (NetworkIdentity identity in spawned) {
                if (LoopSmokeSpawnCounts.IsRuntimeNonPlayer(identity) && identity.TryGetComponent(out T _))
                    count++;
            }

            return count;
        }
    }
}
#endif
