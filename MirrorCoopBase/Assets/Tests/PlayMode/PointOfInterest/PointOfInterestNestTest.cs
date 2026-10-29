#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using Features.CameraModule.Scripts;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.PointOfInterestModule.Scripts.Network;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Generated;
using Features.WeaponModule.Scripts.Readiness;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.PointOfInterest {
    // The host buys the pistol at the first station and flies into the cruise; the debug window puts the ship just
    // outside the nearest bug nest's zone and the cruise flies it in: the nest starts once, the ship's throttle goes to
    // Stop and the entry cutscene plays (controls blocked, HUD hidden) and ends by itself, the nest's bugs are out; the
    // host shoots the reward container open with the pistol through the fire input and the synced balance grows by the
    // reward once. Then the debug window's test cutscene is skipped with Space.
    public sealed class PointOfInterestNestTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const string PISTOL_ID = "pistol";
        private const float ENTRY_THROTTLE = 0.6f;
        private const int SHOT_ATTEMPTS = 12;
        private const float AIM_TIMEOUT_SECONDS = 3f;
        private const float READY = 1f;
        private const float COOLDOWN_TIMEOUT_SECONDS = 2.5f;
        private const float AFTER_SHOT_SECONDS = 0.3f;
        // The close photo of the nest: beside and a little above it.
        private static readonly Vector3 _nestPhotoOffset = new(14f, 4f, -10f);

        private FieldAgentTestFlight _flight;
        private PointOfInterestTestSteps _steps;

        [SetUp]
        public void SetUp() {
            _flight = new FieldAgentTestFlight();
            _steps = new PointOfInterestTestSteps(_flight.Session, _flight.SetThrottle);
        }

        [TearDown]
        public void TearDown() {
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            _flight.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenTheShipEntersANest_AndTheCutscenePlays_ThenBugsComeOutAndTheShotOpenContainerPaysOnce() {
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine(BuyPistolCoroutine);
            yield return _steps.WaitForPointsCoroutine();

            Vector3 cruiseStart = _flight.Ship.transform.position;
            int pointId = 0;
            yield return _steps.MoveToNearestPointCoroutine(id => pointId = id);
            Vector3 nest = _steps.PositionOf(pointId);
            yield return PointOfInterestTestSteps.CapturePhotoCoroutine("nest-up-close.png", nest + _nestPhotoOffset, nest);
            yield return _steps.EnterZoneCoroutine(pointId, ENTRY_THROTTLE);
            yield return _steps.WatchEntryCutsceneCoroutine(false, "cutscene-looks-at-the-nest.png", "cutscene-bugs-flying-out.png");
            _steps.AssertBugsCameOut();
            Assert.AreEqual(PointOfInterestState.Triggered, _steps.StateOf(pointId), "Points of interest: the nest did not stay started after its cutscene.");

            yield return ShootContainerOpenCoroutine(pointId);
            yield return _steps.PlayAndSkipTestCutsceneCoroutine();

            yield return _flight.ClickDebugActionCoroutine("Remove all field agents");
            yield return PutShipBackCoroutine(cruiseStart);
            yield return _flight.LandAndLeaveCoroutine();
        }

        private IEnumerator ShootContainerOpenCoroutine(int pointId) {
            long balance = SessionQueries.Resolve<IReadOnlyWalletModel>().Balance;
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            Assert.IsTrue(readiness.IsHolding, "The host does not hold the pistol in cruise.");
            Vector3 container = _steps.PositionOf(pointId);
            CameraLookDriver.DebugPitchOverride = true;
            for (int attempt = 1; _steps.StateOf(pointId) != PointOfInterestState.Opened; attempt++) {
                Assert.LessOrEqual(attempt, SHOT_ATTEMPTS, $"{SHOT_ATTEMPTS} pistol shots did not open the container of point {pointId}.");
                yield return AimCoroutine(container);
                if (attempt == 1)
                    yield return PointOfInterestTestSteps.CaptureCoroutine("nest-aimed-with-the-pistol.png");

                yield return HandWeaponAiming.ClickFireCoroutine();
                yield return new WaitForSeconds(AFTER_SHOT_SECONDS);
                yield return WaitForReadyCoroutine(readiness);
            }

            yield return PointOfInterestTestSteps.CaptureCoroutine("nest-shot-open.png");
            yield return _steps.AssertPaidOnceCoroutine(pointId, balance);
        }

        // The debug move lands in the flight's next step: the landing takes its heading to the station from there.
        private IEnumerator PutShipBackCoroutine(Vector3 cruiseStart) {
            _flight.Ship.FlightControlDebug.DebugMove(cruiseStart - _flight.Ship.transform.position);
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        private static IEnumerator AimCoroutine(Vector3 target) {
            float deadline = Time.realtimeSinceStartup + AIM_TIMEOUT_SECONDS;
            while (!HandWeaponAiming.KeepAiming(target) && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        private static IEnumerator WaitForReadyCoroutine(HeldWeaponReadinessModel readiness) {
            float deadline = Time.time + COOLDOWN_TIMEOUT_SECONDS;
            while (readiness.Readiness < READY && Time.time < deadline)
                yield return null;
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

        private static bool IsLoosePistol(HandWeaponItem item) =>
            item.Definition.Id == PISTOL_ID && item.GetComponent<Grabbable>().CanBeGrabbed;
    }
}
#endif
