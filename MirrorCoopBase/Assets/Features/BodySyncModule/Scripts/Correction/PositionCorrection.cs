using UnityEngine;

namespace Features.BodySyncModule.Correction {
    // Turns the error between a copy and its target (target minus copy, in the platform's axes) into this physics
    // step's move. Horizontally a fixed share of the error goes each step, so the copy slides in without a visible
    // pull; while the owner presses no walk key the copy is left alone near the target and pulled only gently past
    // that, so a copy that ran a little past its stopped owner settles back unnoticed. Vertically nothing happens
    // inside the dead zone, the pull grows across it and is capped per step, so the network does not fight the copy's
    // own jumps and falls; when the copy is stuck (see GroundMismatchTimer) the dead zone is dropped. A very large
    // error snaps.
    public static class PositionCorrection {
        public static PositionCorrectionStep Compute(Vector3 error, bool ownerSteers, bool ignoreVerticalDeadZone, BodyCorrectionSettings settings) {
            if (error.magnitude > settings.SnapDistance)
                return PositionCorrectionStep.Snap(error);

            Vector3 horizontal = ComputeHorizontalMove(new Vector3(error.x, 0f, error.z), ownerSteers, settings);
            float vertical = ComputeVerticalMove(error.y, ignoreVerticalDeadZone, settings);
            return PositionCorrectionStep.Move(horizontal + Vector3.up * vertical);
        }

        private static Vector3 ComputeHorizontalMove(Vector3 horizontalError, bool ownerSteers, BodyCorrectionSettings settings) {
            if (ownerSteers)
                return horizontalError * settings.PositionPull;

            if (horizontalError.magnitude <= settings.IdleDeadZone)
                return Vector3.zero;

            return horizontalError * settings.IdlePositionPull;
        }

        private static float ComputeVerticalMove(float verticalError, bool ignoreDeadZone, BodyCorrectionSettings settings) {
            float distance = Mathf.Abs(verticalError);
            float strength = ignoreDeadZone
                ? 1f
                : Mathf.InverseLerp(settings.VerticalDeadZoneMin, settings.VerticalDeadZoneMax, distance);

            // Never past the target: a small error is closed, not overshot.
            float move = Mathf.Min(distance, settings.VerticalPullPerStep) * strength;
            return Mathf.Sign(verticalError) * move;
        }
    }
}
