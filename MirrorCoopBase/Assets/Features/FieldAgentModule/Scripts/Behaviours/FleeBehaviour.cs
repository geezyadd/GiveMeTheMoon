using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // Get away from the ship: the goal lies straight away from it. Finished once the agent is the flee distance off.
    public sealed class FleeBehaviour : IFieldAgentBehaviour {
        private const float NO_ARRIVE_RADIUS = 0f;
        private const float MIN_AWAY_SQR = 0.01f;

        private readonly float _fleeDistance;
        private readonly float _speedShare;

        public FleeBehaviour(float fleeDistance, float speedShare) {
            _fleeDistance = fleeDistance;
            _speedShare = speedShare;
        }

        public FieldAgentGoal Evaluate(in FieldAgentBehaviourInput input) {
            if (input.Ship.HasShip == false)
                return new FieldAgentGoal(input.WorldPosition, false, NO_ARRIVE_RADIUS, _speedShare, false);

            Vector3 away = input.WorldPosition - input.Ship.Position;
            away = away.sqrMagnitude > MIN_AWAY_SQR ? away.normalized : input.Ship.Rotation * Vector3.back;
            return new FieldAgentGoal(input.Ship.Position + away * (_fleeDistance * 2f), false, NO_ARRIVE_RADIUS, _speedShare, false);
        }

        public bool IsFinished(in FieldAgentBehaviourInput input) =>
            input.Ship.HasShip == false || Vector3.Distance(input.WorldPosition, input.Ship.Position) >= _fleeDistance;
    }
}
