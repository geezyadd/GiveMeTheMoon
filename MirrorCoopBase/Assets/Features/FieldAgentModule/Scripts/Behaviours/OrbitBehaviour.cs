using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // Circle the ship: the goal is a ship-space point on a circle around it a little ahead of the agent, so the agent
    // keeps flying round and keeps up with the flying ship. Never finished; without a ship the agent holds where it is.
    public sealed class OrbitBehaviour : IFieldAgentBehaviour {
        private const float NO_ARRIVE_RADIUS = 0f;
        private const float MIN_OFFSET_SQR = 0.01f;

        private readonly float _radius;
        private readonly float _height;
        private readonly float _speedShare;
        private readonly float _leadDegrees;

        // A negative lead circles the other way.
        public OrbitBehaviour(float radius, float height, float speedShare, float leadDegrees) {
            _radius = radius;
            _height = height;
            _speedShare = speedShare;
            _leadDegrees = leadDegrees;
        }

        public FieldAgentGoal Evaluate(in FieldAgentBehaviourInput input) {
            if (input.Ship.HasShip == false)
                return new FieldAgentGoal(input.WorldPosition, false, NO_ARRIVE_RADIUS, _speedShare, false);

            Vector3 offset = input.ShipLocalPosition;
            offset.y = 0f;
            if (offset.sqrMagnitude < MIN_OFFSET_SQR)
                offset = Vector3.forward;

            Vector3 onCircle = Quaternion.AngleAxis(_leadDegrees, Vector3.up) * offset.normalized * _radius;
            return new FieldAgentGoal(onCircle + Vector3.up * _height, true, NO_ARRIVE_RADIUS, _speedShare, false);
        }

        public bool IsFinished(in FieldAgentBehaviourInput input) =>
            false;
    }
}
