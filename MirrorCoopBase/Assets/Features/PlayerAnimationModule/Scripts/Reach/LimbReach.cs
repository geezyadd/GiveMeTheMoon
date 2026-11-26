using System.Collections.Generic;
using Features.ActiveRagdollModule;
using UnityEngine;

namespace Features.PlayerAnimationModule.Reach {
    // Lets any system point a limb of the body at something over the animation: a slap's hand at a head, a hand on an
    // item, the head looking at a thing. The limb's constraint is in the rig the ragdoll's AnimatedPoseSampler builds
    // into the animator's graph, so the limbs follow the reaching pose like any pose.
    // A system takes a LimbReachRequest (Request), sets its weight and goal every physics step and releases it when
    // done; each limb follows its winning request (LimbReachRule.PickWinner), its weight eased in and out and its goal
    // smoothed, so a switch between requests or a release never snaps the limb.
    // Works on any humanoid ragdoll that has the chains; one physics step after the request changes.
    public sealed class LimbReach : MonoBehaviour {
        [SerializeField] private AnimatedPoseSampler _sampler;
        [SerializeField] private LimbReachChain[] _chains = new LimbReachChain[0];
        [Tooltip("A limb's weight goes the whole way 0 to 1 in this time, seconds: a switch or a release does not snap.")]
        [SerializeField, Min(0f)] private float _blendSeconds = 0.08f;
        [Tooltip("How softly a limb's goal follows a moving target, seconds: a jittering head does not shake the arm.")]
        [SerializeField, Min(0f)] private float _goalSmoothingSeconds = 0.03f;

        private readonly List<LimbReachRequest> _requests = new();
        private int _nextOrder;

        public LimbReachRequest Request(LimbId limb, int priority) {
            if (HasLimb(limb) == false)
                Debug.LogError($"Reach: {name} has no chain for {limb}; the request is never shown.", this);

            LimbReachRequest request = new LimbReachRequest(limb, priority, _nextOrder++);
            _requests.Add(request);
            return request;
        }

        public bool HasLimb(LimbId limb) {
            foreach (LimbReachChain chain in _chains)
                if (chain.Limb == limb && chain.IsValid)
                    return true;

            return false;
        }

        private void OnEnable() =>
            _sampler.RigEvaluating += HandleRigEvaluating;

        private void OnDisable() {
            _sampler.RigEvaluating -= HandleRigEvaluating;
            foreach (LimbReachChain chain in _chains)
                chain.Reset();
        }

        private void HandleRigEvaluating(Vector3 animatedPoseShift) {
            _requests.RemoveAll(request => request.IsReleased);
            float deltaTime = Time.fixedDeltaTime;
            foreach (LimbReachChain chain in _chains) {
                if (chain.IsValid == false)
                    continue;

                LimbReachRequest winner = LimbReachRule.PickWinner(_requests, chain.Limb);
                chain.Step(winner, animatedPoseShift, _blendSeconds, _goalSmoothingSeconds, deltaTime);
            }
        }
    }
}
