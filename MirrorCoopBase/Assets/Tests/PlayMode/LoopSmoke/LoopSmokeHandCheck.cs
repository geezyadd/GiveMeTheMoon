#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.GrabModule.Scripts;
using Features.ShipModule.Scripts;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Tests.PlayMode.LoopSmoke {
    // Plays the hand the way a player does: aims the real third-person camera and presses interact / release.
    // Takes the radar off by hand, drops it on the deck, picks it up again and installs it back.
    public sealed class LoopSmokeHandCheck {
        private const float AIM_DISTANCE = 1.6f;
        private const float AIM_TOLERANCE_DEGREES = 0.5f;
        private const float AIM_TIMEOUT_SECONDS = 3f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float DROP_SETTLE_SECONDS = 1.5f;
        private const float HAND_POSE_TOLERANCE = 0.01f;

        private readonly ShipBase _ship;

        public LoopSmokeHandCheck(ShipBase ship) =>
            _ship = ship;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static LocalPlayerInteraction Interaction => Player.GetComponent<LocalPlayerInteraction>();
        private static GrabController Hand => Player.GetComponent<GrabController>();

        public IEnumerator ReinstallRadarByHandCoroutine() {
            ShipSocket socket = FindRadarSocket();
            Assert.IsTrue(socket.ServerTryInstall(ShipModuleType.Radar, ItemViewId.Radar), "Radar install failed.");
            try {
                CameraLookDriver.DebugPitchOverride = true;
                Grabbable radar = null;
                yield return TakeRadarOffCoroutine(socket, item => radar = item);
                yield return DropCoroutine(radar);
                yield return PickUpCoroutine(radar);
                yield return InstallCoroutine(socket, radar);
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }
        }

        private IEnumerator TakeRadarOffCoroutine(ShipSocket socket, Action<Grabbable> taken) {
            yield return AimAtCoroutine(TargetPoint(socket));
            Assert.IsInstanceOf<ShipUninstallInteractable>(Interaction.Target.Usable, "The installed radar is not offered for removal under the crosshair.");
            Interaction.Interact();
            yield return WaitForCoroutine(() => Hand.IsHolding && Hand.Held != null, "the removed radar in the hand");
            Assert.IsFalse(socket.IsOccupied, "The socket still has the radar after it was taken off.");

            Grabbable radar = Hand.Held;
            Assert.IsFalse(radar.CanBeGrabbed, "A held item can be grabbed by someone else.");
            yield return null;
            Assert.Less(Vector3.Distance(radar.transform.position, Hand.ArmPoint.position), HAND_POSE_TOLERANCE, "The held item does not follow the hand.");
            taken(radar);
        }

        private static IEnumerator DropCoroutine(Grabbable radar) {
            Interaction.ReleaseHeld();
            yield return WaitForCoroutine(() => Hand.IsHolding == false, "the hand to be empty after release");
            Assert.IsTrue(radar.CanBeGrabbed, "A dropped item cannot be grabbed again.");
            Assert.IsFalse(radar.GetComponent<Rigidbody>().isKinematic, "A dropped item does not fall on the server.");
            yield return new WaitForSeconds(DROP_SETTLE_SECONDS);
        }

        private IEnumerator PickUpCoroutine(Grabbable radar) {
            yield return AimAtCoroutine(TargetPoint(radar));
            Assert.AreSame(radar, Interaction.Target.Grabbable, "The dropped radar is not under the crosshair.");
            Interaction.Interact();
            yield return WaitForCoroutine(() => Hand.Held == radar, "the dropped radar back in the hand");
        }

        private IEnumerator InstallCoroutine(ShipSocket socket, Grabbable radar) {
            yield return AimAtCoroutine(TargetPoint(socket));
            Assert.IsInstanceOf<ShipInstallInteractable>(Interaction.Target.Usable, "The empty socket is not offered for install under the crosshair.");
            Interaction.Interact();
            yield return WaitForCoroutine(() => socket.IsOccupied && Hand.IsHolding == false && radar == null, "the radar installed from the hand");
            Assert.AreEqual(ItemViewId.Radar, socket.InstalledView, "The socket shows the wrong module.");
        }

        // Stands the player in front of the target and tilts the camera until its centre ray points at it.
        private static IEnumerator AimAtCoroutine(Vector3 target) {
            Transform player = Player.transform;
            Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
            Rigidbody body = Player.GetComponent<CharacterMovableBase>().Body;
            Vector3 stand = target - forward * AIM_DISTANCE;
            stand.y = body.position.y;
            body.position = stand;
            body.transform.position = stand;
            body.linearVelocity = Vector3.zero;

            IGameCameraService cameras = ResolveCameras();
            float deadline = Time.realtimeSinceStartup + AIM_TIMEOUT_SECONDS;
            while (true) {
                yield return null;
                Transform view = cameras.OutputCamera.transform;
                float error = Vector3.SignedAngle(view.forward, target - view.position, view.right);
                if (Mathf.Abs(error) < AIM_TOLERANCE_DEGREES)
                    break;

                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"The camera did not settle on {target}: {error:0.0} degrees off.");

                CameraLookDriver.DebugPitch += error;
            }

            // LocalPlayerInteraction aims in Update, before this frame's camera pose: let it see the settled view.
            yield return null;
        }

        private static IGameCameraService ResolveCameras() {
            foreach (SceneContext context in UnityEngine.Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None)) {
                if (context.Container.HasBinding<IGameCameraService>())
                    return context.Container.Resolve<IGameCameraService>();
            }

            throw new InvalidOperationException("No scene context binds IGameCameraService.");
        }

        private static IEnumerator WaitForCoroutine(Func<bool> condition, string target) {
            float deadline = Time.realtimeSinceStartup + COMMAND_TIMEOUT_SECONDS;
            while (condition() == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Timed out after {COMMAND_TIMEOUT_SECONDS} s waiting for {target}.");

                yield return null;
            }
        }

        private ShipSocket FindRadarSocket() {
            foreach (ShipSocket socket in _ship.Sockets) {
                if (socket.AcceptedType == ShipModuleType.Radar)
                    return socket;
            }

            throw new InvalidOperationException("The ship has no radar socket.");
        }

        private static Vector3 TargetPoint(Component target) =>
            target.GetComponentInChildren<Collider>().bounds.center;
    }
}
#endif
