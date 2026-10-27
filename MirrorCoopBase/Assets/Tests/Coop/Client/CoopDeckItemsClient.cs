#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Mirror;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The client handles items aboard (scenario deck-items): at the station it drops a free item on the deck by hand
    // and carries another one aboard; in the cruise it lets the carried one go onto the deck; it reports the items it
    // sees on the deck after the release, after the host's hard turn and after the landing. Each step on the host's
    // command.
    public sealed class CoopDeckItemsClient : CoopClientBot {
        private const int LOOP = 1;
        private const float HAND_DROP_HEIGHT = 1f;
        private const float FALL_SECONDS = 1.5f;
        private const float FAR_AWAY = 100f;
        // The host drops its item between the deck's middle and its right edge; the client drops and carries its items
        // a little sternward of it. That part of the deck is clear: the throttle and the helm stand on the left towards
        // the bow, the engines at the stern corners.
        private const float SIDE_SPOT_SHARE = 0.4f;
        private const float STERNWARD_OF_HOST_METERS = 1.5f;
        private const float LYING_GAP = 0.05f;

        private readonly WaitForSeconds _fall = new WaitForSeconds(FALL_SECONDS);
        private readonly ShipDeckItemHandling _handling = new ShipDeckItemHandling();

        private static NetworkIdentity Player => SessionQueries.LocalPlayer;

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return DropOneAndCarryAnotherCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            yield return ReleaseInCruiseCoroutine();
            yield return ReportOnCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_ITEMS_TURNED, CoopFlightSteps.ITEMS_TURNED);
            yield return WaitForLandingCoroutine(LOOP);
            yield return ReportOnCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_ITEMS_LANDED, CoopFlightSteps.ITEMS_LANDED);
        }

        private IEnumerator DropOneAndCarryAnotherCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_DROP_ITEM);
            yield return _handling.GrabFirstCoroutine(FindItemsNotLyingOnDeck());
            yield return FailIfCoroutine(_handling.Failure, "Taking an item to drop on the deck");
            Grabbable dropped = _handling.Taken;
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), Ship, SideSpot(), HAND_DROP_HEIGHT);
            yield return Settle;
            yield return _handling.ReleaseCoroutine();
            yield return FailIfCoroutine(_handling.Failure, "Dropping the item on the deck");
            yield return _handling.WaitForRestCoroutine(dropped);
            yield return FailIfCoroutine(_handling.Failure, "The dropped item");
            yield return Settle;
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.ITEM_DROPPED);
            observation.DroppedItemId = dropped.netId;
            Report(observation);

            List<Grabbable> others = FindItemsNotLyingOnDeck();
            others.Remove(dropped);
            yield return _handling.GrabFirstCoroutine(others);
            yield return FailIfCoroutine(_handling.Failure, "Taking an item to carry aboard");
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), Ship, SideSpot(), HAND_DROP_HEIGHT);
            yield return Settle;
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.ITEM_CARRIED));
        }

        private IEnumerator ReleaseInCruiseCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_RELEASE_ITEM);
            yield return _handling.ReleaseCoroutine();
            yield return FailIfCoroutine(_handling.Failure, "Letting the carried item go in cruise");
            yield return _fall;
            // A player walks on after letting an item go: whatever it fell onto, it must end up on the deck.
            float walked = 0f;
            yield return PlayerReleaseProbe.WalkCoroutine(Player, Vector2.left, distance => walked = distance);
            yield return _fall;
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.ITEM_RELEASED);
            observation.WalkedMeters = walked;
            Report(observation);
        }

        private IEnumerator ReportOnCommandCoroutine(string command, string step) {
            yield return WaitForCommandCoroutine(command);
            Report(CoopFlightObserver.CaptureFlight(step));
        }

        // The items lying on the deck are the crew's drops; the rest (the station's, the deck's tool rack) are free to take.
        private List<Grabbable> FindItemsNotLyingOnDeck() {
            List<Grabbable> items = new List<Grabbable>();
            foreach (Grabbable item in ShipDeckItemHandling.FindLooseBoxItems(Ship)) {
                if (IsLyingOnDeck(item) == false)
                    items.Add(item);
            }

            return items;
        }

        private bool IsLyingOnDeck(Grabbable item) =>
            SessionQueries.IsOnDeck(Ship, item.transform.position)
            && Mathf.Abs(ShipDeckItemRest.MeasureGapAboveDeck(Ship, item.GetComponentInChildren<BoxCollider>())) < LYING_GAP;

        private Vector3 SideSpot() {
            Vector3 middle = Ship.transform.InverseTransformPoint(Ship.DeckGeometry.Bounds.center);
            Vector3 edge = ShipDeckBoarding.DeckEdgeSpot(Ship, Vector3.right * FAR_AWAY, Vector3.left);
            Vector3 hostSpot = Vector3.Lerp(new Vector3(middle.x, edge.y, middle.z), edge, SIDE_SPOT_SHARE);
            return ShipDeckBoarding.DeckEdgeSpot(Ship, hostSpot + Vector3.back * STERNWARD_OF_HOST_METERS, Vector3.zero);
        }
    }
}
#endif
