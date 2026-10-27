#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Shared {
    // Plays loose items as a player does, for the coop client bot (no NUnit there): stands before an item, tilts the
    // real camera onto it, takes it with interact, lets it go with release, and waits until a dropped item rests. A
    // failure is a message.
    public sealed class ShipDeckItemHandling {
        private const float AIM_DISTANCE = 1.5f;
        private const float AIM_TOLERANCE_DEGREES = 0.5f;
        private const float AIM_TIMEOUT_SECONDS = 3f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float RESTING_SPEED = 0.3f;
        private const float REST_HOLD_SECONDS = 0.5f;
        private const float REST_TIMEOUT_SECONDS = 8f;
        private const float SERVER_CATCH_UP_SECONDS = 0.5f;

        private readonly WaitForSeconds _serverCatchUp = new WaitForSeconds(SERVER_CATCH_UP_SECONDS);

        // Null while every step worked; otherwise what went wrong, and the step stopped there.
        public string Failure { get; private set; }
        public Grabbable Taken { get; private set; }

        private static NetworkIdentity Player => SessionQueries.LocalPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();
        private static GrabController Hand => Player.GetComponent<GrabController>();

        // Free items with a box collider (tools, modules), nearest to the deck's middle first: drops of earlier stations lie
        // far behind the ship, and some of this station's drops lie under its deck, out of reach.
        public static Grabbable[] FindLooseBoxItems(ShipBase ship) {
            List<Grabbable> items = new List<Grabbable>();
            List<float> distances = new List<float>();
            Vector3 deckMiddle = ship.DeckGeometry.Bounds.center;
            foreach (Grabbable item in Object.FindObjectsByType<Grabbable>(FindObjectsSortMode.None)) {
                if (IsLooseBoxItem(item) == false)
                    continue;

                items.Add(item);
                distances.Add((item.transform.position - deckMiddle).sqrMagnitude);
            }

            Grabbable[] sorted = items.ToArray();
            Array.Sort(distances.ToArray(), sorted);
            return sorted;
        }

        public static bool IsLooseBoxItem(Grabbable item) =>
            item != null && item.CanBeGrabbed && item.GetComponentInChildren<BoxCollider>() != null;

        // The aim at an item may meet another loose item first, which is taken then, or something else, and the next
        // one is tried.
        public IEnumerator GrabFirstCoroutine(IReadOnlyList<Grabbable> candidates) {
            Failure = null;
            Taken = null;
            List<string> missed = new List<string>();
            try {
                CameraLookDriver.DebugPitchOverride = true;
                foreach (Grabbable candidate in candidates) {
                    if (IsLooseBoxItem(candidate) == false)
                        continue;

                    yield return AimAtCoroutine(candidate.GetComponentInChildren<BoxCollider>().bounds.center);
                    Grabbable target = Interaction.Target.Grabbable;
                    if (IsLooseBoxItem(target) == false) {
                        missed.Add($"{candidate.name} at {candidate.transform.position}");
                        continue;
                    }

                    Interaction.Interact();
                    yield return WaitForCoroutine(() => Hand.Held == target, COMMAND_TIMEOUT_SECONDS, $"{target.name} in the hand");
                    if (Failure != null) {
                        missed.Add($"{target.name}: {Failure}");
                        Failure = null;
                        continue;
                    }

                    Taken = target;
                    yield break;
                }
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            Failure = $"none of the loose items came under the crosshair: {string.Join("; ", missed)}";
        }

        public IEnumerator ReleaseCoroutine() {
            Failure = null;
            Interaction.ReleaseHeld();
            yield return WaitForCoroutine(() => Hand.IsHolding == false, COMMAND_TIMEOUT_SECONDS, "the hand to be empty after release");
        }

        // A dropped item topples and slides for a moment: it rests once it stays put for a while. By its position, not
        // its body's velocity: on a client the network body moves a kinematic body.
        public IEnumerator WaitForRestCoroutine(Grabbable item) {
            Failure = null;
            Transform itemTransform = item.transform;
            Vector3 last = itemTransform.position;
            float start = Time.time;
            float restingSince = start;
            while (Time.time - restingSince < REST_HOLD_SECONDS) {
                yield return null;
                Vector3 now = itemTransform.position;
                float speed = Time.deltaTime > 0f ? Vector3.Distance(now, last) / Time.deltaTime : 0f;
                last = now;
                if (speed >= RESTING_SPEED)
                    restingSince = Time.time;

                if (Time.time - start > REST_TIMEOUT_SECONDS) {
                    Failure = $"{item.name} does not rest after {REST_TIMEOUT_SECONDS} s ({speed:F2} m/s)";
                    yield break;
                }
            }
        }

        // Stands the player before the target and tilts the camera until its centre ray points at it.
        private IEnumerator AimAtCoroutine(Vector3 target) {
            Rigidbody body = Player.GetComponent<CharacterMovableBase>().Body;
            Vector3 forward = Vector3.ProjectOnPlane(Player.transform.forward, Vector3.up).normalized;
            Vector3 stand = target - forward * AIM_DISTANCE;
            stand.y = body.position.y;
            body.position = stand;
            body.transform.position = stand;
            body.linearVelocity = Vector3.zero;
            // A client's grab is checked against its position on the server, which arrives a moment later.
            yield return _serverCatchUp;

            Camera camera = SessionQueries.Resolve<IGameCameraService>().OutputCamera;
            float deadline = Time.realtimeSinceStartup + AIM_TIMEOUT_SECONDS;
            while (true) {
                yield return null;
                Transform view = camera.transform;
                float error = Vector3.SignedAngle(view.forward, target - view.position, view.right);
                if (Mathf.Abs(error) < AIM_TOLERANCE_DEGREES || Time.realtimeSinceStartup > deadline)
                    break;

                CameraLookDriver.DebugPitch += error;
            }

            // LocalPlayerInteraction aims in Update, before this frame's camera pose: let it see the settled view.
            yield return null;
        }

        private IEnumerator WaitForCoroutine(Func<bool> condition, float timeoutSeconds, string target) {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline) {
                    Failure = $"timed out waiting for {target}";
                    yield break;
                }

                yield return null;
            }
        }
    }
}
#endif
