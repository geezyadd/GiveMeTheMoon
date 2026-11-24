using System;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // How a body finds the ground, hovers over it, falls through the air and slows down.
    [Serializable]
    public sealed class RagdollStandSettings {
        [Header("Ground")]
        [Tooltip("Layers the body can stand on.")]
        public LayerMask GroundMask = ~0;
        [Tooltip("Steepest surface the body stands on, degrees.")]
        public float MaxStandSlope = 50f;
        [Tooltip("A rigidbody lighter than this is not stood on (crates, items); still and kinematic ones are.")]
        public float MinSupportMass = 500f;
        [Tooltip("Radius of the support probe cast down from the root limb.")]
        public float ProbeRadius = 0.15f;
        [Tooltip("The probe reaches the root limb's animated height plus this.")]
        public float ProbeExtraLength = 0.3f;

        [Header("Hover")]
        [Tooltip("Up / down acceleration on the hovered limbs when the height limb is a full smoothing band off.")]
        public float StandForce = 100f;
        [Tooltip("Height band, metres, over which the stand force grows from zero to full.")]
        public float StandSmoothing = 0.25f;

        [Header("In the air")]
        [Tooltip("Share of the full air gravity by time since the ground, 0..1 over the curve's time.")]
        public AnimationCurve AirGravityRamp = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Speeds the ramp up: curve time = seconds in the air * this.")]
        public float AirGravityRampSpeed = 1f;
        [Tooltip("Full air gravity, vertical acceleration. Large because the drag eats most of it every step.")]
        public float MaxAirGravity = -500f;
        [Tooltip("Below this control factor the body falls with Unity's own gravity instead.")]
        public float UnityGravityBelowControl = 0.9f;

        [Header("Drag")]
        [Tooltip("Share of the velocity kept per 1/60 s at full control; the same feel at any physics rate. " +
                 "Sets the top speed and stops the hover from bouncing. A limp body keeps all its velocity.")]
        public float VelocityKeptPer60th = 0.85f;
    }
}
