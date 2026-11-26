using UnityEngine;

namespace Features.PlayerReplicaModule.Visual {
    // How far a copy's visible parts lag behind its body after a hidden jump, in world axes. A jump of the body leaves
    // the visual where it was (the offset takes the jump back), and the offset then shrinks smoothly to zero, so the
    // eye sees a quick slide instead of a jump. Tiny jumps are not worth hiding; very long ones (a respawn) are shown as
    // they are, and also drop any slide still going.
    public sealed class VisualLagOffset {
        // After the catch-up time this share of the offset is left: the slide is over to the eye.
        private const float LEFT_AFTER_CATCH_UP = 0.05f;
        private const float SETTLED = 0.001f;

        public Vector3 Offset { get; private set; }

        public void HideJump(Vector3 bodyJump, float threshold, float maxDistance) {
            float distance = bodyJump.magnitude;
            if (distance > maxDistance) {
                Offset = Vector3.zero;
                return;
            }

            if (distance < threshold)
                return;

            Offset -= bodyJump;
        }

        public void Tick(float deltaSeconds, float catchUpSeconds) {
            if (Offset == Vector3.zero)
                return;

            float keep = Mathf.Pow(LEFT_AFTER_CATCH_UP, deltaSeconds / catchUpSeconds);
            Offset *= keep;
            if (Offset.magnitude < SETTLED)
                Offset = Vector3.zero;
        }
    }
}
