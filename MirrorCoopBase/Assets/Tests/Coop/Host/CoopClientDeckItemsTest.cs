#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // Items aboard with a real second process (PR #65): the host drops an item on the deck at the station, the client
    // drops another one by hand and carries a third aboard, lets it go in the cruise; all three lie on the deck on both
    // peers in the cruise, after a hard turn (where they rode along) and after the landing.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientDeckItemsTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const int LOOP = 1;
        private const float GAP_TOLERANCE = 0.05f;
        // An item's lowest point resting on the deck or on another thing lying on it (m).
        private const float MAX_REST_GAP = 0.35f;
        private const float MIN_WALK_METERS = 0.5f;
        private const float RIDE_DRIFT_TOLERANCE = 0.02f;
        private const float TURN_SECONDS = 2f;
        private const float LEVEL_TIMEOUT_SECONDS = 6f;
        private const float LEVEL_DOT = 0.9999f;
        // The client sees items at a station through their network bodies a moment behind the server.
        private const float CLIENT_CATCH_UP_SECONDS = 1f;

        private readonly WaitForSeconds _turn = new WaitForSeconds(TURN_SECONDS);
        private readonly WaitForSeconds _clientCatchUp = new WaitForSeconds(CLIENT_CATCH_UP_SECONDS);
        private readonly List<Grabbable> _items = new List<Grabbable>();
        private readonly ShipDeckItemHandling _handling = new ShipDeckItemHandling();

        private CoopHostHarness _host;

        private ShipBase Ship => _host.Ship;

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness();

        [TearDown]
        public void TearDown() =>
            _host?.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientDropsAndCarriesItemsAboard_AndShipFliesTurnsAndLands_ThenItemsRestOnDeckOnBothPeers() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_DECK_ITEMS);
            yield return _host.StartAndBoardCoroutine();
            yield return HostDropsItemCoroutine();
            yield return WatchClientDropAndCarryCoroutine();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            AssertItemsLieOnBoth("after the takeoff");
            yield return WatchClientReleaseInCruiseCoroutine();
            yield return TurnHardCoroutine();
            _host.Flight.FaceDestination();
            yield return _host.WaitForLandingCoroutine(LOOP);
            yield return CompareAfterLandingCoroutine();
            yield return _host.FinishCoroutine();
        }

        private IEnumerator HostDropsItemCoroutine() {
            _host.BeginStep("host drops an item on the deck");
            Grabbable[] items = LoopSmokeDeckItemCheck.FindLooseBoxItems(Ship);
            Assert.IsNotEmpty(items, "No loose item at the first station to drop on the deck.");
            yield return LoopSmokeDeckItemCheck.DropOnDeckByHandCoroutine(Ship, items, _items.Add);
            // Off the side the client drops its items on.
            yield return _host.Flight.BoardPlayerCoroutine();
        }

        private IEnumerator WatchClientDropAndCarryCoroutine() {
            _host.BeginStep("client drops an item on the deck");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_DROP_ITEM, CoopFlightSteps.ITEM_DROPPED);
            Grabbable dropped = FindItem(_host.LastReport.DroppedItemId);
            yield return WaitForRestCoroutine(dropped);
            _items.Add(dropped);
            AssertItemsLieOnBoth("at the station after the client's drop");

            _host.BeginStep("client carries an item aboard");
            yield return _host.WaitForReportCoroutine(CoopFlightSteps.ITEM_CARRIED);
            uint carried = _host.LastReport.HeldItemId;
            Assert.AreNotEqual(0u, carried, "The client reports no item in its hand.");
            Grabbable held = _host.ClientRider().GetComponent<GrabController>().Held;
            Assert.IsNotNull(held, "The client's hand is empty on the server.");
            Assert.AreEqual(carried, held.netId, "The server sees another item in the client's hand than the client does.");
            Assert.IsTrue(_host.IsClientOnDeckOnServer(), $"The client carrying an item is not on the deck: {_host.ClientServerPosition()}.");
        }

        private IEnumerator WatchClientReleaseInCruiseCoroutine() {
            _host.BeginStep("client lets the carried item go in cruise");
            Grabbable carried = _host.ClientRider().GetComponent<GrabController>().Held;
            Assert.IsNotNull(carried, "The client lost the carried item before the cruise.");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_RELEASE_ITEM, CoopFlightSteps.ITEM_RELEASED);
            _items.Add(carried);
            Assert.IsFalse(_host.ClientRider().GetComponent<GrabController>().IsHolding, "The client's hand still holds the item on the server.");
            Assert.Greater(_host.LastReport.WalkedMeters, MIN_WALK_METERS, "The client did not walk on after letting the item go.");
            AssertItemsLieOnBoth("after the client let the carried one go in cruise");
            _host.AssertSameView(true);
        }

        // The pilot's stick hard to one side: the ship turns and banks, then levels out again.
        private IEnumerator TurnHardCoroutine() {
            _host.BeginStep("host turns the ship hard");
            List<Vector3> before = ShipLocalPositions();
            Ship.FlightControlDebug.SetDebugSteer(true, 1f);
            yield return _turn;
            Ship.FlightControlDebug.SetDebugSteer(false, 0f);
            yield return _host.Session.WaitForCoroutine(IsShipLevel, LEVEL_TIMEOUT_SECONDS, "the ship to level out after the turn");
            List<Vector3> after = ShipLocalPositions();
            for (int i = 0; i < _items.Count; i++) {
                float drift = Vector3.Distance(before[i], after[i]);
                Assert.Less(drift, RIDE_DRIFT_TOLERANCE, $"{_items[i].name} moved {drift:F3} m on the deck during the turn.");
            }

            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_ITEMS_TURNED, CoopFlightSteps.ITEMS_TURNED);
            AssertItemsLieOnBoth("after a hard turn");
            _host.AssertSameView(true);
        }

        private IEnumerator CompareAfterLandingCoroutine() {
            _host.BeginStep("items after the landing");
            foreach (Grabbable item in _items)
                yield return WaitForRestCoroutine(item);

            yield return _clientCatchUp;
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_ITEMS_LANDED, CoopFlightSteps.ITEMS_LANDED);
            AssertItemsLieOnBoth("after the landing");
            foreach (Grabbable item in _items) {
                Assert.IsFalse(item.GetComponent<Rigidbody>().isKinematic, $"{item.name} stays kinematic after the landing.");
                Assert.IsTrue(item.CanBeGrabbed, $"{item.name} cannot be picked up after the landing.");
            }
        }

        // Every tracked item over the deck on both peers, resting on the deck or on what lies on it, at the same height
        // above the deck surface on both (a module's box need not reach down to the deck).
        private void AssertItemsLieOnBoth(string when) {
            CoopObservation host = CoopObserver.CaptureGame(_host.LastReport.Step);
            CoopObservation client = _host.LastReport;
            foreach (Grabbable item in _items) {
                string what = $"{item.name} (on the host at ship-local {Ship.transform.InverseTransformPoint(item.transform.position)}) {when}";
                float hostGap = FindGap(host, item.netId, $"{what} on the host");
                float clientGap = FindGap(client, item.netId, $"{what} on the client");
                float lowest = MeasureLowestPointAboveDeck(item);
                Assert.Less(Mathf.Abs(lowest), MAX_REST_GAP, $"{what}: its lowest point is {lowest:0.000} m above the deck on the host.");
                Assert.Less(Mathf.Abs(hostGap - clientGap), GAP_TOLERANCE, $"{what}: its box {hostGap:0.000} m above the deck on the host, {clientGap:0.000} m on the client.");
            }
        }

        // The lowest point of the item's solid colliders over the deck surface under it, in ship space: a module's box
        // collider may cover only its upper part.
        private float MeasureLowestPointAboveDeck(Grabbable item) {
            float lowest = float.PositiveInfinity;
            foreach (Collider collider in item.GetComponentsInChildren<Collider>()) {
                if (collider.isTrigger == false)
                    lowest = Mathf.Min(lowest, Ship.transform.InverseTransformPoint(collider.bounds.min).y);
            }

            Vector3 local = Ship.transform.InverseTransformPoint(item.transform.position);
            Assert.IsTrue(Ship.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY), $"No deck surface under {item.name} at ship-local {local}.");
            return lowest - surfaceY;
        }

        private static float FindGap(CoopObservation observation, uint netId, string what) {
            int index = Array.IndexOf(observation.DeckItemIds, netId);
            Assert.GreaterOrEqual(index, 0, $"{what}: not over the deck or held. Deck items: [{observation.DescribeDeckItems()}].");
            return observation.DeckItemGaps[index];
        }

        private IEnumerator WaitForRestCoroutine(Grabbable item) {
            yield return _handling.WaitForRestCoroutine(item);
            Assert.IsNull(_handling.Failure, _handling.Failure);
        }

        private static Grabbable FindItem(uint netId) {
            Assert.IsTrue(NetworkServer.spawned.TryGetValue(netId, out NetworkIdentity identity), $"No item #{netId} on the server.");
            return identity.GetComponent<Grabbable>();
        }

        private List<Vector3> ShipLocalPositions() {
            List<Vector3> positions = new List<Vector3>();
            foreach (Grabbable item in _items)
                positions.Add(Ship.transform.InverseTransformPoint(item.transform.position));

            return positions;
        }

        private bool IsShipLevel() =>
            Vector3.Dot(Ship.transform.up, Vector3.up) > LEVEL_DOT;
    }
}
#endif
