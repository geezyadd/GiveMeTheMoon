#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using Features.BugModule.Scripts.Brain;
using Features.CameraModule.Scripts;
using Features.FieldAgentModule.Scripts.Client;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Configurations;
using Features.WeaponModule.Scripts.Readiness;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Bug {
    // The host buys the pistol at the first station and flies into a slow cruise; in the debug window it switches the
    // random bugs off and clicks "Spawn bugs (N)": the group comes from a hidden point, a bug lands on the ship and
    // gnaws it (the synced hull drops), and the host turns its view onto that bug and shoots it with the pistol through
    // the fire input; the server kills it and every other bug flies on.
    public sealed class BugGnawTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const string PISTOL_ID = "pistol";
        private const float CRUISE_THROTTLE = 0.15f;
        private const float GNAW_TIMEOUT_SECONDS = 120f;
        private const float HULL_DROP_TIMEOUT_SECONDS = 15f;
        private const float KILL_TIMEOUT_SECONDS = 1f;
        private const float COOLDOWN_TIMEOUT_SECONDS = 2.5f;
        private const int SHOT_ATTEMPTS = 4;
        private const float READY = 1f;
        // The incoming flock is pictured once its nearest bug is this close.
        private const float INCOMING_SCREENSHOT_METERS = 45f;
        private const float AFTER_DEATH_SECONDS = 0.15f;

        private FieldAgentTestFlight _flight;
        private BugTestSteps _steps;
        private bool _hasIncomingShot;

        [SetUp]
        public void SetUp() {
            _flight = new FieldAgentTestFlight();
            _steps = new BugTestSteps(_flight);
        }

        [TearDown]
        public void TearDown() {
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            _flight.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenHostSpawnsBugsInCruise_AndOneGnawsTheShip_ThenTheHullDropsAndThePistolKillsIt() {
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine(BuyPistolCoroutine);
            _flight.SetThrottle(CRUISE_THROTTLE);
            yield return _steps.SpawnGroupCoroutine();

            int gnawer = -1;
            yield return _steps.WaitForStageCoroutine(BugStage.Gnaw, GNAW_TIMEOUT_SECONDS, id => gnawer = id, PictureIncomingCoroutine);
            yield return AssertHullDropsCoroutine();
            yield return BugTestSteps.CaptureCoroutine("bug-gnawing-the-ship.png");
            yield return ShootCoroutine(gnawer);
            Debug.Log($"{BugTestSteps.LOG_PREFIX}the pistol killed gnawing bug {gnawer}, {_steps.Bugs.Count} bugs left");

            yield return _flight.ClickDebugActionCoroutine("Remove all field agents");
            yield return _flight.LandAndLeaveCoroutine();
        }

        private static IEnumerator BuyPistolCoroutine(LoopSmokeFlight flight) {
            HandWeaponDefinition pistol = SessionQueries.Resolve<ShopCatalog>().HandWeapons.First(handWeapon => handWeapon.Id == PISTOL_ID);
            CameraLookDriver.DebugPitchOverride = true;
            HandWeaponItem bought = null;
            yield return LoopSmokeKiosk.BuyCoroutine<HandWeaponItem>(flight, LoopSmokeKiosk.FindHandWeaponEntry(PISTOL_ID), pistol.Price, IsLoosePistol, item => bought = item);
            yield return LoopSmokeKiosk.PickUpCoroutine(bought.GetComponent<Grabbable>());
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
        }

        private IEnumerator PictureIncomingCoroutine() {
            if (_hasIncomingShot || _steps.FindNearestDistance() > INCOMING_SCREENSHOT_METERS)
                yield break;

            _hasIncomingShot = true;
            yield return BugTestSteps.AimAtShownBugCoroutine(_steps.FindNearestBug(), _ => { });
            yield return BugTestSteps.CaptureCoroutine("bugs-flock-incoming.png");
        }

        private IEnumerator AssertHullDropsCoroutine() {
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            float before = stats.Hull;
            float deadline = Time.time + HULL_DROP_TIMEOUT_SECONDS;
            while (stats.Hull >= before) {
                Assert.Less(Time.time, deadline, $"A gnawing bug did not lower the hull in {HULL_DROP_TIMEOUT_SECONDS} s (hull {stats.Hull}).");
                yield return null;
            }

            Debug.Log($"{BugTestSteps.LOG_PREFIX}gnawing: hull {before:0.0} -> {stats.Hull:0.0}");
        }

        // A sitting bug sways: a shot may miss it, as a player's may; the next one goes after the cooldown.
        private IEnumerator ShootCoroutine(int bugId) {
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            Assert.IsTrue(readiness.IsHolding, "The host does not hold the pistol in cruise.");
            for (int attempt = 1; _steps.Bugs.TryGet(bugId, out BugAgent _); attempt++) {
                Assert.LessOrEqual(attempt, SHOT_ATTEMPTS, $"None of {SHOT_ATTEMPTS} pistol shots killed bug {bugId}.");
                bool isOnTarget = false;
                yield return BugTestSteps.AimAtShownBugCoroutine(bugId, onTarget => isOnTarget = onTarget);
                Assert.IsTrue(isOnTarget, $"The view did not settle on bug {bugId}.");
                yield return HandWeaponAiming.ClickFireCoroutine();
                yield return WaitForKillCoroutine(bugId);
                if (_steps.Bugs.TryGet(bugId, out BugAgent _))
                    yield return WaitForReadyCoroutine(readiness);
            }

            yield return new WaitForSeconds(AFTER_DEATH_SECONDS);
            yield return BugTestSteps.CaptureCoroutine("bug-shot-with-pistol.png");
            Assert.IsFalse(_flight.Control.TryGetAgent(bugId, out _), $"Killed bug {bugId} is still a field agent.");
        }

        private IEnumerator WaitForKillCoroutine(int bugId) {
            for (float waited = 0f; waited < KILL_TIMEOUT_SECONDS && _steps.Bugs.TryGet(bugId, out BugAgent _); waited += Time.deltaTime) {
                if (SessionQueries.Resolve<IFieldAgentShownAgents>().TryGetShownWorldPosition(bugId, out Vector3 position))
                    BugTestSteps.KeepAiming(position);

                yield return null;
            }
        }

        private static IEnumerator WaitForReadyCoroutine(HeldWeaponReadinessModel readiness) {
            float deadline = Time.time + COOLDOWN_TIMEOUT_SECONDS;
            while (readiness.Readiness < READY && Time.time < deadline)
                yield return null;
        }

        private static bool IsLoosePistol(HandWeaponItem item) =>
            item.Definition.Id == PISTOL_ID && item.GetComponent<Grabbable>().CanBeGrabbed;
    }
}
#endif
