using Features.FieldAgentModule.Scripts.Agents;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // Sit on the ship at a ship-local point and hold on there while the ship flies: in flight it flies to the point
    // and lands on it, on the deck it stays on it. Finished once it sits there.
    public sealed class AttachBehaviour : IFieldAgentBehaviour {
        private const float FULL_SPEED = 1f;

        private readonly Vector3 _shipPoint;
        private readonly float _arriveRadius;

        public AttachBehaviour(Vector3 shipPoint, float arriveRadius) {
            _shipPoint = shipPoint;
            _arriveRadius = arriveRadius;
        }

        public FieldAgentGoal Evaluate(in FieldAgentBehaviourInput input) =>
            new(_shipPoint, true, _arriveRadius, FULL_SPEED, true);

        public bool IsFinished(in FieldAgentBehaviourInput input) =>
            input.Mode == FieldAgentMode.Deck && FieldAgentGoalDistance.Calculate(input, _shipPoint, true) <= _arriveRadius;
    }
}
