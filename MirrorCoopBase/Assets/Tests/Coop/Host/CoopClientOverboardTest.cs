#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.IO;
using System.Linq;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts.Drift;
using Features.DebugModule.Contracts;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts.Overboard;
using Features.ShopModule.Scripts.Configurations;
using Features.SonarModule.Data;
using Mirror;
using NUnit.Framework;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Coop.Host {
    // A player overboard with a real second process: in the first cruise the client throws itself overboard through
    // its debug window; the server puts it overboard, the host sees its body drift level and tumble and the "man
    // overboard" marker on its screen, and brings the ship to it through its own debug window: the client rides the
    // deck again. The client falls out a second time and the host hooks it with the harpoon (test setup puts the
    // harpoon in the host's hand) through the real aim and fire input: the rope pulls it to the ship, which takes it
    // back.
    [PrebuildSetup("Tests.Coop.Editor.CoopClientBuild")]
    public sealed class CoopClientOverboardTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const int LOOP = 1;
        private const string BRING_LABEL = "Bring the ship to the drifting player";
        private const string HARPOON_ID = "harpoon";
        private const string OVERBOARD_KIND_ID = "overboard";
        private const string TUMBLING_VISUAL = "RotatablePart/Visual";
        private const string SCREENSHOT_FOLDER = "../.agent-orchestrator/screenshots/overboard";
        private const float DRIFT_WATCH_SECONDS = 2f;
        private const float MAX_HEIGHT_CHANGE_METERS = 1.5f;
        private const float AIM_SECONDS = 1f;
        private const float HOOK_WAIT_SECONDS = 1.5f;
        private const float RELOAD_WAIT_SECONDS = 2f;
        private const int THROW_ATTEMPTS = 4;
        private const float BODY_MIDDLE_HEIGHT = 1f;

        private CoopFlightHostHarness _host;

        [SetUp]
        public void SetUp() =>
            _host = new CoopFlightHostHarness();

        [TearDown]
        public void TearDown() {
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            _host?.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenClientFallsOverboardInCruise_ThenHostSeesItDriftAndTheShipOrTheHarpoonTakesItBack() {
            // LoopSmokeErrorLog fails the test with the full list instead.
            LogAssert.ignoreFailingMessages = true;
            yield return _host.HostAndLetClientInCoroutine(CoopFlightSteps.SCENARIO_OVERBOARD);
            yield return _host.StartAndBoardCoroutine();
            _host.Flight.PullLaunchLever(LOOP);
            yield return _host.WaitForCruiseCoroutine(LOOP, _host.FaceAwayFromStation);
            yield return ClientFallsOutAndShipComesCoroutine();
            yield return ClientFallsOutAndHarpoonPullsCoroutine();
            yield return _host.FinishCoroutine();
        }

        private IEnumerator ClientFallsOutAndShipComesCoroutine() {
            _host.BeginStep("the client falls overboard; the host sees it drift and brings the ship");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_THROW_OVERBOARD, CoopFlightSteps.THROWN_OVERBOARD);
            yield return AssertHostSeesDriftCoroutine();
            yield return CaptureCoroutine("coop-host-sees-client-drift.png");

            DebugWindowClicking clicking = new();
            yield return clicking.ClickCoroutine(DebugSections.SHIP, BRING_LABEL);
            Assert.IsNull(clicking.Failure, $"Debug window \"{BRING_LABEL}\": {clicking.Failure}.");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_BACK_ON_DECK, CoopFlightSteps.BACK_ON_DECK);
            AssertClientBackOnDeck();
            Debug.Log($"[CoopTest] the ship took the drifting client back; screenshots {_host.LastReport.Screenshot}");
        }

        private IEnumerator ClientFallsOutAndHarpoonPullsCoroutine() {
            _host.BeginStep("the client falls overboard again; the host pulls it in with the harpoon");
            yield return GiveHarpoonToHostCoroutine();
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_THROW_OVERBOARD_AGAIN, CoopFlightSteps.THROWN_OVERBOARD_AGAIN);
            ShipOverboardRider client = ClientOverboard();
            Assert.IsTrue(client.IsOverboard, "The server does not have the client overboard the second time.");
            yield return HookClientCoroutine(client);
            yield return CaptureCoroutine("coop-host-harpoon-pulls-client.png");
            yield return _host.WaitForCoroutine(() => client.IsOverboard == false, "the hooked client pulled to the ship and taken back");
            yield return _host.CommandAndWaitCoroutine(CoopFlightSteps.COMMAND_REPORT_PULLED_ON_DECK, CoopFlightSteps.PULLED_ON_DECK);
            AssertClientBackOnDeck();
            SessionQueries.LocalPlayer.GetComponent<GrabController>().ServerConsumeHeld();
            Debug.Log($"[CoopTest] the host's harpoon pulled the drifting client in; screenshot {_host.LastReport.Screenshot}");
        }

        private IEnumerator AssertHostSeesDriftCoroutine() {
            ShipOverboardRider client = ClientOverboard();
            Assert.IsTrue(client.IsOverboard, "The server does not have the thrown client overboard.");
            Assert.IsTrue(client.GetComponent<CharacterDrift>().IsDrifting, "The host does not show the client's body drifting.");
            float startHeight = _host.ClientServerPosition().y;
            yield return new WaitForSeconds(DRIFT_WATCH_SECONDS);
            float heightChange = _host.ClientServerPosition().y - startHeight;
            Assert.Less(Mathf.Abs(heightChange), MAX_HEIGHT_CHANGE_METERS, $"The host sees the drifting client fall or rise ({heightChange:0.00} m).");
            Assert.AreNotEqual(Quaternion.identity, client.transform.Find(TUMBLING_VISUAL).localRotation, "The host does not show the client's body tumbling.");
            SonarMarkerScreenModel markers = SessionQueries.Resolve<SonarMarkerScreenModel>();
            Assert.IsTrue(markers.Markers.Any(marker => marker.Kind.Id == OVERBOARD_KIND_ID), "The host's screen has no man overboard marker.");
            Debug.Log($"[CoopTest] the host sees the client drift: height {heightChange:+0.00;-0.00} m in {DRIFT_WATCH_SECONDS} s, {markers.Markers.Count} marker(s)");
        }

        // The view turned through the look input onto the drifting body, then the fire button, until the rope holds.
        private IEnumerator HookClientCoroutine(ShipOverboardRider client) {
            PlayerKey hostKey = SessionQueries.LocalPlayer.GetComponent<GrabController>().Key;
            CameraLookDriver.DebugPitchOverride = true;
            for (int attempt = 1; IsHookedBy(client, hostKey) == false; attempt++) {
                Assert.LessOrEqual(attempt, THROW_ATTEMPTS, $"None of {THROW_ATTEMPTS} ropes hooked the drifting client.");
                for (float aimed = 0f; aimed < AIM_SECONDS; aimed += Time.deltaTime) {
                    HandWeaponAiming.KeepAiming(client.Position + Vector3.up * BODY_MIDDLE_HEIGHT);
                    yield return null;
                }

                yield return HandWeaponAiming.ClickFireCoroutine();
                for (float waited = 0f; waited < HOOK_WAIT_SECONDS && IsHookedBy(client, hostKey) == false; waited += Time.deltaTime)
                    yield return null;

                if (IsHookedBy(client, hostKey) == false)
                    yield return new WaitForSeconds(RELOAD_WAIT_SECONDS);
            }

            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            HandWeaponAiming.TurnViewBack();
            Debug.Log($"[CoopTest] the host's rope hooked the client {Vector3.Distance(client.Position, _host.Ship.transform.position):0.0} m from the ship");
        }

        private IEnumerator GiveHarpoonToHostCoroutine() {
            HandWeaponDefinition harpoon = SessionQueries.Resolve<ShopCatalog>().HandWeapons.First(handWeapon => handWeapon.Id == HARPOON_ID);
            Transform host = SessionQueries.LocalPlayer.transform;
            GameObject item = Object.Instantiate(harpoon.ItemPrefab.gameObject, host.position + Vector3.up, host.rotation);
            NetworkServer.Spawn(item);
            yield return null;
            Assert.IsTrue(host.GetComponent<GrabController>().ServerGive(item.GetComponent<Grabbable>()), "The harpoon could not be put into the host's hand.");
        }

        private void AssertClientBackOnDeck() {
            Assert.IsFalse(ClientOverboard().IsOverboard, "The server still has the client overboard.");
            Assert.IsFalse(ClientOverboard().GetComponent<CharacterDrift>().IsDrifting, "The host still shows the client drifting.");
            Assert.IsTrue(_host.IsClientOnDeckOnServer(), $"The client taken back is not on the deck by its server position: {_host.ClientServerPosition()}.");
        }

        private ShipOverboardRider ClientOverboard() =>
            _host.ClientRider().GetComponent<ShipOverboardRider>();

        private static bool IsHookedBy(ShipOverboardRider client, PlayerKey hooker) =>
            client.IsHooked && client.HookerKey.Equals(hooker);

        private static IEnumerator CaptureCoroutine(string fileName) {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
            yield return null;
            yield return null;
        }
    }
}
#endif
