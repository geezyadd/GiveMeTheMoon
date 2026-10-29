#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Globalization;
using Features.BugModule.Scripts.Brain;
using Features.DebugModule.Contracts;
using Features.FieldAgentModule.Scripts.Client;
using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Features.ParcelModule.Scripts.Services;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // The real client process takes a parcel at the start station's delivery stand with E and puts its crate on the
    // deck: the host's server and peer have the parcel and its crate on board. In a slow cruise the host's debug window
    // sends a bug to steal it: the server names the thief, the host's peer and the client's both show the crate hanging
    // under it. The thief removed well away from the ship, the crate drifts and collides again on the client.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientParcelTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const int LOOP = 1;
        private const string STEAL_LABEL = "A bug steals the parcel";
        private const string REMOVE_LABEL = "Remove all field agents";
        private const float CRUISE_THROTTLE = 0.15f;
        private const float FULL_THROTTLE = 1f;
        private const float CRATE_UNDER_THIEF_METERS = 3f;
        private const char MESSAGE_SEPARATOR = ' ';
        private const float DRIFT_TIMEOUT_SECONDS = 5f;
        private const float THIEF_AWAY_METERS = 20f;

        private CoopFlightHostHarness _host;
        private uint _parcelNetId;

        private static IReadOnlyParcelsModel Parcels => SessionQueries.Resolve<IReadOnlyParcelsModel>();

        [SetUp]
        public void SetUp() =>
            _host = new CoopFlightHostHarness();

        [TearDown]
        public void TearDown() =>
            _host?.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenTheClientTakesAParcel_AndABugStealsItInCruise_ThenTheHostSeesTheParcelAndBothSeeTheTheft() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_PARCEL);
            yield return _host.StartAndBoardCoroutine(ClientTakesParcelCoroutine);
            AssertParcelOnBoard();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            yield return _host.WaitForTurnDoneCoroutine();
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(CRUISE_THROTTLE);
            yield return BugStealsParcelCoroutine();
            yield return ThiefGoneCoroutine();
            _host.Flight.FaceDestination();
            SessionQueries.Resolve<IShipThrottleDebug>().DebugSetThrottle(FULL_THROTTLE);
            yield return _host.WaitForLandingCoroutine(LOOP);
            yield return _host.FinishCoroutine();
        }

        // A station offers 0..2 parcels at random: the server is asked for new offers until there is one (test setup).
        private IEnumerator ClientTakesParcelCoroutine() {
            _host.BeginStep("client takes a parcel at the stand");
            IParcelOfferService offers = SessionQueries.Resolve<IParcelOfferService>();
            yield return _host.WaitForCoroutine(() => Parcels.IsAvailable, "the synced parcel model");
            while (Parcels.Offers.Count == 0) {
                offers.ServerCreateOffers(_host.Model.LoopIndex);
                yield return null;
            }

            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_TAKE_PARCEL, CoopFlightSteps.PARCEL_TAKEN);
            _parcelNetId = uint.Parse(_host.LastReport.Message, CultureInfo.InvariantCulture);
            Assert.AreEqual(1, Parcels.Entries.Count, "The host's model does not have the parcel the client took.");
            Assert.AreEqual(_parcelNetId, Parcels.Entries[0].NetId, "The client took another parcel than the host's model has.");
            Assert.AreEqual(_host.Model.LoopIndex + 1, Parcels.Entries[0].DestinationLoop, "The client's parcel does not go to the next station.");
            Debug.Log($"[CoopTest] the client took parcel {_parcelNetId}; client screenshot {_host.LastReport.Screenshot}");
        }

        private void AssertParcelOnBoard() {
            Assert.IsTrue(NetworkServer.spawned.TryGetValue(_parcelNetId, out NetworkIdentity crate), $"The host has no crate {_parcelNetId}.");
            Assert.AreEqual(ParcelState.Loose, Parcels.Entries[0].State, "The client's parcel is not a loose crate.");
            Assert.IsTrue(SessionQueries.Resolve<IParcelAboardService>().IsAboard(crate.transform.position, _host.Ship), $"The client's crate is not on board on the host ({crate.transform.position}).");
        }

        private IEnumerator BugStealsParcelCoroutine() {
            _host.BeginStep("a bug steals the parcel");
            yield return ClickCoroutine(STEAL_LABEL);
            yield return _host.WaitForCoroutine(() => Parcels.Entries.Count == 1 && Parcels.Entries[0].State == ParcelState.Carried, "the parcel carried off on the server");
            int thief = Parcels.Entries[0].CarrierAgentId;
            yield return _host.WaitForCoroutine(() => IsShownUnderThief(thief), "the host's peer showing the crate under its thief");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_THEFT, CoopFlightSteps.PARCEL_STOLEN);
            string[] parts = _host.LastReport.Message.Split(MESSAGE_SEPARATOR);
            Assert.AreEqual(_parcelNetId, uint.Parse(parts[0], CultureInfo.InvariantCulture), "The client reports another parcel stolen.");
            Assert.AreEqual(thief, int.Parse(parts[1], CultureInfo.InvariantCulture), "The client shows another thief than the server's.");
            Assert.Less(float.Parse(parts[2], CultureInfo.InvariantCulture), CRATE_UNDER_THIEF_METERS, "The client does not show the crate under its thief.");
            Debug.Log($"[CoopTest] both peers show parcel {_parcelNetId} under thief {thief}; client screenshot {_host.LastReport.Screenshot}");
        }

        private IEnumerator ThiefGoneCoroutine() {
            _host.BeginStep("the thief is removed, the crate drifts");
            // Removed right beside the ship, the thief would drop the crate where the ship flies into it at once.
            yield return _host.WaitForCoroutine(IsThiefAway, $"the thief {THIEF_AWAY_METERS} m away from the ship");
            string before = DescribeParcels();
            yield return ClickCoroutine(REMOVE_LABEL);
            float deadline = Time.time + DRIFT_TIMEOUT_SECONDS;
            while (Parcels.Entries.Count != 1 || Parcels.Entries[0].State != ParcelState.Drifting) {
                Assert.IsTrue(Time.time <= deadline, $"The parcel does not drift on the server {DRIFT_TIMEOUT_SECONDS} s after its thief was removed: before {before}, now {DescribeParcels()}.");
                yield return null;
            }

            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_DRIFT, CoopFlightSteps.PARCEL_DRIFTING);
            Assert.AreEqual(bool.TrueString, _host.LastReport.Message, "The drifting crate does not collide on the client after its thief let it go.");
        }

        private bool IsThiefAway() =>
            NetworkServer.spawned.TryGetValue(_parcelNetId, out NetworkIdentity crate)
            && Vector3.Distance(crate.transform.position, _host.Ship.transform.position) > THIEF_AWAY_METERS;

        private static string DescribeParcels() {
            string parcels = $"{SessionQueries.Resolve<BugServerModel>().Count} bugs;";
            foreach (ParcelEntry entry in Parcels.Entries) {
                parcels += $" {entry.NetId}:{entry.State}";
                if (NetworkServer.spawned.TryGetValue(entry.NetId, out NetworkIdentity crate))
                    parcels += $" {Vector3.Distance(crate.transform.position, SessionQueries.Resolve<IShipRunBindingModel>().Ship.transform.position):0.0} m from the ship";
            }

            return parcels;
        }

        private bool IsShownUnderThief(int thief) =>
            NetworkServer.spawned.TryGetValue(_parcelNetId, out NetworkIdentity crate)
            && SessionQueries.Resolve<IFieldAgentShownAgents>().TryGetShownWorldPosition(thief, out Vector3 shown)
            && Vector3.Distance(crate.transform.position, shown) < CRATE_UNDER_THIEF_METERS;

        private static IEnumerator ClickCoroutine(string label) {
            DebugWindowClicking clicking = new();
            yield return clicking.ClickCoroutine(DebugSections.AGENTS, label, null);
            Assert.IsNull(clicking.Failure, $"Debug window \"{label}\": {clicking.Failure}.");
        }
    }
}
#endif
