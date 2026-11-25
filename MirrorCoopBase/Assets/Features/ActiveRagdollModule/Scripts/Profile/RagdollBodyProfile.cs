using System.Collections.Generic;
using UnityEngine;

namespace Features.ActiveRagdollModule {
    // Describes a ragdoll body as data, so any creature gets a ragdoll from its own profile without new code.
    // The first limb is the root: it has no joint and every other limb hangs on it through its parents.
    [CreateAssetMenu(fileName = "RagdollBodyProfile", menuName = "Ragdoll/Body Profile")]
    public sealed class RagdollBodyProfile : ScriptableObject {
        [SerializeField] private List<RagdollLimbProfile> _limbs = new();

        [Header("All limbs")]
        [Tooltip("Multiplies every limb's mass when the body is baked.")]
        [SerializeField] private float _massMultiplier = 1f;
        [Tooltip("Joint drive damper = spring * this.")]
        [SerializeField] private float _damperToSpringRatio = 0.1f;
        [Tooltip("Angular limit of every joint on each side, degrees. Near 180 the joint is free but cannot flip over.")]
        [SerializeField] private float _angularLimit = 177f;
        [SerializeField] private float _angularDamping = 0.05f;
        [SerializeField] private float _maxAngularVelocity = 50f;
        [Tooltip("Limbs two joints apart or siblings do not collide with each other, so overlapping capsules " +
                 "around a joint do not push the body apart.")]
        [SerializeField] private bool _ignoreNearbyLimbCollisions = true;

        [Header("Following the animated pose")]
        [Tooltip("Spring that pulls each limb toward its animated position (from the physical root limb), " +
                 "as acceleration per metre of error.")]
        [SerializeField] private float _poseForce = 150f;
        [Tooltip("Damps the pose spring: slows each limb's speed relative to the root limb, as acceleration per " +
                 "metre per second. The sway keeps its size and dies out sooner (0: no damping; about twice the " +
                 "square root of the pose force: no overshoot). Read every physics step, so it can be tuned in Play mode.")]
        [SerializeField, Min(0f)] private float _poseDamping = 12f;
        [Tooltip("Turns each limb, the root too, toward its animated world axes, as angular acceleration per " +
                 "degree of error. This is what keeps the body upright.")]
        [SerializeField] private float _poseTorque = 15f;
        [Tooltip("Damps only the root limb's (the hips') rotation against the animated pose's own rotation, on every " +
                 "axis, per second: the hips stop swinging like a pendulum (turning left / right, tilting) sooner after " +
                 "a jump or a landing, while turning toward the look keeps its speed, since the pose turns too. 0: none. " +
                 "Read every physics step, so it can be tuned in Play mode.")]
        [SerializeField, Min(0f)] private float _rootSwingDamping;
        [Tooltip("The pose turns toward the wanted facing (a player's look) at most this fast, degrees per second, so a " +
                 "fast spin of the look does not whirl the body round and fling its legs out. 0: at once. Read every " +
                 "physics step, so it can be tuned in Play mode.")]
        [SerializeField, Min(0f)] private float _maxFacingTurnDegreesPerSecond;

        [SerializeField] private RagdollStandSettings _stand = new();

        [Header("Baking")]
        [Tooltip("Multiplies every limb's capsule radius when the body is baked.")]
        [SerializeField] private float _radiusMultiplier = 1.3f;

        public List<RagdollLimbProfile> Limbs => _limbs;
        public float RadiusMultiplier => _radiusMultiplier;
        public float PoseForce => _poseForce;
        public float PoseDamping => _poseDamping;
        public float PoseTorque => _poseTorque;
        public float RootSwingDamping => _rootSwingDamping;
        public float MaxFacingTurnDegreesPerSecond => _maxFacingTurnDegreesPerSecond;
        public RagdollStandSettings Stand => _stand;
        public float MassMultiplier => _massMultiplier;
        public float DamperToSpringRatio => _damperToSpringRatio;
        public float AngularLimit => _angularLimit;
        public float AngularDamping => _angularDamping;
        public float MaxAngularVelocity => _maxAngularVelocity;
        public bool IgnoreNearbyLimbCollisions => _ignoreNearbyLimbCollisions;
    }
}
