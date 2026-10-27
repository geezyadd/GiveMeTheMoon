#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Services;
using Features.InputModule.Realization.Scripts.Generated;
using UnityEngine;

namespace Tests.Shared {
    // Plays a weapon in the hand as a player does: turns the view through the look input (yaw) and the test pitch onto
    // a point, and clicks the fire button through the input service's Fire action, as the mouse sends it. Used by the
    // loop test and the coop client bot (no NUnit there). The caller turns CameraLookDriver.DebugPitchOverride on.
    public static class HandWeaponAiming {
        public const float AIM_TOLERANCE_DEGREES = 0.4f;
        // Mouse look pixels per degree of turn, a little under the camera's sensitivity so the turn never overshoots.
        private const float LOOK_PIXELS_PER_DEGREE = 4f;
        private const float MAX_LOOK_PIXELS = 60f;

        private static float _turnedLookPixels;

        // One step of the turn; true once the view is on the target.
        public static bool KeepAiming(Vector3 target) {
            Transform view = SessionQueries.Resolve<IGameCameraService>().OutputCamera.transform;
            Vector3 toTarget = target - view.position;
            float yawError = Vector3.SignedAngle(Vector3.ProjectOnPlane(view.forward, Vector3.up), Vector3.ProjectOnPlane(toTarget, Vector3.up), Vector3.up);
            float pitchError = Vector3.SignedAngle(view.forward, Vector3.ProjectOnPlane(toTarget, view.right), view.right);
            if (Mathf.Abs(yawError) < AIM_TOLERANCE_DEGREES && Mathf.Abs(pitchError) < AIM_TOLERANCE_DEGREES)
                return true;

            Turn(yawError);
            CameraLookDriver.DebugPitch += pitchError;
            return false;
        }

        // Gives back every turn KeepAiming made, in one look input: the view yaw is the sum of the look input, so the
        // later steps that aim with the pitch alone find the view along the body again.
        public static void TurnViewBack() {
            SessionQueries.Resolve<IInputService>().Look.VectorChangedPerformed?.Invoke(new Vector2(-_turnedLookPixels, 0f));
            _turnedLookPixels = 0f;
        }

        // Down and up on the fire button; a hand weapon fires on the press.
        public static IEnumerator ClickFireCoroutine() {
            IInputService input = SessionQueries.Resolve<IInputService>();
            input.Fire.Started?.Invoke();
            yield return null;
            input.Fire.Canceled?.Invoke();
            yield return null;
        }

        private static void Turn(float yawError) {
            float look = Mathf.Clamp(yawError * LOOK_PIXELS_PER_DEGREE, -MAX_LOOK_PIXELS, MAX_LOOK_PIXELS);
            _turnedLookPixels += look;
            SessionQueries.Resolve<IInputService>().Look.VectorChangedPerformed?.Invoke(new Vector2(look, 0f));
        }
    }
}
#endif
