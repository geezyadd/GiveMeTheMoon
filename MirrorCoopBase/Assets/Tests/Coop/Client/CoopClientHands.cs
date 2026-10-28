#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Text;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The client bot's hands, as a player uses them: stands near a thing, turns the view onto it through the look input
    // and the test pitch (Tests.Shared.HandWeaponAiming), and presses interact on what the crosshair offers. A failure
    // ends the scenario through the client (FailCoroutine never returns). The caller turns
    // CameraLookDriver.DebugPitchOverride on.
    public sealed class CoopClientHands {
        private const float APPROACH_METERS = 1.6f;
        private const int APPROACH_SIDES = 6;
        private const float WALK_TIMEOUT_SECONDS = 10f;
        private const float STAND_HEIGHT = 0.8f;
        private const float DECK_DROP_HEIGHT = 0.5f;
        private const float SETTLE_SECONDS = 1f;
        private const float AIM_TIMEOUT_SECONDS = 4f;
        private const float DESCRIBED_RAY_METERS = 15f;

        // Toward the item's top and corners, in its bounds' half extents.
        private static readonly Vector3[] _visibleParts = {
            Vector3.zero, new Vector3(0f, 0.7f, 0f), new Vector3(0.6f, 0.3f, 0.6f), new Vector3(-0.6f, 0.3f, 0.6f),
            new Vector3(0.6f, 0.3f, -0.6f), new Vector3(-0.6f, 0.3f, -0.6f)
        };

        private readonly CoopTestClient _client;
        private readonly WaitForSeconds _settle = new WaitForSeconds(SETTLE_SECONDS);

        public CoopClientHands(CoopTestClient client) =>
            _client = client;

        public static LocalPlayerInteraction Interaction =>
            SessionQueries.LocalPlayer.GetComponent<LocalPlayerInteraction>();

        public static GrabController Hand =>
            SessionQueries.LocalPlayer.GetComponent<GrabController>();

        public static CharacterMovableBase Body =>
            SessionQueries.LocalPlayer.GetComponent<CharacterMovableBase>();

        // On the deck a step from a point of the ship toward the deck's middle, so the view looks at the point from the
        // side and not down at the own feet (an engine socket plate sits in the deck).
        public IEnumerator StandOnDeckNearCoroutine(ShipBase ship, Vector3 point) {
            Vector3 local = ship.transform.InverseTransformPoint(point);
            Vector3 inward = new Vector3(-local.x, 0f, -local.z);
            inward = inward.sqrMagnitude > 0f ? inward.normalized : Vector3.forward;
            Vector3 spot = ShipDeckBoarding.DeckEdgeSpot(ship, local + inward * APPROACH_METERS, inward);
            ShipDeckBoarding.PutAbove(Body, ship, spot, DECK_DROP_HEIGHT);
            yield return _settle;
        }

        public IEnumerator StandAtCoroutine(Vector3 position) {
            Rigidbody body = Body.Body;
            body.position = position;
            body.transform.position = position;
            body.linearVelocity = Vector3.zero;
            yield return _settle;
        }

        public IEnumerator AimAtCoroutine(Vector3 target, string what) =>
            AimAtCoroutine(() => target, what);

        public IEnumerator AimAtCoroutine(Func<Vector3> target, string what) =>
            AimAtCoroutine(target, what, true);

        // target: read every frame, so the view follows a thing riding a ship that still moves. A view that cannot
        // settle (a point beyond the camera's pitch) fails the scenario only when the aim is required.
        private IEnumerator AimAtCoroutine(Func<Vector3> target, string what, bool isRequired) {
            float deadline = Time.realtimeSinceStartup + AIM_TIMEOUT_SECONDS;
            while (HandWeaponAiming.KeepAiming(target()) == false) {
                if (Time.realtimeSinceStartup > deadline && isRequired)
                    yield return _client.FailCoroutine($"The view did not settle on {what} at {target()}.");

                if (Time.realtimeSinceStartup > deadline)
                    yield break;

                yield return null;
            }

            // LocalPlayerInteraction aims in Update, before this frame's camera pose: let it see the settled view.
            yield return null;
        }

        // Walks around the item until nothing else lies between the view and it, as a player steps around a pile.
        public IEnumerator PickUpCoroutine(Grabbable item, string what) {
            Vector3 center = item.GetComponentInChildren<Collider>().bounds.center;
            Vector3 fromPlayer = SessionQueries.LocalPlayer.transform.position - center;
            fromPlayer.y = 0f;
            Vector3 first = fromPlayer.sqrMagnitude > 0f ? fromPlayer.normalized : Vector3.back;
            for (int side = 0; side < APPROACH_SIDES && Interaction.Target.Grabbable != item; side++) {
                Vector3 direction = Quaternion.Euler(0f, side * 360f / APPROACH_SIDES, 0f) * first;
                yield return StandAtCoroutine(item.transform.position + direction * APPROACH_METERS + Vector3.up * STAND_HEIGHT);
                yield return AimAtVisiblePartCoroutine(item, what);
            }

            if (Interaction.Target.Grabbable != item)
                yield return _client.FailCoroutine($"{what} is not under the crosshair from any side (looked at {Interaction.Target.LookedAt}, {DescribeView()}).");

            Interaction.Interact();
            yield return _client.WaitForCoroutine(() => Hand.Held == item, $"{what} in the hand");
        }

        // On a flying ship a teleport leaves the rider behind: the player walks there through the scripted move stick,
        // which moves it relative to the camera as the keys do. target: read every frame (it rides the ship).
        public IEnumerator WalkToCoroutine(Func<Vector3> target, ShipBase ship, string what) {
            CharacterInputBuffer input = SessionQueries.Resolve<CharacterInputBuffer>();
            Transform player = SessionQueries.LocalPlayer.transform;
            float deadline = Time.realtimeSinceStartup + WALK_TIMEOUT_SECONDS;
            input.BeginScripted();
            while (IsWalking(player, target(), ship, deadline)) {
                input.SetScripted(CameraRelativeStick(target() - player.position), false, false);
                yield return null;
            }

            input.SetScripted(Vector2.zero, false, false);
            input.EndScripted();
            if (FlatDistance(player.position, target()) > APPROACH_METERS)
                yield return _client.FailCoroutine($"The client did not walk to {what} ({FlatDistance(player.position, target()):0.0} m left, on the deck {SessionQueries.IsOnDeck(ship, player.position)}).");

            yield return _settle;
        }

        private static bool IsWalking(Transform player, Vector3 target, ShipBase ship, float deadline) =>
            FlatDistance(player.position, target) > APPROACH_METERS && SessionQueries.IsOnDeck(ship, player.position) && Time.realtimeSinceStartup < deadline;

        // The view onto the item's middle, then onto points toward its corners, until the crosshair offers this item:
        // the part of it another item does not cover.
        public IEnumerator AimAtVisiblePartCoroutine(Grabbable item, string what) {
            Collider collider = item.GetComponentInChildren<Collider>();
            for (int point = 0; point < _visibleParts.Length && Interaction.Target.Grabbable != item; point++) {
                Vector3 part = _visibleParts[point];
                yield return AimAtCoroutine(() => collider.bounds.center + Vector3.Scale(part, collider.bounds.extents), what, false);
            }
        }

        // Aims at the point and presses interact there, when the crosshair offers this very target.
        public IEnumerator UseAimedCoroutine(InteractableBase expected, Vector3 point, string what) {
            yield return AimAtCoroutine(point, what);
            if (Interaction.Target.Usable != expected)
                yield return _client.FailCoroutine($"{what} is not offered under the crosshair (usable {Interaction.Target.Usable}, looked at {Interaction.Target.LookedAt}, held {Hand.Held}, aimed at {point}, {DescribeView()}).");

            Interaction.Interact();
        }

        // The centre of the target's own collider the interaction ray can find: enabled, on an interactable layer and not
        // a child place of another interaction (a socket's repair volume, its seat).
        public static Vector3 FindAimPoint(InteractableBase target) {
            int interactableMask = SessionQueries.Resolve<GrabConfiguration>().InteractableMask;
            foreach (Collider collider in target.GetComponentsInChildren<Collider>()) {
                bool isAimable = collider.enabled && (interactableMask & (1 << collider.gameObject.layer)) != 0;
                InteractableBase owner = collider.GetComponentInParent<InteractableBase>();
                if (isAimable && (owner == target || owner.gameObject == target.gameObject))
                    return collider.bounds.center;
            }

            throw new MissingComponentException($"{target.name} has no enabled interactable collider of its own.");
        }

        // Where the view is and everything the crosshair ray meets, for a failed aim.
        public static string DescribeView() {
            Transform view = SessionQueries.Resolve<IGameCameraService>().OutputCamera.transform;
            StringBuilder hits = new StringBuilder();
            foreach (RaycastHit hit in Physics.RaycastAll(view.position, view.forward, DESCRIBED_RAY_METERS, Physics.AllLayers, QueryTriggerInteraction.Collide)) {
                Collider collider = hit.collider;
                hits.Append($"[{collider.name} L{collider.gameObject.layer} trigger {collider.isTrigger} at {hit.distance:0.00} m] ");
            }

            return $"view at {view.position} along {view.forward}, player at {SessionQueries.LocalPlayer.transform.position}, ray hits {hits}";
        }

        // CharacterMovableBase turns the stick by Camera.main, so the stick is worked out from the same camera.
        private static Vector2 CameraRelativeStick(Vector3 toTarget) {
            Transform view = Camera.main.transform;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(view.right, Vector3.up).normalized;
            return new Vector2(Vector3.Dot(toTarget, right), Vector3.Dot(toTarget, forward)).normalized;
        }

        private static float FlatDistance(Vector3 from, Vector3 to) =>
            Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude;

        public IEnumerator DropCoroutine() {
            Interaction.ReleaseHeld();
            yield return _client.WaitForCoroutine(() => Hand.IsHolding == false, "an empty hand after dropping");
            yield return _settle;
        }
    }
}
#endif
