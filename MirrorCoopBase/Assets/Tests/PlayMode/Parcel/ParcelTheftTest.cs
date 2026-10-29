#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using Features.BugModule.Scripts.Brain;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.FieldAgentModule.Scripts.Client;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Features.ParcelModule.Scripts.Configuration;
using Features.ParcelModule.Scripts.Crate;
using Features.ParcelModule.Scripts.Sonar;
using Features.ShopModule.Scripts.Configurations;
using Features.SonarModule.Contracts;
using Features.WeaponModule.Scripts.Readiness;
using NUnit.Framework;
using Tests.PlayMode.Bug;
using Tests.PlayMode.FieldAgent;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Parcel {
    // At the first station the host buys the harpoon (left on the deck) and the pistol (in the hand). In a slow cruise
    // the debug window gives the ship a parcel and sends a bug to steal it: the bug grabs the crate and flies off with
    // it, slower than the ship, a stolen parcel contact on the sonar; the host shoots the thief with the pistol through
    // the fire input, the crate drifts, the host takes the harpoon from the deck, the ship chases the crate, and the
    // harpoon's rope pulls it back onto the deck. The landing at the next station pays for it.
    public sealed class ParcelTheftTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const string PISTOL_ID = "pistol";
        private const string HARPOON_ID = "harpoon";
        private const string GIVE_LABEL = "Give the ship a parcel";
        private const string STEAL_LABEL = "A bug steals the parcel";
        private const string CONTACT_FORMAT = "parcel:{0}";
        private const float CRUISE_THROTTLE = 0.15f;
        private const float FULL_THROTTLE = 1f;
        private const float GIVE_TIMEOUT_SECONDS = 5f;
        private const float STEAL_TIMEOUT_SECONDS = 120f;
        private const float CHASE_PICTURE_SECONDS = 2f;
        private const float KILL_TIMEOUT_SECONDS = 1f;
        private const float COOLDOWN_TIMEOUT_SECONDS = 2.5f;
        private const float DRIFT_TIMEOUT_SECONDS = 3f;
        private const int SHOT_ATTEMPTS = 5;
        private const float READY = 1f;
        private const float SETTLE_SECONDS = 1.5f;
        private const float AIM_SECONDS = 3f;
        private const float HAND_TIMEOUT_SECONDS = 3f;
        private const float HARPOON_REACH_METERS = 40f;
        private const float CHASE_TIMEOUT_SECONDS = 60f;
        private const float STOP_SECONDS = 3f;
        private const float HOOK_TIMEOUT_SECONDS = 1.5f;
        private const float BOARD_TIMEOUT_SECONDS = 15f;
        private const float HANDOVER_TIMEOUT_SECONDS = 5f;
        private const float SIDEWAYS_METERS = 5f;

        private FieldAgentTestFlight _flight;
        private ParcelTestSteps _steps;
        private Grabbable _harpoon;
        private long _reward;

        private static LocalPlayerInteraction Interaction => SessionQueries.LocalPlayer.GetComponent<LocalPlayerInteraction>();
        private static BugServerModel Bugs => SessionQueries.Resolve<BugServerModel>();
        private static GrabController Hand => SessionQueries.LocalPlayer.GetComponent<GrabController>();

        [SetUp]
        public void SetUp() {
            _flight = new FieldAgentTestFlight();
            _steps = new ParcelTestSteps();
        }

        [TearDown]
        public void TearDown() {
            _steps.StopRecordingWallet();
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            _flight.Dispose();
        }

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenABugStealsTheParcel_AndTheCrewShootsItAndHarpoonsTheCrate_ThenTheParcelIsBackOnBoardAndPaysAtTheStation() {
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine(BuyWeaponsCoroutine);
            _flight.SetThrottle(CRUISE_THROTTLE);
            yield return _flight.ClickDebugActionCoroutine(GIVE_LABEL);
            yield return _steps.WaitForStateCoroutine(ParcelState.Loose, GIVE_TIMEOUT_SECONDS);
            _reward = _steps.GetOnlyEntry().Reward;

            yield return _flight.ClickDebugActionCoroutine(STEAL_LABEL);
            yield return _steps.WaitForStateCoroutine(ParcelState.Carried, STEAL_TIMEOUT_SECONDS);
            ParcelEntry stolen = _steps.GetOnlyEntry();
            ParcelCrate crate = _steps.GetCrate(stolen);
            AssertStolenContact(stolen);
            yield return BugTestSteps.AimAtShownBugCoroutine(stolen.CarrierAgentId, _ => { });
            yield return ParcelTestSteps.CaptureCoroutine("parcel-theft.png");
            yield return new WaitForSeconds(CHASE_PICTURE_SECONDS);
            yield return BugTestSteps.AimAtShownBugCoroutine(stolen.CarrierAgentId, _ => { });
            yield return ParcelTestSteps.CaptureCoroutine("parcel-chase.png");

            yield return ShootThiefCoroutine(stolen.CarrierAgentId);
            yield return _steps.WaitForStateCoroutine(ParcelState.Drifting, DRIFT_TIMEOUT_SECONDS);
            yield return LookAtCoroutine(crate.Position);
            yield return ParcelTestSteps.CaptureCoroutine("parcel-drifting.png");

            yield return SwapToHarpoonCoroutine();
            yield return ChaseCrateCoroutine(crate);
            yield return HarpoonCrateCoroutine(crate);
            yield return _steps.WaitForStateCoroutine(ParcelState.Loose, BOARD_TIMEOUT_SECONDS);
            yield return new WaitForSeconds(SETTLE_SECONDS);
            Assert.IsTrue(_steps.IsAboard(crate, _flight.Ship), $"The harpooned parcel is not on board ({crate.Position}).");
            yield return LookAtCoroutine(crate.Position);
            yield return ParcelTestSteps.CaptureCoroutine("parcel-recovered.png");

            yield return _flight.ClickDebugActionCoroutine("Remove all field agents");
            _steps.StartRecordingWallet();
            yield return _flight.LandAndLeaveCoroutine(AssertPaidCoroutine);
        }

        // The harpoon goes onto the deck where the host will ride, the pistol stays in the hand.
        private IEnumerator BuyWeaponsCoroutine(LoopSmokeFlight flight) {
            CameraLookDriver.DebugPitchOverride = true;
            _harpoon = null;
            yield return BuyAndPickUpCoroutine(flight, HARPOON_ID, item => _harpoon = item);
            ShipDeckBoarding.PutAboveDeck(SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>(), _flight.Ship);
            yield return new WaitForSeconds(SETTLE_SECONDS);
            Interaction.ReleaseHeld();
            yield return new WaitForSeconds(SETTLE_SECONDS);
            yield return BuyAndPickUpCoroutine(flight, PISTOL_ID, _ => { });
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
        }

        private static IEnumerator BuyAndPickUpCoroutine(LoopSmokeFlight flight, string id, System.Action<Grabbable> bought) {
            HandWeaponDefinition definition = SessionQueries.Resolve<ShopCatalog>().HandWeapons.First(handWeapon => handWeapon.Id == id);
            HandWeaponItem item = null;
            yield return LoopSmokeKiosk.BuyCoroutine<HandWeaponItem>(flight, LoopSmokeKiosk.FindHandWeaponEntry(id), definition.Price, candidate => IsLoose(candidate, id), result => item = result);
            yield return LoopSmokeKiosk.PickUpCoroutine(item.GetComponent<Grabbable>());
            bought(item.GetComponent<Grabbable>());
        }

        private void AssertStolenContact(in ParcelEntry stolen) {
            ParcelConfiguration configuration = SessionQueries.Resolve<ParcelConfiguration>();
            ParcelSonarContactSource contacts = new(SessionQueries.Resolve<IReadOnlyParcelsModel>(), configuration);
            Assert.IsTrue(contacts.TryGetContact(string.Format(CONTACT_FORMAT, stolen.NetId), out SonarContact contact), "The stolen parcel is no sonar contact.");
            Assert.AreSame(configuration.StolenSonarKind, contact.Kind, "The stolen parcel is not of the stolen parcel kind.");
        }

        // A darting thief may dodge a shot, as a player's may; the next one goes after the cooldown.
        private IEnumerator ShootThiefCoroutine(int thiefId) {
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            Assert.IsTrue(readiness.IsHolding, "The host does not hold the pistol in cruise.");
            for (int attempt = 1; Bugs.TryGet(thiefId, out BugAgent _); attempt++) {
                Assert.LessOrEqual(attempt, SHOT_ATTEMPTS, $"None of {SHOT_ATTEMPTS} pistol shots killed thief {thiefId}.");
                yield return BugTestSteps.AimAtShownBugCoroutine(thiefId, _ => { });
                yield return HandWeaponAiming.ClickFireCoroutine();
                yield return KeepOnThiefCoroutine(thiefId);
                if (Bugs.TryGet(thiefId, out BugAgent _))
                    yield return WaitForReadyCoroutine(readiness);
            }

            Debug.Log($"{ParcelTestSteps.LOG_PREFIX}the pistol killed thief {thiefId}");
        }

        private IEnumerator KeepOnThiefCoroutine(int thiefId) {
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            for (float waited = 0f; waited < KILL_TIMEOUT_SECONDS && Bugs.TryGet(thiefId, out BugAgent _); waited += Time.deltaTime) {
                if (shown.TryGetShownWorldPosition(thiefId, out Vector3 position))
                    BugTestSteps.KeepAiming(position);

                yield return null;
            }
        }

        // The pistol is let go to the side, so it does not land on the harpoon, then the harpoon is taken from the deck.
        private IEnumerator SwapToHarpoonCoroutine() {
            Transform player = SessionQueries.LocalPlayer.transform;
            yield return LookAtCoroutine(player.position + player.right * SIDEWAYS_METERS);
            Interaction.ReleaseHeld();
            yield return WaitUntilCoroutine(() => Hand.IsHolding == false, HAND_TIMEOUT_SECONDS, "the pistol let go");
            yield return new WaitForSeconds(SETTLE_SECONDS);
            Vector3 harpoonCenter = _harpoon.GetComponentInChildren<Collider>().bounds.center;
            yield return LookAtCoroutine(harpoonCenter);
            Assert.AreSame(_harpoon, Interaction.Target.Grabbable, $"The harpoon on the deck at {harpoonCenter} is not under the crosshair.");
            Interaction.Interact();
            yield return WaitUntilCoroutine(() => Hand.Held == _harpoon, HAND_TIMEOUT_SECONDS, "the harpoon in the hand");
        }

        // The ship turns toward the drifting crate and closes in, then stops within the harpoon's reach.
        private IEnumerator ChaseCrateCoroutine(ParcelCrate crate) {
            float deadline = Time.time + CHASE_TIMEOUT_SECONDS;
            while (Vector3.Distance(crate.Position, _flight.Ship.transform.position) > HARPOON_REACH_METERS) {
                Assert.IsTrue(Time.time <= deadline, $"The ship did not come within {HARPOON_REACH_METERS} m of the crate in {CHASE_TIMEOUT_SECONDS} s.");
                Assert.AreEqual(ParcelState.Drifting, _steps.GetOnlyEntry().State, "The drifting parcel was lost during the chase.");
                _flight.Ship.FlightControlDebug.DebugFace(crate.Position - _flight.Ship.transform.position);
                _flight.SetThrottle(FULL_THROTTLE);
                yield return null;
            }

            _flight.SetThrottle(0f);
            yield return new WaitForSeconds(STOP_SECONDS);
        }

        private IEnumerator HarpoonCrateCoroutine(ParcelCrate crate) {
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            for (int attempt = 1; _steps.GetOnlyEntry().State == ParcelState.Drifting; attempt++) {
                Assert.LessOrEqual(attempt, SHOT_ATTEMPTS, $"None of {SHOT_ATTEMPTS} harpoon throws hooked the crate {Vector3.Distance(crate.Position, _flight.Ship.transform.position):0} m away.");
                yield return WaitForReadyCoroutine(readiness);
                yield return LookAtCoroutine(crate.Position);
                yield return HandWeaponAiming.ClickFireCoroutine();
                float deadline = Time.time + HOOK_TIMEOUT_SECONDS;
                while (Time.time < deadline && _steps.GetOnlyEntry().State == ParcelState.Drifting) {
                    HandWeaponAiming.KeepAiming(crate.Position);
                    yield return null;
                }
            }

            yield return null;
            yield return ParcelTestSteps.CaptureCoroutine("parcel-harpooned.png");
            Debug.Log($"{ParcelTestSteps.LOG_PREFIX}the harpoon hooked the parcel");
        }

        private IEnumerator AssertPaidCoroutine() {
            yield return WaitUntilCoroutine(() => _steps.Parcels.Entries.Count == 0, HANDOVER_TIMEOUT_SECONDS, "the parcel handed over");
            yield return new WaitForSeconds(SETTLE_SECONDS);
            Assert.AreEqual(1, _steps.WalletRises.Count(rise => rise == _reward), $"The wallet rose by the reward {_reward} not exactly once: {string.Join(", ", _steps.WalletRises)}.");
        }

        private static IEnumerator LookAtCoroutine(Vector3 point) {
            CameraLookDriver.DebugPitchOverride = true;
            float deadline = Time.time + AIM_SECONDS;
            while (Time.time < deadline && BugTestSteps.KeepAiming(point) == false)
                yield return null;

            yield return null;
        }

        private static IEnumerator WaitForReadyCoroutine(HeldWeaponReadinessModel readiness) {
            float deadline = Time.time + COOLDOWN_TIMEOUT_SECONDS;
            while (readiness.Readiness < READY && Time.time < deadline)
                yield return null;
        }

        private static IEnumerator WaitUntilCoroutine(System.Func<bool> condition, float timeoutSeconds, string target) {
            float deadline = Time.time + timeoutSeconds;
            while (condition() == false) {
                Assert.IsTrue(Time.time <= deadline, $"No {target} in {timeoutSeconds} s.");
                yield return null;
            }
        }

        private static bool IsLoose(HandWeaponItem item, string id) =>
            item.Definition.Id == id && item.GetComponent<Grabbable>().CanBeGrabbed;
    }
}
#endif
