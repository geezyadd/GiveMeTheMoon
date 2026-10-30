#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ParcelModule.Data;
using Features.ParcelModule.Scripts.Crate;
using Features.ParcelModule.Scripts.Rules;
using Features.ParcelModule.Scripts.Services;
using Features.ParcelModule.Scripts.Stand;
using Features.ShipModule.Scripts.Generated;
using NUnit.Framework;
using Tests.PlayMode.FieldAgent;
using Tests.PlayMode.LoopSmoke;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.Parcel {
    // At the first station the host aims at an offer on the pad's delivery stand and presses E: the parcel's crate
    // appears on the stand, the host picks it up, carries it aboard and puts it on the deck; the ship flies to the
    // next station with it lying on the deck, and on arrival the crew wallet rises by the parcel's reward exactly once
    // and the parcel is handed over.
    public sealed class ParcelDeliveryTest {
        private const int TEST_TIMEOUT_MS = 300000;
        private const float OFFER_TIMEOUT_SECONDS = 10f;
        private const float SPAWN_TIMEOUT_SECONDS = 5f;
        private const float SETTLE_SECONDS = 1.5f;
        private const float CRUISE_SECONDS = 3f;
        private const float HANDOVER_TIMEOUT_SECONDS = 5f;
        private const float AFTER_PAY_SECONDS = 2f;
        private const string OFFER_NAME_PREFIX = "Offer";

        private FieldAgentTestFlight _flight;
        private ParcelTestSteps _steps;
        private long _reward;
        private int _destination;

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
        public IEnumerator WhenTheCrewTakesAParcelAtTheStand_AndFliesToTheNextStation_ThenTheWalletRisesByItsRewardOnce() {
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine(TakeParcelAboardCoroutine);
            ParcelEntry entry = _steps.GetOnlyEntry();
            ParcelCrate crate = _steps.GetCrate(entry);
            yield return new WaitForSeconds(CRUISE_SECONDS);
            Assert.AreEqual(ParcelState.Loose, _steps.GetOnlyEntry().State, "The parcel left the deck in a cruise without bugs.");
            Assert.IsTrue(_steps.IsAboard(crate, _flight.Ship), $"The parcel does not lie on board in cruise ({crate.Position}).");
            yield return LookAtCoroutine(crate.Position);
            yield return ParcelTestSteps.CaptureCoroutine("parcel-crate-on-deck.png");

            _steps.StartRecordingWallet();
            yield return _flight.LandAndLeaveCoroutine(AssertPaidOnceCoroutine);
        }

        private IEnumerator TakeParcelAboardCoroutine(LoopSmokeFlight flight) {
            yield return EnsureOfferCoroutine();
            ParcelOfferInteractable offer = FindOfferedSlot();
            CameraLookDriver.DebugPitchOverride = true;
            yield return LoopSmokeHandCheck.AimAtCoroutine(offer.GetComponent<Collider>().bounds.center);
            LocalPlayerInteraction interaction = SessionQueries.LocalPlayer.GetComponent<LocalPlayerInteraction>();
            Assert.AreSame(offer, interaction.Target.Usable, "The stand's offer is not usable under the crosshair.");
            yield return ParcelTestSteps.CaptureCoroutine("parcel-offer-at-the-stand.png");
            interaction.Interact();
            yield return WaitUntilCoroutine(() => _steps.TryGetOnlyEntry(out _), SPAWN_TIMEOUT_SECONDS, "the parcel taken at the stand");

            ParcelEntry entry = _steps.GetOnlyEntry();
            _reward = entry.Reward;
            _destination = entry.DestinationLoop;
            Assert.AreEqual(SessionQueries.Resolve<IReadOnlyShipRunModel>().LoopIndex + 1, _destination, "The parcel does not go to the next station.");
            ParcelCrate crate = _steps.GetCrate(entry);
            yield return LoopSmokeKiosk.PickUpCoroutine(crate.Grabbable);
            ShipDeckBoarding.PutAboveDeck(SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>(), _flight.Ship);
            yield return new WaitForSeconds(SETTLE_SECONDS);
            interaction.ReleaseHeld();
            yield return new WaitForSeconds(SETTLE_SECONDS);
            Assert.IsTrue(_steps.IsAboard(crate, _flight.Ship), $"The parcel put down on the deck is not on board ({crate.Position}).");
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            Debug.Log($"{ParcelTestSteps.LOG_PREFIX}took parcel {entry.NetId} for {_reward} to loop {_destination}");
        }

        // A station offers 0..2 parcels at random: the test asks the server for new offers until there is one.
        private IEnumerator EnsureOfferCoroutine() {
            yield return WaitUntilCoroutine(() => _steps.Parcels.IsAvailable, OFFER_TIMEOUT_SECONDS, "the synced parcel model");
            IParcelOfferService offers = SessionQueries.Resolve<IParcelOfferService>();
            int loop = SessionQueries.Resolve<IReadOnlyShipRunModel>().LoopIndex;
            while (_steps.Parcels.Offers.Count == 0) {
                offers.ServerCreateOffers(loop);
                yield return null;
            }
        }

        // The one delivery stand stands beside the pad under the ship.
        private ParcelOfferInteractable FindOfferedSlot() {
            ParcelOffer first = _steps.Parcels.Offers.First(offer => offer.IsTaken == false);
            ParcelOfferInteractable[] slots = Object.FindObjectsByType<ParcelOfferInteractable>(FindObjectsSortMode.None);
            Assert.AreEqual(ParcelOfferRule.MAX_OFFERS, slots.Length, "The station does not have exactly one delivery stand.");
            ParcelOfferInteractable slot = slots.FirstOrDefault(candidate => candidate.name == OFFER_NAME_PREFIX + first.Slot);
            Assert.IsNotNull(slot, $"The delivery stand has no slot {first.Slot}.");
            return slot;
        }

        private IEnumerator AssertPaidOnceCoroutine() {
            Assert.AreEqual(_destination, SessionQueries.Resolve<IReadOnlyShipRunModel>().LoopIndex, "The ship landed at another station than the parcel's.");
            yield return WaitUntilCoroutine(() => _steps.Parcels.Entries.Count == 0, HANDOVER_TIMEOUT_SECONDS, "the parcel handed over");
            yield return new WaitForSeconds(AFTER_PAY_SECONDS);
            Assert.AreEqual(1, _steps.WalletRises.Count(rise => rise == _reward), $"The wallet rose by the reward {_reward} not exactly once: {string.Join(", ", _steps.WalletRises)}.");
            yield return ParcelTestSteps.CaptureCoroutine("parcel-delivered.png");
            Debug.Log($"{ParcelTestSteps.LOG_PREFIX}delivered at loop {_destination}: wallet rises {string.Join(", ", _steps.WalletRises)}");
        }

        private static IEnumerator LookAtCoroutine(Vector3 point) {
            CameraLookDriver.DebugPitchOverride = true;
            float deadline = Time.time + SETTLE_SECONDS;
            while (Time.time < deadline && HandWeaponAiming.KeepAiming(point) == false)
                yield return null;
        }

        private static IEnumerator WaitUntilCoroutine(System.Func<bool> condition, float timeoutSeconds, string target) {
            float deadline = Time.time + timeoutSeconds;
            while (condition() == false) {
                Assert.IsTrue(Time.time <= deadline, $"No {target} in {timeoutSeconds} s.");
                yield return null;
            }
        }
    }
}
#endif
