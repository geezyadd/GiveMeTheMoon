using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Features.PlayerAnimationModule.Reach {
    // One limb's constraint in the body's rig (a two bone IK for a hand, an aim for the head) and the transform the
    // constraint reaches for. LimbReach moves the goal and sets the constraint's weight; nothing else should.
    [Serializable]
    public sealed class LimbReachChain {
        [SerializeField] private LimbId _limb;
        [Tooltip("A constraint of the rig the ragdoll's AnimatedPoseSampler builds (TwoBoneIKConstraint, MultiAimConstraint, ...).")]
        [SerializeField] private MonoBehaviour _constraint;
        [Tooltip("The constraint's target. Not under the animated model, or the animation would move it.")]
        [SerializeField] private Transform _goal;

        [NonSerialized] private float _shownWeight;
        [NonSerialized] private Vector3 _shownGoal;

        public LimbId Limb => _limb;
        public bool IsValid => _constraint is IRigConstraint && _goal != null;

        // Just before the rig is evaluated. The goal is moved by the shift of the animated pose from the real body, so it
        // stands to the animated pose as it stands to the real body. While the weight fades out the last goal is kept.
        public void Step(LimbReachRequest winner, Vector3 animatedPoseShift, float blendSeconds, float smoothingSeconds,
            float deltaTime) {
            float wanted = 0f;
            if (winner != null && winner.TryGetGoal(out Vector3 goal)) {
                wanted = winner.Weight;
                _shownGoal = _shownWeight <= 0f ? goal : LimbReachRule.StepGoal(_shownGoal, goal, smoothingSeconds, deltaTime);
            }

            _shownWeight = LimbReachRule.StepWeight(_shownWeight, wanted, blendSeconds, deltaTime);
            if (_shownWeight > 0f)
                _goal.position = _shownGoal + animatedPoseShift;
            ((IRigConstraint)_constraint).weight = _shownWeight;
        }

        public void Reset() {
            _shownWeight = 0f;
            if (_constraint is IRigConstraint constraint)
                constraint.weight = 0f;
        }
    }
}
