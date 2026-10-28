using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // Go to a point: a fixed world point, or a ship-space point that moves with the ship (the ship itself, a module on
    // the deck). Finished within the arrive radius.
    public sealed class SeekBehaviour : IFieldAgentBehaviour, IWorldShiftFollower {
        private readonly bool _isShipSpace;
        private readonly float _arriveRadius;
        private readonly float _speedShare;
        private Vector3 _point;

        public SeekBehaviour(Vector3 point, bool isShipSpace, float arriveRadius, float speedShare) {
            _point = point;
            _isShipSpace = isShipSpace;
            _arriveRadius = arriveRadius;
            _speedShare = speedShare;
        }

        public FieldAgentGoal Evaluate(in FieldAgentBehaviourInput input) =>
            new(_point, _isShipSpace, _arriveRadius, _speedShare, false);

        public bool IsFinished(in FieldAgentBehaviourInput input) =>
            FieldAgentGoalDistance.Calculate(input, _point, _isShipSpace) <= _arriveRadius;

        public void FollowWorldShift(Vector3 delta) {
            if (_isShipSpace == false)
                _point += delta;
        }
    }
}
