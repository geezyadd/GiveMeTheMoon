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

        [Tooltip("After a jump the body does not stand for this long, so the hover does not cancel the jump.")]
        public float StandPauseAfterJump = 0.3f;

        [Header("Held on a spot (a seat)")]
        [Tooltip("Horizontal pull toward the held spot, acceleration per metre off it. Gentle: the body may sway, it " +
                 "only must not drift away over time.")]
        public float HeldSpotSpring = 4f;
        [Tooltip("Damping of the body's horizontal speed relative to its carrier while held, per second.")]
        public float HeldSpotDamping = 4f;
        [Tooltip("Closer than this to the held spot, metres, the body is left alone.")]
        public float HeldSpotTolerance = 0.05f;

        [Header("Hover")]
        [Tooltip("Up / down acceleration on the hovered limbs when the height limb is a full smoothing band off.")]
        public float StandForce = 100f;
        [Tooltip("Height band, metres, over which the stand force grows from zero to full.")]
        public float StandSmoothing = 0.25f;

        [Header("Legs reaching the floor")]
        [Tooltip("The legs may hang this far above the floor, metres, before the trunk is lowered onto them.")]
        [Min(0f)]
        public float LegReachTolerance = 0.05f;
        [Tooltip("How fast the trunk drops onto legs that no longer reach the floor: the time constant, seconds.")]
        [Min(0f)]
        public float LegReachSmoothing = 0.1f;
        [Tooltip("Once a leg reaches the floor again, the trunk rises back at this speed, metres per second.")]
        [Min(0f)]
        public float LegReachRiseSpeed = 0.5f;
        [Tooltip("The trunk is never lowered more than this for its legs, metres.")]
        [Min(0f)]
        public float MaxLegReachDrop = 0.6f;

        [Header("Landing sag")]
        [Tooltip("On landing the hover lets the trunk sink this many metres per m/s of the landing speed, then lifts it " +
                 "back. 0: no sag. Read every physics step, so it can be tuned in Play mode.")]
        public float LandingSagPerSpeed = 0.03f;
        [Tooltip("The deepest landing sag, metres.")]
        public float MaxLandingSag = 0.3f;
        [Tooltip("How long a landing sag lasts from touching down until the trunk is back up, seconds.")]
        public float LandingSagSeconds = 0.35f;
        [Tooltip("Share of the sag time spent sinking (the rest is coming back up), 0..1.")]
        [Range(0.05f, 0.95f)]
        public float LandingSagSinkShare = 0.3f;

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
