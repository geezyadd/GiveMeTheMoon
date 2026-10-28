using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // Drag a cargo to a world point, slower than free flight; the cargo follows the agent. Finished at the point.
    public sealed class CarryBehaviour : IFieldAgentBehaviour, IWorldShiftFollower {
        private readonly IFieldAgentCargo _cargo;
        private readonly float _arriveRadius;
        private readonly float _speedShare;
        private Vector3 _destination;

        public CarryBehaviour(IFieldAgentCargo cargo, Vector3 destination, float arriveRadius, float speedShare) {
            _cargo = cargo;
            _destination = destination;
            _arriveRadius = arriveRadius;
            _speedShare = speedShare;
        }

        public FieldAgentGoal Evaluate(in FieldAgentBehaviourInput input) {
            _cargo.FollowCarrier(input.WorldPosition);
            return new FieldAgentGoal(_destination, false, _arriveRadius, _speedShare, false);
        }

        public bool IsFinished(in FieldAgentBehaviourInput input) =>
            Vector3.Distance(input.WorldPosition, _destination) <= _arriveRadius;

        public void FollowWorldShift(Vector3 delta) =>
            _destination += delta;
    }
}
