using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // Hang at a world point with a light up and down sway. Never finished.
    public sealed class IdleBehaviour : IFieldAgentBehaviour, IWorldShiftFollower {
        private const float SWAY_SPEED_SHARE = 0.25f;

        private readonly float _swayMeters;
        private readonly float _swayFrequency;
        private Vector3 _anchor;

        public IdleBehaviour(Vector3 anchor, float swayMeters, float swayFrequency) {
            _anchor = anchor;
            _swayMeters = swayMeters;
            _swayFrequency = swayFrequency;
        }

        public FieldAgentGoal Evaluate(in FieldAgentBehaviourInput input) {
            float sway = Mathf.Sin((input.Time * _swayFrequency + input.AgentId) * Mathf.PI * 2f) * _swayMeters;
            return new FieldAgentGoal(_anchor + Vector3.up * sway, false, 0f, SWAY_SPEED_SHARE, false);
        }

        public bool IsFinished(in FieldAgentBehaviourInput input) =>
            false;

        public void FollowWorldShift(Vector3 delta) =>
            _anchor += delta;
    }
}
