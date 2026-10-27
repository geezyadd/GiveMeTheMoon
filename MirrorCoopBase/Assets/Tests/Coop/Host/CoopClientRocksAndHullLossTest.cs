#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.ShipImpactModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Host {
    // Rocks with the client riding, a real second process: the host flies into a small rock (it breaks and damages)
    // and a large one (it knocks the ship back); the client gets the same impacts, its view shakes, and it sees the same
    // hull, breaches and broken modules. A near pass inside a rock's data sphere but clear of its contact gives no
    // impact and no damage on either peer. Then the hull is lost in that cruise: both players end up in the lobby, free.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientRocksAndHullLossTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const int LOOP = 1;
        // The near pass puts the hull's side this share of the way from the rock's contact sphere to its data sphere.
        private const float NEAR_PASS_GAP_SHARE = 0.75f;
        private const float PASS_SECONDS = 1.5f;
        // The hull's distance is read once a frame, the server checks every physics step in between (m).
        private const float STEP_TOLERANCE_METERS = 0.5f;
        // The client's hull a moment behind the host's while open breaches leak (hull points).
        private const float HULL_TOLERANCE = 2f;
        private const float CLIENT_CATCH_UP_SECONDS = 1f;
        // As LoopSmokeHullLossCheck: the lobby arrival settles before the player walks.
        private const float LOBBY_SETTLE_SECONDS = 2f;
        private const float MIN_WALK_METERS = 1f;

        private readonly WaitForSeconds _clientCatchUp = new WaitForSeconds(CLIENT_CATCH_UP_SECONDS);
        private readonly WaitForSeconds _lobbySettle = new WaitForSeconds(LOBBY_SETTLE_SECONDS);
        private readonly List<ShipRockImpact> _hostImpacts = new List<ShipRockImpact>();
        private readonly List<FieldRockDestroyed> _destroyed = new List<FieldRockDestroyed>();

        private CoopHostHarness _host;
        private LoopSmokeContext _context;

        private ShipBase Ship => _host.Ship;

        [SetUp]
        public void SetUp() =>
            _host = new CoopHostHarness();

        [TearDown]
        public void TearDown() =>
            _host?.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenShipHitsRocksWithClientAboard_AndHullIsLostInFlight_ThenClientSeesTheSameDamageAndBothEndFreeInLobby() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_ROCKS_AND_HULL_LOSS);
            yield return _host.StartAndBoardCoroutine();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            _context = new LoopSmokeContext(_host.Session, null);
            _context.CreateFlight();
            yield return HitRocksWithClientAboardCoroutine();
            yield return PassRockWithClientAboardCoroutine();
            yield return LoseHullWithClientAboardCoroutine();
            yield return _host.FinishCoroutine();
        }

        private IEnumerator HitRocksWithClientAboardCoroutine() {
            _host.BeginStep("host flies into a small and a large rock with the client aboard");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_WATCH_IMPACTS, CoopFlightSteps.WATCHING);
            IShipImpactEvents impactEvents = SessionQueries.Resolve<IShipImpactEvents>();
            _hostImpacts.Clear();
            impactEvents.OnImpact += _hostImpacts.Add;
            try {
                yield return new LoopSmokeRockImpactCheck(_context).HitSmallAndLargeRockInCruiseCoroutine();
                _host.FaceAwayFromStation();
                yield return _clientCatchUp;
            } finally {
                impactEvents.OnImpact -= _hostImpacts.Add;
            }

            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_IMPACTS, CoopFlightSteps.IMPACTS);
            CoopObservation client = _host.LastReport;
            CollectionAssert.Contains(client.ImpactKinds, nameof(ShipRockImpactKind.Small), $"No small rock impact reached the client: {client.DescribeFlight()}");
            CollectionAssert.Contains(client.ImpactKinds, nameof(ShipRockImpactKind.Large), $"No large rock impact reached the client: {client.DescribeFlight()}");
            Assert.AreEqual(_hostImpacts.Count, client.ImpactKinds.Length, $"The client got {client.ImpactKinds.Length} rock impacts, the host {_hostImpacts.Count}.");
            Assert.AreEqual(client.ImpactKinds.Length, client.ShakenImpacts, "Not every rock impact shook the riding client's view.");
            Assert.IsTrue(client.IsRiding, "The client stopped riding the ship on the rock hits.");
            AssertSameDamage("after the rock hits");
            _host.AssertSameView(true);
            Debug.Log($"[CoopTest] the client's rock impacts: [{string.Join(", ", client.ImpactKinds)}], all shook its view; screenshot {client.Screenshot}");
        }

        private IEnumerator PassRockWithClientAboardCoroutine() {
            _host.BeginStep("host passes a small rock close by with the client aboard");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_WATCH_NEAR_PASS, CoopFlightSteps.WATCHING);
            CoopObservation before = CoopFlightObserver.CaptureFlight(CoopFlightSteps.WATCHING);
            Vector3 start = Ship.transform.position;
            LoopSmokeRockNearPassCheck nearPass = new LoopSmokeRockNearPassCheck(_context);
            Assert.IsTrue(nearPass.TryFindSmallRock(start, out FieldRockDescription rock), "Near pass: no lone small rock far enough from the ship.");
            float contactRadius = SessionQueries.Resolve<IShipRockContactRule>().CalculateContactRadius(ShipRockImpactKind.Small, rock.Radius);
            float sideGap = Mathf.Lerp(contactRadius, rock.Radius, NEAR_PASS_GAP_SHARE);
            IShipImpactEvents impactEvents = SessionQueries.Resolve<IShipImpactEvents>();
            IFieldRockEvents rockEvents = SessionQueries.Resolve<IFieldRockEvents>();
            float nearest = float.MaxValue;
            _hostImpacts.Clear();
            _destroyed.Clear();
            impactEvents.OnImpact += _hostImpacts.Add;
            rockEvents.RockDestroyed += _destroyed.Add;
            try {
                _host.Flight.PutShipBeside(rock.Center, LoopSmokeRockNearPassCheck.ReadHullHalfWidth(Ship) + sideGap, 0f);
                yield return null;
                for (float time = 0f; time < PASS_SECONDS; time += Time.deltaTime) {
                    nearest = Mathf.Min(nearest, LoopSmokeRockNearPassCheck.ReadHullDistance(Ship, rock.Center));
                    yield return null;
                }

                yield return _host.Flight.PutShipBackCoroutine(start);
                _host.FaceAwayFromStation();
                yield return _clientCatchUp;
            } finally {
                impactEvents.OnImpact -= _hostImpacts.Add;
                rockEvents.RockDestroyed -= _destroyed.Add;
            }

            Debug.Log($"[CoopTest] near pass by {rock.Id} ({rock.Diameter:0.0} m): hull {nearest:0.0} m from its middle at the nearest, contact radius {contactRadius:0.0} m, data radius {rock.Radius:0.0} m");
            Assert.Less(nearest, rock.Radius, $"Near pass: the hull never came inside the data sphere of {rock.Id}.");
            Assert.Greater(nearest, contactRadius + STEP_TOLERANCE_METERS, $"Near pass: the hull came within the contact sphere of {rock.Id} ({nearest:0.0} m, contact radius {contactRadius:0.0} m): no clear pass to check.");
            Assert.IsEmpty(_hostImpacts, "Near pass: the host got a rock impact without a contact.");
            Assert.IsFalse(_destroyed.Exists(destroyed => destroyed.Id.Equals(rock.Id)), $"Near pass: {rock.Id} broke without a contact.");

            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_NEAR_PASS, CoopFlightSteps.IMPACTS);
            CoopObservation client = _host.LastReport;
            Assert.IsEmpty(client.ImpactKinds, $"Near pass: the client got rock impacts without a contact: {client.DescribeFlight()}");
            Assert.AreEqual(0, client.ShakenImpacts, "Near pass: the client's view shook without a contact.");
            CoopObservation after = CoopFlightObserver.CaptureFlight(CoopFlightSteps.IMPACTS);
            CollectionAssert.AreEqual(before.BrokenSockets, after.BrokenSockets, "Near pass: a module broke without a contact.");
            CollectionAssert.AreEqual(before.OpenBreaches, after.OpenBreaches, "Near pass: a breach opened without a contact.");
            AssertSameDamage("after the near pass");
        }

        private IEnumerator LoseHullWithClientAboardCoroutine() {
            _host.BeginStep("the hull is lost in flight with the client aboard");
            Assert.IsTrue(_host.ClientRider().IsRiding, "The client does not ride the ship before the hull loss.");
            yield return _host.Session.ReturnToLobbyByHullLossCoroutine();
            yield return _lobbySettle;
            string hostProblems = PlayerReleaseProbe.DescribeProblems(SessionQueries.LocalPlayer);
            Assert.IsEmpty(hostProblems, $"The host is not free in the lobby after the hull loss: {hostProblems}.");
            float hostWalked = 0f;
            yield return PlayerReleaseProbe.WalkCoroutine(SessionQueries.LocalPlayer, Vector2.up, walked => hostWalked = walked);
            Assert.GreaterOrEqual(hostWalked, MIN_WALK_METERS, "The host does not walk in the lobby after the hull loss.");

            yield return _host.WaitForReportCoroutine(CoopFlightSteps.FREE_IN_LOBBY);
            CoopObservation client = _host.LastReport;
            Assert.IsEmpty(client.ReleaseProblems, $"The client is not free in the lobby after the hull loss: {client.ReleaseProblems}.");
            Assert.GreaterOrEqual(client.WalkedMeters, MIN_WALK_METERS, "The client does not walk in the lobby after the hull loss.");
            ShipRider serverClient = _host.ClientRider();
            Assert.IsFalse(serverClient.IsRiding || serverClient.IsSeated, "The client's player still rides or sits on the server in the lobby.");
        }

        private void AssertSameDamage(string when) {
            CoopObservation host = CoopFlightObserver.CaptureFlight(_host.LastReport.Step);
            CoopObservation client = _host.LastReport;
            Assert.AreEqual(host.Hull, client.Hull, HULL_TOLERANCE, $"Host and client see different hull {when}.");
            CollectionAssert.AreEqual(host.BrokenSockets, client.BrokenSockets, $"Host and client see different broken modules {when}.");
            CollectionAssert.AreEqual(host.OpenBreaches, client.OpenBreaches, $"Host and client see different breaches {when}.");
            Debug.Log($"[CoopTest] damage {when}: host hull {host.Hull:0.0}, client {client.Hull:0.0}, broken [{string.Join(", ", host.BrokenSockets)}], breaches [{string.Join(", ", host.OpenBreaches)}]");
        }
    }
}
#endif
