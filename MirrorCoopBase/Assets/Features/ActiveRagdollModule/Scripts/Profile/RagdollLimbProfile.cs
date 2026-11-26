using System;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // One physical bone of a body: where it is, how heavy and stiff it is, and the shape of its capsule.
    [Serializable]
    public sealed class RagdollLimbProfile {
        [Tooltip("Path of the bone from the body root, as in the hierarchy (e.g. Armature/Hips/Spine).")]
        public string BonePath;

        [Tooltip("The bone where the capsule ends. Empty: the capsule continues the parent limb's direction for " +
                 "Fallback Length.")]
        public string EndBonePath;

        public float Mass = 10f;

        [Tooltip("Joint drive spring toward the animated pose. The damper is the spring times the profile's ratio.")]
        public float DriveSpring = 1000f;

        [Tooltip("Capsule radius in world units.")]
        public float Radius = 0.05f;

        [Tooltip("Capsule length in world units when there is no end bone.")]
        public float FallbackLength = 0.15f;

        [Tooltip("The hover force holds this limb at standing height (for a person: hips, spine, chest, head).")]
        public bool Hovered;

        [Tooltip("The limb whose height above the ground is kept at its animated height (for a person: the head). " +
                 "One per body.")]
        public bool HeightReference;

        [Tooltip("How far the joint may turn (applied when baked or by the inspector's \"Apply joint limits\").")]
        public RagdollJointLimits Limits = new();
    }
}
