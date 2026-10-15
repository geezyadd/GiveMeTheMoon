#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Mirror;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests.PlayMode.LoopSmoke {
    public sealed class LoopSmokeTest {
        private const int TEST_TIMEOUT_MS = 180000;

        private readonly HashSet<uint> _firstRunObjects = new HashSet<uint>();

        private LoopSmokeErrorLog _errorLog;
        private LoopSmokeSteamLobbySpy _steamLobby;
        private LoopSmokeShipRunConfigOverride _configOverride;
        private LoopSmokeSession _session;
        private LoopSmokeFlight _flight;
        private int _lobbyRuntimeObjects;

        [SetUp]
        public void SetUp() {
            _configOverride = new LoopSmokeShipRunConfigOverride(LoopSmokeShipRunConfigOverride.FAST_TIMINGS_JSON);
            _errorLog = new LoopSmokeErrorLog();
            _steamLobby = new LoopSmokeSteamLobbySpy();
            _steamLobby.BindIntoSceneContexts();
            _session = new LoopSmokeSession(_errorLog);
            _flight = new LoopSmokeFlight(_session);
        }

        [TearDown]
        public void TearDown() {
            _configOverride.Dispose();
            _errorLog.Dispose();
            _steamLobby.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostFliesTwoLoops_AndAllDieAndStartAgain_ThenSecondRunStartsCleanAndMenuReturns() {
            // LoopSmokeErrorLog fails the test with the full list and its allow-list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return EnterGameAsHostCoroutine();
            _flight.InstallRequiredEngines();
            yield return _flight.BoardPlayerCoroutine();
            int startStationItems = LoopSmokeSpawnCounts.CountSceneObjects<ShipItem>();
            Assert.Greater(startStationItems, 0, "Run 1 has no items at the start station.");
            yield return new LoopSmokeHandCheck(_session.Ship).ReinstallRadarByHandCoroutine();

            ShipLandingPad startPad = _flight.FindPadUnderShip();
            yield return _flight.FlyLoopCoroutine(1);
            ShipLandingPad firstPad = _flight.FindPadUnderShip();
            yield return _flight.AssertLandedSafelyCoroutine(1, startPad, firstPad);
            int firstLandingSpawns = LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values);
            LoopSmokeSpawnCounts.Log("run 1 after the first landing");
            Assert.Greater(firstLandingSpawns, 0, "Run 1 spawned no pad or drops on landing.");
            yield return new LoopSmokeReviveCheck(_session.Ship).BuyBackCrewCoroutine(firstPad);

            LoopSmokeRejoinCheck rejoin = new LoopSmokeRejoinCheck(_session.Model);
            yield return rejoin.JoinAtStationAndDieCoroutine();
            yield return _flight.FlyLoopCoroutine(2, rejoin.LeaveAndKnockInFlightCoroutine);
            yield return _flight.AssertLandedSafelyCoroutine(2, firstPad, _flight.FindPadUnderShip());
            yield return rejoin.ComeBackAtStationCoroutine();

            RememberFirstRunObjects();
            LoopSmokeSpawnCounts.Log("run 1 before returning to the lobby");
            yield return _session.ReturnToLobbyByAllDeadCoroutine();
            LoopSmokeSpawnCounts.Log("lobby after run 1");
            AssertFirstRunObjectsAreGone();

            yield return _session.StartGameCoroutine();
            _flight.InstallRequiredEngines();
            yield return _flight.BoardPlayerCoroutine();
            Assert.AreEqual(startStationItems, LoopSmokeSpawnCounts.CountSceneObjects<ShipItem>(), "Run 2 does not start with the same station items as run 1.");
            ShipLandingPad secondRunStartPad = _flight.FindPadUnderShip();
            LoopSmokeCrewInviteCheck crewInvite = new LoopSmokeCrewInviteCheck(_steamLobby);
            yield return _flight.FlyLoopCoroutine(1, crewInvite.CheckClosedInFlightCoroutine);
            yield return _flight.AssertLandedSafelyCoroutine(1, secondRunStartPad, _flight.FindPadUnderShip());
            yield return crewInvite.CheckAtStationCoroutine(_flight.FindPadUnderShip());
            LoopSmokeSpawnCounts.Log("run 2 after the first landing");
            Assert.AreEqual(firstLandingSpawns, LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values), "Run 2 does not spawn the same pad and drops on landing as run 1.");
            yield return new LoopSmokeShopCheck().BuyAtKioskAndWalkAwayCoroutine(_flight.FindPadUnderShip());

            yield return _session.LeaveToMenuCoroutine();
            yield return new LoopSmokeRejectCheck().JoinRejectedInFlightCoroutine();
            Assert.AreEqual(0, _errorLog.Count, $"Errors were logged during the loop:\n{_errorLog.Describe()}");
        }

        private IEnumerator EnterGameAsHostCoroutine() {
            yield return _session.EnterLobbyAsHostCoroutine();
            _lobbyRuntimeObjects = LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values);
            LoopSmokeSpawnCounts.Log("lobby before run 1");
            yield return new LoopSmokeCrewInviteCheck(_steamLobby).CheckInLobbyCoroutine();
            yield return _session.StartGameCoroutine();
            _configOverride.AssertBound();
        }

        private void RememberFirstRunObjects() {
            _firstRunObjects.Clear();
            foreach (NetworkIdentity identity in NetworkServer.spawned.Values) {
                if (LoopSmokeSpawnCounts.IsRuntimeNonPlayer(identity))
                    _firstRunObjects.Add(identity.netId);
            }

            Assert.Greater(_firstRunObjects.Count, _lobbyRuntimeObjects, "Run 1 spawned no runtime objects: the leak check would prove nothing.");
        }

        private void AssertFirstRunObjectsAreGone() {
            List<string> leftovers = new List<string>();
            CollectFirstRunLeftovers(NetworkServer.spawned.Values, "server", leftovers);
            CollectFirstRunLeftovers(NetworkClient.spawned.Values, "client", leftovers);
            Assert.IsEmpty(leftovers, $"Run 1 objects survived the return to the lobby: {string.Join(", ", leftovers)}");
            Assert.AreEqual(_lobbyRuntimeObjects, LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkServer.spawned.Values), "Server runtime objects in the lobby differ from before run 1.");
            Assert.AreEqual(_lobbyRuntimeObjects, LoopSmokeSpawnCounts.CountRuntimeNonPlayerObjects(NetworkClient.spawned.Values), "Client runtime objects in the lobby differ from before run 1.");
            Assert.AreEqual(0, LoopSmokeSpawnCounts.CountSceneObjects<ShipLandingPad>(), "Landing pads survived the return to the lobby.");
            Assert.AreEqual(0, LoopSmokeSpawnCounts.CountSceneObjects<ShipItem>(), "Ship items survived the return to the lobby.");
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
