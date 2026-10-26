#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Mirror;
using NUnit.Framework;

namespace Tests.PlayMode.LoopSmoke {
    // A second run starts clean: no object of the first run survives its return to the lobby, the lobby has as many
    // runtime objects as before it, and run 2 starts with the same station items and spawns the same pad and drops on
    // its first landing as run 1.
    public sealed class LoopSmokeRunLeakCheck {
        private readonly HashSet<uint> _firstRunObjects = new HashSet<uint>();
        private int _lobbyRuntimeObjects;
        private int _startStationItems;
        private int _firstLandingSpawns;

        [LoopSmokeStep(LoopSmokePhase.InLobby, 1, 0)]
        private void CountLobbyObjects() {
            _lobbyRuntimeObjects = LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values);
            LoopSmokeSpawnCounts.Log("lobby before run 1");
        }

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 1, 1, 0)]
        private void CountStartStationItems() {
            _startStationItems = LoopSmokeSpawnCounts.CountSceneObjects<ShipItem>();
            Assert.Greater(_startStationItems, 0, "Run 1 has no items at the start station.");
        }

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 1, 1, 0)]
        private void CountFirstLandingSpawns() {
            _firstLandingSpawns = LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values);
            LoopSmokeSpawnCounts.Log("run 1 after the first landing");
            Assert.Greater(_firstLandingSpawns, 0, "Run 1 spawned no pad or drops on landing.");
        }

        // Last at the last station of run 1, right before everyone dies.
        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 1, 2, 100)]
        private void RememberFirstRunObjects() {
            _firstRunObjects.Clear();
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values) {
                if (LoopSmokeSpawnCounts.IsRuntimeNonPlayer(identity))
                    _firstRunObjects.Add(identity.netId);
            }

            Assert.Greater(_firstRunObjects.Count, _lobbyRuntimeObjects, "Run 1 spawned no runtime objects: the leak check would prove nothing.");
            LoopSmokeSpawnCounts.Log("run 1 before returning to the lobby");
        }

        [LoopSmokeStep(LoopSmokePhase.AfterRun, 1, 0)]
        private void AssertFirstRunObjectsAreGone() {
            LoopSmokeSpawnCounts.Log("lobby after run 1");
            List<string> leftovers = new List<string>();
            CollectFirstRunLeftovers(NetworkServer.spawned.Values, "server", leftovers);
            CollectFirstRunLeftovers(NetworkClient.spawned.Values, "client", leftovers);
            Assert.IsEmpty(leftovers, $"Run 1 objects survived the return to the lobby: {string.Join(", ", leftovers)}");
            Assert.AreEqual(_lobbyRuntimeObjects, LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values), "Server runtime objects in the lobby differ from before run 1.");
            Assert.AreEqual(_lobbyRuntimeObjects, LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkClient.spawned.Values), "Client runtime objects in the lobby differ from before run 1.");
            Assert.AreEqual(0, LoopSmokeSpawnCounts.CountSceneObjects<ShipLandingPad>(), "Landing pads survived the return to the lobby.");
            Assert.AreEqual(0, LoopSmokeSpawnCounts.CountSceneObjects<ShipItem>(), "Ship items survived the return to the lobby.");
        }

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 2, 1, 0)]
        private void AssertSameStartStationItems() =>
            Assert.AreEqual(_startStationItems, LoopSmokeSpawnCounts.CountSceneObjects<ShipItem>(), "Run 2 does not start with the same station items as run 1.");

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 2, 1, 0)]
        private void AssertSameFirstLandingSpawns() {
            LoopSmokeSpawnCounts.Log("run 2 after the first landing");
            Assert.AreEqual(_firstLandingSpawns, LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values), "Run 2 does not spawn the same pad and drops on landing as run 1.");
        }

        private void CollectFirstRunLeftovers(IEnumerable<NetworkIdentity> spawned, string side, List<string> leftovers) {
            foreach (NetworkIdentity identity in spawned) {
                if (identity != null && _firstRunObjects.Contains(identity.netId))
                    leftovers.Add($"{side}:{identity.name}#{identity.netId}");
            }
        }
    }
}
#endif
