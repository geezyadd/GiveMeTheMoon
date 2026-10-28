#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Ore;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.NetworkModelModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Generated;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Coop.Host {
    // Ore and the harpoon with a real second process: in the first cruise the host breaks rocks until one drops ore;
    // the client sees the same pieces at the same places and shows them. The host puts the harpoon in the client's hand
    // (test setup: the purchase is LoopSmokeOreCheck's); the client aims and clicks, its rope hooks a piece (the server
    // names the client as the hooker), the host draws the client's rope, the piece reaches the ship once and the crew
    // balance grows by its price on both peers.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientHarpoonTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const int LOOP = 1;
        private const string HARPOON_ID = "harpoon";
        private const float PULL_DISTANCE = 30f;
        private const float PULL_HEIGHT_BELOW = 1f;
        private const float SETTLE_SECONDS = 0.5f;
        // A client places a drifting piece by the same network time as the host, a moment behind it (m).
        private const float ORE_PLACE_TOLERANCE_METERS = 1.5f;
        private const float DELIVERY_TIMEOUT_SECONDS = 10f;

        private readonly List<FieldOreCollected> _collected = new List<FieldOreCollected>();

        private CoopFlightHostHarness _host;
        private LoopSmokeContext _context;

        [SetUp]
        public void SetUp() =>
            _host = new CoopFlightHostHarness();

        [TearDown]
        public void TearDown() =>
            _host?.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostBreaksRocksAndClientThrowsTheHarpoon_ThenBothSeeTheOreAndTheRopeAndTheCrewIsPaidOnce() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_HARPOON);
            yield return _host.StartAndBoardCoroutine();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            _context = new LoopSmokeContext(_host.Session, null);
            _context.CreateFlight();
            yield return ClientSeesTheHostsOreCoroutine();
            yield return ClientPullsOreInCoroutine();
            yield return _host.FinishCoroutine();
        }

        private IEnumerator ClientSeesTheHostsOreCoroutine() {
            _host.BeginStep("host breaks rocks until one drops ore; the client sees it");
            LoopSmokeOreCheck ore = new LoopSmokeOreCheck(_context);
            yield return ore.BreakRocksUntilOreCoroutine();
            FieldOreId dropped = ore.DroppedOre[0];
            Assert.IsTrue(SessionQueries.Resolve<IFieldOreQuery>().TryGetOre(dropped, out FieldOreDescription piece), $"{dropped} is gone on the host.");
            _host.Flight.PutShipBeside(piece.Center, PULL_DISTANCE, PULL_HEIGHT_BELOW);
            yield return new WaitForSeconds(SETTLE_SECONDS);

            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_ORE, CoopFlightSteps.ORE_SEEN);
            CoopObservation client = _host.LastReport;
            CoopObservation host = CoopFlightObserver.CaptureFlight(CoopFlightSteps.ORE_SEEN);
            int clientIndex = Array.IndexOf(client.OreIds, dropped.Value);
            int hostIndex = Array.IndexOf(host.OreIds, dropped.Value);
            Assert.GreaterOrEqual(clientIndex, 0, $"The client does not have {dropped}: {client.DescribeFlight()}");
            Assert.IsTrue(client.OreShown[clientIndex], $"The client does not show {dropped}.");
            float placeError = Vector3.Distance(client.OreFromDestination[clientIndex], host.OreFromDestination[hostIndex]);
            Assert.Less(placeError, ORE_PLACE_TOLERANCE_METERS, $"The client shows {dropped} {placeError:0.00} m from where the host does.");
            Debug.Log($"[CoopTest] the client sees {client.OreIds.Length} piece(s) of the host's ore, {dropped} {placeError:0.00} m from the host's; screenshot {client.Screenshot}");
        }

        private IEnumerator ClientPullsOreInCoroutine() {
            _host.BeginStep("the client throws the harpoon and pulls a piece in");
            yield return GiveHarpoonToClientCoroutine();
            PlayerKey clientKey = _host.ClientRider().Key;
            IFieldOreEvents events = SessionQueries.Resolve<IFieldOreEvents>();
            IReadOnlyWalletModel wallet = SessionQueries.Resolve<IReadOnlyWalletModel>();
            long balance = wallet.Balance;
            bool hasSeenClientRope = false;
            _collected.Clear();
            events.OreCollected += _collected.Add;
            try {
                _host.Command(CoopFlightSteps.COMMAND_THROW_HARPOON);
                yield return _host.WaitForReportCoroutine(CoopFlightSteps.HARPOON_HOOKED, () => hasSeenClientRope |= IsClientRopeShown(clientKey));
                int hooked = _host.LastReport.OwnHookedOreId;
                Assert.Greater(hooked, 0, $"The client reports no piece on its rope: {_host.LastReport.DescribeFlight()}");
                yield return _host.WaitForCoroutine(() => WatchForDelivery(clientKey, hooked, ref hasSeenClientRope), $"ore {hooked} on the client's rope at the ship");
                Assert.IsTrue(hasSeenClientRope, "The host never drew the client's rope.");
                Assert.AreEqual(1, _collected.Count(collected => collected.Id.Value == hooked), $"Ore {hooked} reached the ship more than once.");
                FieldOreCollected delivery = _collected.First(collected => collected.Id.Value == hooked);
                Assert.AreEqual(clientKey, delivery.Hooker, "The server did not take the client as the piece's hooker.");
                Assert.AreEqual(balance + _collected.Sum(collected => collected.Price), wallet.Balance, "The crew balance did not grow by exactly the delivered prices.");
            } finally {
                events.OreCollected -= _collected.Add;
            }

            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_DELIVERY, CoopFlightSteps.ORE_DELIVERED);
            Assert.AreEqual(wallet.Balance, _host.LastReport.Balance, "The client sees another crew balance after the delivery.");
            _host.ClientRider().GetComponent<GrabController>().ServerConsumeHeld();
            Debug.Log($"[CoopTest] the client's harpoon pulled ore in: balance {balance} -> {wallet.Balance}, rope seen on the host; screenshot {_host.LastReport.Screenshot}");
        }

        private IEnumerator GiveHarpoonToClientCoroutine() {
            HandWeaponDefinition harpoon = SessionQueries.Resolve<ShopCatalog>().HandWeapons.First(handWeapon => handWeapon.Id == HARPOON_ID);
            Transform client = _host.ClientRider().transform;
            GameObject item = Object.Instantiate(harpoon.ItemPrefab.gameObject, client.position + Vector3.up, client.rotation);
            NetworkServer.Spawn(item);
            yield return null;
            Assert.IsTrue(client.GetComponent<GrabController>().ServerGive(item.GetComponent<Grabbable>()), "The harpoon could not be put into the client's hand.");
        }

        private bool WatchForDelivery(PlayerKey clientKey, int hooked, ref bool hasSeenClientRope) {
            hasSeenClientRope |= IsClientRopeShown(clientKey);
            return _collected.Any(collected => collected.Id.Value == hooked);
        }

        // The host draws a rope from the client's harpoon to a piece the server names the client's.
        private static bool IsClientRopeShown(PlayerKey clientKey) {
            List<FieldOreDescription> ores = new List<FieldOreDescription>();
            SessionQueries.Resolve<IFieldOreQuery>().FindAll(ores);
            FieldOreSceneModel scene = SessionQueries.Resolve<FieldOreSceneModel>();
            foreach (FieldOreDescription ore in ores) {
                if (ore.Hooker.Equals(clientKey) && scene.TryGetView(ore.Id.Value, out FieldOreView view) && view.GetComponentInChildren<LineRenderer>(true).enabled)
                    return true;
            }

            return false;
        }
    }
}
#endif
