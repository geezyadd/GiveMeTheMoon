using Features.BugModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.GameCoreModule.Contracts;
using UnityEngine;

namespace Features.BugModule.Scripts.Behaviours {
    // A bug's way of flying on top of any goal: jerk - freeze - dash. In every cycle the bug first nearly hangs still,
    // then dashes off to one side of its line, while a small circle on its course makes it loop lightly. The steering
    // aims at a point a little ahead on the way to the goal, so the darting shows at any distance. Near the goal (a
    // landing, a grab) it flies straight in. Each bug has its own phase and its own sides.
    public sealed class BugDartBehaviour : IFieldAgentBehaviour, IWorldShiftFollower {
        // Spreads the phases of the bugs of a group (the golden ratio never repeats a phase).
        private const float PHASE_PER_ID = 0.618034f;
        private const int CYCLE_HASH = 73856093;
        private const int ID_HASH = 19349663;
        private const int SIDE_HASH_RANGE = 2048;
        private const float MIN_DISTANCE = 0.01f;

        private readonly IFieldAgentBehaviour _inner;
        private readonly BugConfiguration _bugConfiguration;

        public BugDartBehaviour(IFieldAgentBehaviour inner, BugConfiguration bugConfiguration) {
            _inner = inner;
            _bugConfiguration = bugConfiguration;
        }

        public FieldAgentGoal Evaluate(in FieldAgentBehaviourInput input) {
            FieldAgentGoal goal = _inner.Evaluate(input);
            Vector3 from = goal.IsShipSpace ? input.ShipLocalPosition : input.WorldPosition;
            Vector3 toGoal = goal.Point - from;
            float distance = toGoal.magnitude;
            bool isCalm = distance <= Mathf.Max(_bugConfiguration.CalmNearGoalMeters, _bugConfiguration.DartLookAheadMeters) || (goal.IsShipSpace && input.Ship.HasShip == false);
            if (isCalm)
                return goal;

            float cycles = input.Time / _bugConfiguration.DartCycleSeconds + input.AgentId * PHASE_PER_ID;
            float phase = cycles - Mathf.Floor(cycles);
            if (phase < _bugConfiguration.FreezeShare)
                return new FieldAgentGoal(goal.Point, goal.IsShipSpace, goal.ArriveRadius, goal.SpeedShare * _bugConfiguration.FreezeSpeedShare, goal.IsLandingOnArrival);

            Vector3 direction = toGoal / Mathf.Max(distance, MIN_DISTANCE);
            Vector3 aim = from + direction * _bugConfiguration.DartLookAheadMeters + CalculateDartOffset(direction, Mathf.FloorToInt(cycles), input);
            return new FieldAgentGoal(aim, goal.IsShipSpace, 0f, goal.SpeedShare, false);
        }

        public bool IsFinished(in FieldAgentBehaviourInput input) =>
            _inner.IsFinished(input);

        public void FollowWorldShift(Vector3 delta) {
            if (_inner is IWorldShiftFollower follower)
                follower.FollowWorldShift(delta);
        }

        // Sideways for this cycle's dash (a side of its own per cycle and bug) plus the point on the small loop.
        private Vector3 CalculateDartOffset(Vector3 direction, int cycle, in FieldAgentBehaviourInput input) {
            Vector3 side = Vector3.Cross(Vector3.up, direction);
            if (side.sqrMagnitude < MIN_DISTANCE)
                side = Vector3.right;

            side.Normalize();
            Vector3 up = Vector3.Cross(direction, side);
            float sideShare = CalculateSideShare(cycle, input.AgentId);
            float loopAngle = (input.Time / _bugConfiguration.LoopSeconds + input.AgentId * PHASE_PER_ID) * Mathf.PI * 2f;
            Vector3 loop = (side * Mathf.Cos(loopAngle) + up * Mathf.Sin(loopAngle)) * _bugConfiguration.LoopMeters;
            return side * (sideShare * _bugConfiguration.DashSidewaysMeters) + loop;
        }

        // From -1 to 1, the same for one bug in one cycle.
        private static float CalculateSideShare(int cycle, int agentId) {
            int hash = (cycle * CYCLE_HASH) ^ (agentId * ID_HASH);
            int bucket = ((hash % SIDE_HASH_RANGE) + SIDE_HASH_RANGE) % SIDE_HASH_RANGE;
            return bucket / (SIDE_HASH_RANGE * 0.5f) - 1f;
        }
    }
}
