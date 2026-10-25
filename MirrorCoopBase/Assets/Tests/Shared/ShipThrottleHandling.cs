#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShipModule.Scripts.Throttle;
using Mirror;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Shared {
    // Plays the throttle handle as a player does: stands behind it on the deck facing the nose, tilts the real camera
    // onto the grip, holds interact on it, moves the mouse forward through the interaction's look input until the push
    // is done, then lets go. Used by the loop test and the coop client bot (no NUnit there): a failure is a message.
    public sealed class ShipThrottleHandling {
        private const float BEHIND_HANDLE_METERS = 1.1f;
        private const float AIM_TOLERANCE_DEGREES = 0.5f;
        private const float AIM_TIMEOUT_SECONDS = 3f;
        private const float COMMAND_TIMEOUT_SECONDS = 5f;
        private const float PUSH_TIMEOUT_SECONDS = 20f;
        // Mouse look per frame while pushing (pixels).
        private const float PUSH_LOOK = 12f;

        private readonly ShipBase _ship;

        // Null while every step worked; otherwise what went wrong, and the push stopped there.
        public string Failure { get; private set; }
        public bool WasViewLockedWhileHeld { get; private set; }
        public float ReleasedPosition { get; private set; }

        private static NetworkIdentity Player => SessionQueries.LocalPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();
        private static IReadOnlyShipThrottleModel Throttle => SessionQueries.Resolve<IReadOnlyShipThrottleModel>();

        public ShipThrottleHandling(ShipBase ship) =>
            _ship = ship;

        public IEnumerator PushHandleCoroutine(Func<bool> isPushedFarEnough) {
            Failure = null;
            ShipThrottleInteractable handle = Object.FindAnyObjectByType<ShipThrottleInteractable>();
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return AimAtHandleCoroutine(handle);
                if (Failure != null)
                    yield break;

                if (Interaction.Target.Usable != handle) {
                    Failure = "the throttle handle is not under the crosshair";
                    yield break;
                }

                Interaction.Interact();
                string playerKey = Player.GetComponent<ShipRider>().Key.Id;
                yield return WaitForCoroutine(() => Throttle.HolderKey == playerKey, COMMAND_TIMEOUT_SECONDS, "the server to give the handle to this player");
                WasViewLockedWhileHeld = SessionQueries.Resolve<CameraLookLockModel>().IsLocked;
                yield return WaitForCoroutine(() => Push(isPushedFarEnough), PUSH_TIMEOUT_SECONDS, "the push to be done");
                Interaction.EndInteract();
                yield return WaitForCoroutine(() => string.IsNullOrEmpty(Throttle.HolderKey), COMMAND_TIMEOUT_SECONDS, "the handle let go on release");
                ReleasedPosition = Throttle.Position;
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }
        }

        private static bool Push(Func<bool> isPushedFarEnough) {
            Interaction.MoveHeldControl(Vector2.up * PUSH_LOOK);
            return isPushedFarEnough();
        }

        private IEnumerator AimAtHandleCoroutine(ShipThrottleInteractable handle) {
            Vector3 target = handle.GetComponent<Collider>().bounds.center;
            Rigidbody body = Player.GetComponent<CharacterMovableBase>().Body;
            Vector3 forward = Vector3.ProjectOnPlane(_ship.transform.forward, Vector3.up).normalized;
            Vector3 stand = target - forward * BEHIND_HANDLE_METERS;
            stand.y = body.position.y;
            Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
            body.position = stand;
            body.rotation = facing;
            body.transform.SetPositionAndRotation(stand, facing);
            body.linearVelocity = Vector3.zero;

            Camera camera = SessionQueries.Resolve<IGameCameraService>().OutputCamera;
            float deadline = Time.realtimeSinceStartup + AIM_TIMEOUT_SECONDS;
            while (true) {
                yield return null;
                Transform view = camera.transform;
                float error = Vector3.SignedAngle(view.forward, target - view.position, view.right);
                if (Mathf.Abs(error) < AIM_TOLERANCE_DEGREES)
                    break;

                if (Time.realtimeSinceStartup > deadline) {
                    Failure = $"the camera did not settle on the throttle handle: {error:0.0} degrees off";
                    yield break;
                }

                CameraLookDriver.DebugPitch += error;
            }

            // LocalPlayerInteraction aims in Update, before this frame's camera pose: let it see the settled view.
            yield return null;
        }

        private IEnumerator WaitForCoroutine(Func<bool> condition, float timeoutSeconds, string target) {
            if (Failure != null)
                yield break;

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
