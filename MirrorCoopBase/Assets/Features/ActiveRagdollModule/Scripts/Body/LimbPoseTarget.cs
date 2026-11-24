using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Where the animation wants one limb on this physics step.
    public readonly struct LimbPoseTarget {
        // Rotation relative to the parent limb (not the parent transform: bones between limbs are skipped).
        public readonly Quaternion RotationInParent;
        // World axes of the limb in the animated pose.
        public readonly Vector3 Forward;
        public readonly Vector3 Up;
        // World offset from the root limb in the animated pose; zero for the root itself.
        public readonly Vector3 OffsetFromRoot;
        // Height above the animator's origin (where the animation stands on the ground).
        public readonly float Height;

        public LimbPoseTarget(Quaternion rotationInParent, Vector3 forward, Vector3 up, Vector3 offsetFromRoot,
            float height) {
            RotationInParent = rotationInParent;
            Forward = forward;
            Up = up;
            OffsetFromRoot = offsetFromRoot;
            Height = height;
        }
    }
}
