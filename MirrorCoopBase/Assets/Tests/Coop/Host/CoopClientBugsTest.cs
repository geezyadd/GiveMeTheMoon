#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Network;
using Features.DebugModule.Contracts;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.ShopModule.Scripts.Configurations;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // The host sends a bug group through its debug window in a slow cruise; once a bug gnaws the ship, the host puts
    // a pistol into the client's hand (test setup) and the real client process shoots that bug through its fire
    // input. The client shows the bugs with their tail rigs; the server kills the bug from the client's hit and the
    // host's peer sees its death.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientBugsTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const int LOOP = 1;
        private const string SPAWN_LABEL = "Spawn bugs (N)";
        private const string REMOVE_LABEL = "Remove all field agents";
        private const float CRUISE_THROTTLE = 0.15f;
        private const float FULL_THROTTLE = 1f;
        private const char MESSAGE_SEPARATOR = ' ';

        private readonly List<int> _diedOnHost = new();
        private CoopFlightHostHarness _host;
        private IBugDeathEvents _bugDeathEvents;

        [SetUp]
        public void SetUp() =>
            _host = new CoopFlightHostHarness();

        [TearDown]
        public void TearDown() {
            if (_bugDeathEvents != null)
                _bugDeathEvents.OnBugDied -= _diedOnHost.Add;

            _host?.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenABugGnawsTheShipInCruise_AndTheClientShootsIt_ThenTheClientShowsTailedBugsAndTheHostSeesItDie() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_BUGS);
            yield return _host.StartAndBoardCoroutine();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            yield return _host.WaitForTurnDoneCoroutine();
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(CRUISE_THROTTLE);
            yield return SpawnBugsAndWaitForGnawCoroutine();
            yield return ClientShootsBugCoroutine();
            yield return ClickCoroutine(REMOVE_LABEL, null);
            _host.Flight.FaceDestination();
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(FULL_THROTTLE);
            yield return _host.WaitForLandingCoroutine(LOOP);
            yield return _host.FinishCoroutine();
        }

        private IEnumerator SpawnBugsAndWaitForGnawCoroutine() {
            _host.BeginStep("host sends bugs through its debug window");
            yield return ClickCoroutine(SPAWN_LABEL, CoopFlightSteps.BUG_COUNT.ToString(CultureInfo.InvariantCulture));
            BugServerModel bugs = SessionQueries.Resolve<BugServerModel>();
            Assert.AreEqual(CoopFlightSteps.BUG_COUNT, bugs.Count, "The host's debug button did not send the bugs.");
            yield return _host.WaitForCoroutine(() => IsAnyGnawing(bugs), "a bug gnawing the ship");
        }

        private IEnumerator ClientShootsBugCoroutine() {
            _host.BeginStep("client shoots a gnawing bug");
            yield return GivePistolToClientCoroutine();
            _bugDeathEvents = SessionQueries.Resolve<IBugDeathEvents>();
            _bugDeathEvents.OnBugDied += _diedOnHost.Add;
            _host.Command(CoopFlightSteps.COMMAND_SHOOT_BUG);
            yield return _host.WaitForReportCoroutine(CoopFlightSteps.BUG_SHOT);
            string[] parts = _host.LastReport.Message.Split(MESSAGE_SEPARATOR);
            int bugId = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int tails = int.Parse(parts[1], CultureInfo.InvariantCulture);
            Assert.GreaterOrEqual(tails, 1, "The client shows no bug with its tail rig.");
            Assert.IsFalse(SessionQueries.Resolve<BugServerModel>().TryGet(bugId, out BugAgent _), $"The client's shots did not kill bug {bugId} on the server.");
            CollectionAssert.Contains(_diedOnHost, bugId, $"The host's peer did not see bug {bugId} die.");
            Debug.Log($"[CoopTest] the client shot bug {bugId}, it showed {tails} tailed bugs; client screenshot {_host.LastReport.Screenshot}");
        }

        // Test setup, as in CoopTwoProcessTest: the purchase is the loop test's.
        private IEnumerator GivePistolToClientCoroutine() {
            ShipRider client = _host.ClientRider();
            HandWeaponDefinition pistol = SessionQueries.Resolve<ShopCatalog>().HandWeapons[0];
            GameObject item = Object.Instantiate(pistol.ItemPrefab.gameObject, client.transform.position + Vector3.up, client.transform.rotation);
            NetworkServer.Spawn(item);
            yield return null;
            Assert.IsTrue(client.GetComponent<GrabController>().ServerGive(item.GetComponent<Grabbable>()), "The pistol could not be put into the client's hand.");
        }

        private static IEnumerator ClickCoroutine(string label, string value) {
            DebugWindowClicking clicking = new();
            yield return clicking.ClickCoroutine(DebugSections.AGENTS, label, value);
            Assert.IsNull(clicking.Failure, $"Debug window \"{label}\": {clicking.Failure}.");
        }

        private static bool IsAnyGnawing(BugServerModel bugs) {
            foreach (int id in bugs.Ids) {
                if (bugs.TryGet(id, out BugAgent bug) && bug.Stage == BugStage.Gnaw)
                    return true;
            }

            return false;
        }
    }
}
#endif
