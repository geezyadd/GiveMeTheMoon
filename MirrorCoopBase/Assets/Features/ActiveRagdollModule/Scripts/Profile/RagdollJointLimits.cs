using System;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // How far a limb's joint may turn from the bind pose, so a limp body bends like a body (a knee one way only) instead
    // of twisting freely. Angles are degrees of turn about the bend axis (Unity's rotation sense: positive = the way the
    // axis was chosen to mean flexion), twist about the bone, swing about the axis across both.
    [Serializable]
    public sealed class RagdollJointLimits {
        [Tooltip("The bend axis in the body root's space in the bind pose, chosen so a positive bend is the limb's " +
                 "flexion (for a person facing +Z: +X for the trunk and legs, ±Y for the arms). Zero: no anatomical " +
                 "limits, the joint uses the profile's Angular Limit on every axis.")]
        public Vector3 BendAxis;

        [Tooltip("Smallest bend about the bend axis, degrees (-177..0).")]
        [Range(-177f, 0f)] public float MinBend = -45f;

        [Tooltip("Largest bend about the bend axis, degrees (0..177).")]
        [Range(0f, 177f)] public float MaxBend = 45f;

        [Tooltip("Turn about the bone's own direction, each way, degrees.")]
        [Range(0f, 177f)] public float Twist = 20f;

        [Tooltip("Turn about the axis across the bend axis and the bone, each way, degrees.")]
        [Range(0f, 177f)] public float Swing = 20f;

        public bool IsSet => BendAxis.sqrMagnitude > 0.0001f;

        public RagdollJointLimits() { }

        public RagdollJointLimits(Vector3 bendAxis, float minBend, float maxBend, float twist, float swing) {
            BendAxis = bendAxis;
            MinBend = minBend;
            MaxBend = maxBend;
            Twist = twist;
            Swing = swing;
        }
    }
}
