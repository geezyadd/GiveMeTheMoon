#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Items lie on the deck in every ship phase, played by hand as a player does (run 2 flight 3): one item is dropped
    // on the deck at the station, another one is carried aboard and let go in the cruise (after the forced world shift).
    // Both rest on the deck in the cruise, still after a hard turn, and after the landing as physics bodies again. The
    // coop host test drops its item on the deck with the same steps.
    public sealed class LoopSmokeDeckItemCheck {
        private const int RUN = 2;
        private const int FLIGHT = 3;
        private const float REST_GAP_TOLERANCE = 0.05f;
        private const float RIDE_DRIFT_TOLERANCE = 0.02f;
        private const float RESTING_SPEED = 0.3f;
        private const float REST_HOLD_SECONDS = 0.5f;
        private const float REST_TIMEOUT_SECONDS = 8f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float BOARD_SETTLE_SECONDS = 1f;
        private const float FALL_SECONDS = 1.5f;
        private const float TURN_SECONDS = 2f;
        private const float LEVEL_TIMEOUT_SECONDS = 6f;
        private const float LEVEL_DOT = 0.9999f;
        private const float HAND_DROP_HEIGHT = 1f;
        private const float SIDE_SPOT_SHARE = 0.4f;
        private const float FAR_AWAY = 100f;
        private const int GRAB_ROUNDS = 3;
        private const float GRAB_RETRY_SECONDS = 1f;

        private readonly LoopSmokeContext _context;
        private Grabbable _stationItem;
        private Grabbable _carriedItem;
        private Vector3 _stationItemLocal;

        public LoopSmokeDeckItemCheck(LoopSmokeContext context) =>
            _context = context;

        private ShipBase Ship => _context.Session.Ship;
        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();
        private static GrabController Hand => Player.GetComponent<GrabController>();

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, RUN, FLIGHT, 10)]
        public IEnumerator DropOneItemAndCarryAnotherCoroutine() {
            Grabbable[] items = FindLooseBoxItems(Ship);
            Assert.GreaterOrEqual(items.Length, 2, "Fewer than two loose items to drop on the deck at the station.");
            yield return DropOnDeckByHandCoroutine(Ship, items, OnStationItemDropped);
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return GrabAnyCoroutine(items, _stationItem, OnCarriedItemGrabbed);
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            yield return _context.Flight.BoardPlayerCoroutine();
            AssertLiesOnDeck(_stationItem, "at the station");
            Assert.IsFalse(_stationItem.GetComponent<Rigidbody>().isKinematic, "An item on the deck at the station is not a physics body.");
        }

        [LoopSmokeStep(LoopSmokePhase.InCruise, RUN, FLIGHT, 20)]
        public IEnumerator ItemsRestOnDeckInCruiseCoroutine() {
            AssertLiesOnDeck(_stationItem, "after the takeoff");
            Assert.AreSame(_carriedItem, Hand.Held, "The carried item left the hand before the cruise.");
            yield return ReleaseCoroutine();
            yield return new WaitForSeconds(FALL_SECONDS);
            AssertLiesOnDeck(_carriedItem, "let go in the cruise");

            _stationItemLocal = ShipLocal(_stationItem);
            Vector3 carriedLocal = ShipLocal(_carriedItem);
            yield return TurnCoroutine();
            AssertLiesOnDeck(_stationItem, "after a turn");
            AssertLiesOnDeck(_carriedItem, "after a turn");
            AssertRodeAlong(_stationItem, _stationItemLocal);
            AssertRodeAlong(_carriedItem, carriedLocal);
            _context.Flight.FaceDestination();
        }

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, RUN, FLIGHT, 10)]
        public IEnumerator ItemsLieOnDeckAfterLandingCoroutine() {
            yield return WaitForRestCoroutine(_stationItem);
            yield return WaitForRestCoroutine(_carriedItem);
            foreach (Grabbable item in new[] { _stationItem, _carriedItem }) {
                AssertLiesOnDeck(item, "after the landing");
                Assert.IsFalse(item.GetComponent<Rigidbody>().isKinematic, $"{item.name} stays kinematic after the landing.");
                Assert.IsTrue(item.CanBeGrabbed, $"{item.name} cannot be picked up after the landing.");
            }
        }

        // Free items with a box collider (tools, modules), nearest to the deck's middle first: drops of earlier stations lie
        // far behind the ship, and some of this station's drops lie under its deck, out of reach.
        public static Grabbable[] FindLooseBoxItems(ShipBase ship) =>
            ShipDeckItemHandling.FindLooseBoxItems(ship);

        // At the station, as a player does: takes one of the items through the real aim, walks onto the deck beside its
        // middle and lets go; the item falls and comes to rest on the deck under physics.
        public static IEnumerator DropOnDeckByHandCoroutine(ShipBase ship, Grabbable[] candidates, Action<Grabbable> dropped) {
            Grabbable item = null;
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return GrabAnyCoroutine(candidates, null, grabbed => item = grabbed);
                ShipDeckBoarding.PutAbove(PlayerBody(), ship, SideSpot(ship), HAND_DROP_HEIGHT);
                yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
                yield return ReleaseCoroutine();
                yield return WaitForRestCoroutine(item);
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            dropped(item);
        }

        private void OnStationItemDropped(Grabbable item) =>
            _stationItem = item;

        private void OnCarriedItemGrabbed(Grabbable item) =>
            _carriedItem = item;

        private static bool IsLooseBoxItem(Grabbable item) =>
            ShipDeckItemHandling.IsLooseBoxItem(item);

        private static BoxCollider Box(Grabbable item) =>
            item.GetComponentInChildren<BoxCollider>();

        private static CharacterMovableBase PlayerBody() =>
            Player.GetComponent<CharacterMovableBase>();

        // Between the deck's middle and its right edge: clear of the middle, where the player boards, and of the edge the
        // hand may face.
        private static Vector3 SideSpot(ShipBase ship) {
            Vector3 middle = ship.transform.InverseTransformPoint(ship.DeckGeometry.Bounds.center);
            Vector3 edge = ShipDeckBoarding.DeckEdgeSpot(ship, Vector3.right * FAR_AWAY, Vector3.left);
            return Vector3.Lerp(new Vector3(middle.x, edge.y, middle.z), edge, SIDE_SPOT_SHARE);
        }

        // The aim at an item may meet another loose item first, which is taken then, or something else, and the next one
        // is tried. Right after a run starts the hand may not take anything yet: the items are tried again a few times.
        private static IEnumerator GrabAnyCoroutine(Grabbable[] candidates, Grabbable skipped, Action<Grabbable> grabbed) {
            List<string> missed = new List<string>();
            Grabbable taken = null;
            for (int round = 0; round < GRAB_ROUNDS && taken == null; round++) {
                missed.Clear();
                yield return GrabFirstUnderCrosshairCoroutine(candidates, skipped, missed, item => taken = item);
                if (taken == null)
                    yield return new WaitForSeconds(GRAB_RETRY_SECONDS);
            }

            Assert.IsNotNull(taken, $"None of the loose items came under the crosshair: {string.Join("; ", missed)}.");
            grabbed(taken);
        }

        private static IEnumerator GrabFirstUnderCrosshairCoroutine(Grabbable[] candidates, Grabbable skipped, List<string> missed, Action<Grabbable> grabbed) {
            foreach (Grabbable candidate in candidates) {
                if (candidate == skipped || IsLooseBoxItem(candidate) == false)
                    continue;

                yield return LoopSmokeHandCheck.AimAtCoroutine(Box(candidate).bounds.center);
                Grabbable target = Interaction.Target.Grabbable;
                if (target == null || target == skipped || IsLooseBoxItem(target) == false) {
                    missed.Add(DescribeMiss(candidate, target));
                    continue;
                }

                Interaction.Interact();
                yield return WaitForCoroutine(() => Hand.Held == target, $"{target.name} in the hand");
                grabbed(target);
                yield break;
            }
        }

        private static string DescribeMiss(Grabbable candidate, Grabbable target) {
            InteractableBase lookedAt = Interaction.Target.LookedAt;
            return $"{candidate.name} at {candidate.transform.position} from the player at {Player.transform.position}: "
                   + $"interaction enabled {Interaction.isActiveAndEnabled}, aim on '{(lookedAt != null ? lookedAt.name : "nothing")}', "
                   + $"item {(target != null ? target.name : "none")}";
        }

        private static IEnumerator ReleaseCoroutine() {
            Interaction.ReleaseHeld();
            yield return WaitForCoroutine(() => Hand.IsHolding == false, "the hand to be empty after release");
        }

        // The pilot's stick hard to one side: the ship turns and banks, then levels out again.
        private IEnumerator TurnCoroutine() {
            Ship.FlightControlDebug.SetDebugSteer(true, 1f);
            yield return new WaitForSeconds(TURN_SECONDS);
            Ship.FlightControlDebug.SetDebugSteer(false, 0f);
            yield return _context.Session.WaitForCoroutine(IsShipLevel, LEVEL_TIMEOUT_SECONDS, "the ship to level out after the turn");
        }

        private bool IsShipLevel() =>
            Vector3.Dot(Ship.transform.up, Vector3.up) > LEVEL_DOT;

        private Vector3 ShipLocal(Grabbable item) =>
            Ship.transform.InverseTransformPoint(item.transform.position);

        private void AssertLiesOnDeck(Grabbable item, string when) {
            Assert.IsNotNull(item, $"An item dropped on the deck is gone {when}.");
            Assert.IsTrue(SessionQueries.IsOnDeck(Ship, item.transform.position), $"{item.name} at {ShipLocal(item)} is off the deck {when}.");
            float gap = ShipDeckItemRest.MeasureGapAboveDeck(Ship, Box(item));
            Assert.Less(Mathf.Abs(gap), REST_GAP_TOLERANCE, $"{item.name} does not lie on the deck {when}: {gap:F3} m above it.");
        }

        private void AssertRodeAlong(Grabbable item, Vector3 localBefore) {
            float drift = Vector3.Distance(localBefore, ShipLocal(item));
            Assert.Less(drift, RIDE_DRIFT_TOLERANCE, $"{item.name} moved {drift:F3} m on the deck during the turn.");
        }

        // A dropped item topples and slides for a moment: it rests once it stays slow for a while.
        private static IEnumerator WaitForRestCoroutine(Grabbable item) {
            Rigidbody body = item.GetComponent<Rigidbody>();
            float start = Time.time;
            float restingSince = start;
            while (Time.time - restingSince < REST_HOLD_SECONDS) {
                if (body.linearVelocity.magnitude >= RESTING_SPEED)
                    restingSince = Time.time;

                if (Time.time - start > REST_TIMEOUT_SECONDS)
                    Assert.Fail($"{item.name} does not rest after {REST_TIMEOUT_SECONDS} s ({body.linearVelocity.magnitude:F2} m/s).");

                yield return null;
            }
        }

        private static IEnumerator WaitForCoroutine(Func<bool> condition, string target) {
            float deadline = Time.realtimeSinceStartup + COMMAND_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {COMMAND_TIMEOUT_SECONDS} s waiting for {target}.");

                yield return null;
            }
        }
    }
}
#endif
