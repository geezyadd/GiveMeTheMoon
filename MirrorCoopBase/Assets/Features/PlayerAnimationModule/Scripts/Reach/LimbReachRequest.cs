using UnityEngine;

namespace Features.PlayerAnimationModule.Reach {
    // One system's wish for a limb to reach somewhere (a slap for a head, a hand on an item, a look at something).
    // Taken from LimbReach.Request and kept for as long as the system needs the limb; its weight and goal can change
    // every physics step. A limb follows the request of the highest priority that has a weight above 0 (the latest one
    // on a tie); the others wait. Release gives the limb back: it eases back to the plain animation.
    public sealed class LimbReachRequest {
        private Transform _goalTransform;
        private Vector3 _goalOffset;
        private Vector3 _goalPoint;
        private bool _hasGoalPoint;
        private float _weight;

        // Systems take requests from LimbReach.Request, which counts the orders.
        public LimbReachRequest(LimbId limb, int priority, int order) {
            Limb = limb;
            Priority = priority;
            Order = order;
        }

        public LimbId Limb { get; }
        public int Priority { get; }
        // Later requests have higher orders.
        public int Order { get; }
        public bool IsReleased { get; private set; }

        // How much the limb follows this request, 0 (the plain animation) to 1 (the limb on the goal).
        public float Weight {
            get => IsReleased ? 0f : _weight;
            set => _weight = Mathf.Clamp01(value);
        }

        // Follows a transform (an item, another body's head) at an offset in its own space; a destroyed transform
        // is no goal.
        public void SetGoal(Transform goal, Vector3 localOffset = default) {
            _goalTransform = goal;
            _goalOffset = localOffset;
            _hasGoalPoint = false;
        }

        // A fixed point in the world.
        public void SetGoal(Vector3 worldPoint) {
            _goalTransform = null;
            _goalPoint = worldPoint;
            _hasGoalPoint = true;
        }

        public void Release() =>
            IsReleased = true;

        public bool TryGetGoal(out Vector3 worldPoint) {
            if (_goalTransform != null) {
                worldPoint = _goalTransform.TransformPoint(_goalOffset);
                return true;
            }

            worldPoint = _goalPoint;
            return _hasGoalPoint;
        }
    }
}
