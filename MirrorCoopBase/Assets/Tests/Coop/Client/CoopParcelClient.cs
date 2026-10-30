#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Globalization;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.FieldAgentModule.Scripts.Client;
using Features.GrabModule.Scripts;
using Features.ParcelModule.Data;
using Features.ParcelModule.Data.Generated;
using Features.ParcelModule.Scripts.Crate;
using Features.ParcelModule.Scripts.Stand;
using Mirror;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The client takes a parcel (scenario parcel): at the start station, on the host's command, it stands before an
    // open offer of the delivery stand, turns its view onto it through the look input and presses E, picks the crate up
    // through the aim and lets it go on the deck near the stern; in the cruise, on the host's command, it waits until
    // its own peer shows the parcel stolen (Carried in its synced model) with the crate hanging under the thief it shows;
    // once the thief is gone it reports whether the drifting crate collides again on its peer.
    public sealed class CoopParcelClient : CoopScenarioClientBot {
        private const int LOOP = 1;
        private const string OFFER_NAME_PREFIX = "Offer";
        private const string TAKEN_SCREENSHOT = "parcel-taken";
        private const string STOLEN_SCREENSHOT = "parcel-stolen";
        private const float STAND_BACK_METERS = 1.8f;
        private const float STAND_HEIGHT = 1f;
        private const float STERN_INWARD_METERS = 3f;
        private const float DROP_HEIGHT = 1f;
        // The crate hangs 0.7 m under the thief; the client shows both from the same batch.
        private const float CRATE_UNDER_THIEF_METERS = 3f;
        private const string STOLEN_FORMAT = "{0} {1} {2:0.00}";

        private uint _parcelNetId;

        private static LocalPlayerInteraction Interaction => NetworkClient.localPlayer.GetComponent<LocalPlayerInteraction>();
        private static GrabController Hand => NetworkClient.localPlayer.GetComponent<GrabController>();
        private static IReadOnlyParcelsModel Parcels => SessionQueries.Resolve<IReadOnlyParcelsModel>();

        protected override IEnumerator RunScenarioCoroutine() {
            yield return WaitForCoroutine(SessionQueries.IsGameAtStation, "GameScene at the start station");
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_TAKE_PARCEL);
            CameraLookDriver.DebugPitchOverride = true;
            yield return TakeParcelCoroutine();
            yield return BoardWithParcelCoroutine();
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            yield return WaitForCruiseCoroutine(LOOP);
            yield return ReportTheftCoroutine();
            yield return ReportDriftCoroutine();
            yield return WaitForLandingCoroutine(LOOP);
        }

        private IEnumerator TakeParcelCoroutine() {
            ParcelOfferInteractable slot = null;
            yield return WaitForCoroutine(() => TryFindOpenSlot(out slot), "an open offer on the delivery stand");
            Vector3 front = slot.transform.position - slot.transform.forward * STAND_BACK_METERS + Vector3.up * STAND_HEIGHT;
            Teleport(front);
            yield return Settle;
            Vector3 offerCenter = slot.GetComponent<Collider>().bounds.center;
            yield return WaitForCoroutine(() => IsAimedAt(offerCenter) && Interaction.Target.Usable == slot, "the stand's offer usable under the crosshair");
            Interaction.Interact();
            yield return WaitForCoroutine(() => Parcels.Entries.Count == 1, "the parcel taken at the stand");
            _parcelNetId = Parcels.Entries[0].NetId;
            ParcelCrate crate = null;
            yield return WaitForCoroutine(() => TryFindCrate(out crate), "the parcel's crate spawned on the client");
            string screenshot = null;
            yield return CaptureScreenCoroutine(TAKEN_SCREENSHOT, path => screenshot = path);
            Vector3 crateCenter = crate.GetComponent<Collider>().bounds.center;
            yield return WaitForCoroutine(() => IsAimedAt(crateCenter) && Interaction.Target.Grabbable == crate.Grabbable, "the crate under the crosshair");
            Interaction.Interact();
            yield return WaitForCoroutine(() => Hand.Held == crate.Grabbable, "the crate in the hand");
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.PARCEL_TAKEN);
            observation.Message = _parcelNetId.ToString(CultureInfo.InvariantCulture);
            observation.Screenshot = screenshot;
            Report(observation);
        }

        // The crate goes down on the deck a little in from the stern, where the client then rides.
        private IEnumerator BoardWithParcelCoroutine() {
            Vector3 spot = ShipDeckBoarding.DeckEdgeSpot(Ship, Vector3.back * 100f, Vector3.forward) + Vector3.forward * STERN_INWARD_METERS;
            ShipDeckBoarding.PutAbove(NetworkClient.localPlayer.GetComponent<CharacterMovableBase>(), Ship, spot, DROP_HEIGHT);
            yield return Settle;
            Interaction.ReleaseHeld();
            yield return WaitForCoroutine(() => Hand.IsHolding == false, "the crate let go on the deck");
            yield return Settle;
            Report(CoopFlightObserver.CaptureFlight(CoopTestSteps.BOARDED));
        }

        private IEnumerator ReportTheftCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_THEFT);
            int thief = 0;
            float gap = 0f;
            yield return WaitForCoroutine(() => IsShownStolen(out thief, out gap), "the parcel shown under its thief");
            CameraLookDriver.DebugPitchOverride = true;
            if (TryFindCrate(out ParcelCrate crate))
                yield return WaitForCoroutine(() => IsAimedAt(crate.transform.position), "the view on the stolen crate");

            string screenshot = null;
            yield return CaptureScreenCoroutine(STOLEN_SCREENSHOT, path => screenshot = path);
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            HandWeaponAiming.TurnViewBack();
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.PARCEL_STOLEN);
            observation.Message = string.Format(CultureInfo.InvariantCulture, STOLEN_FORMAT, _parcelNetId, thief, gap);
            observation.Screenshot = screenshot;
            Report(observation);
        }

        // The thief's look held the crate kinematic and out of the physics on this peer; drifting, it collides again.
        private IEnumerator ReportDriftCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_DRIFT);
            yield return WaitForCoroutine(() => Parcels.Entries.Count == 1 && Parcels.Entries[0].State == ParcelState.Drifting, "the parcel drifting after its thief");
            yield return Settle;
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.PARCEL_DRIFTING);
            observation.Message = TryFindCrate(out ParcelCrate crate) && crate.GetComponent<Rigidbody>().detectCollisions ? bool.TrueString : bool.FalseString;
            Report(observation);
        }

        private bool IsShownStolen(out int thief, out float gap) {
            thief = 0;
            gap = float.MaxValue;
            if (Parcels.Entries.Count != 1 || Parcels.Entries[0].State != ParcelState.Carried || TryFindCrate(out ParcelCrate crate) == false)
                return false;

            thief = Parcels.Entries[0].CarrierAgentId;
            if (SessionQueries.Resolve<IFieldAgentShownAgents>().TryGetShownWorldPosition(thief, out Vector3 shown) == false)
                return false;

            gap = Vector3.Distance(crate.transform.position, shown);
            return gap < CRATE_UNDER_THIEF_METERS;
        }

        private bool TryFindCrate(out ParcelCrate crate) {
            crate = null;
            return NetworkClient.spawned.TryGetValue(_parcelNetId, out NetworkIdentity identity) && identity.TryGetComponent(out crate);
        }

        private static bool TryFindOpenSlot(out ParcelOfferInteractable slot) {
            slot = null;
            foreach (ParcelOffer offer in Parcels.Offers) {
                if (offer.IsTaken)
                    continue;

                foreach (ParcelOfferInteractable candidate in FindObjectsByType<ParcelOfferInteractable>(FindObjectsSortMode.None)) {
                    if (candidate.name != OFFER_NAME_PREFIX + offer.Slot)
                        continue;

                    slot = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool IsAimedAt(Vector3 point) =>
            HandWeaponAiming.KeepAiming(point);

        private static void Teleport(Vector3 position) {
            Rigidbody body = NetworkClient.localPlayer.GetComponent<CharacterMovableBase>().Body;
            body.position = position;
            body.transform.position = position;
            body.linearVelocity = Vector3.zero;
        }
    }
}
#endif
