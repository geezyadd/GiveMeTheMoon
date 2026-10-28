using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Deck {
    // Walking in ship space along the corners of a cached NavMesh path to the behaviour's goal: the agent only ever
    // stands on a NavMesh corner or between two, so it never leaves the deck or sinks into it. The path is computed
    // again when the goal moves or it gets old.
    public sealed class FieldAgentDeckStepService : IFieldAgentDeckStepService {
        private const float MIN_FLAT_SQR = 0.0001f;

        private readonly IFieldAgentDeckNavigation _fieldAgentDeckNavigation;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;

        public FieldAgentDeckStepService(IFieldAgentDeckNavigation fieldAgentDeckNavigation, FieldAgentConfiguration fieldAgentConfiguration) {
            _fieldAgentDeckNavigation = fieldAgentDeckNavigation;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public void Step(int index, FieldAgentServerModel model, FieldAgentProfile profile, in FieldAgentShipFrame ship, float time, float deltaTime) {
            ref FieldAgentState state = ref model.GetState(index);
            IFieldAgentBehaviour behaviour = model.GetBehaviour(index);
            FieldAgentBehaviourInput input = new(state.Id, state.Mode, ship.ToWorldPoint(state.Position), state.Position, ship, time);
            FieldAgentGoal goal = behaviour.Evaluate(input);
            state.IsBehaviourFinished = behaviour.IsFinished(input);
            Vector3 goalLocal = goal.IsShipSpace ? goal.Point : ship.ToShipPoint(goal.Point);

            FieldAgentDeckPath path = model.GetDeckPath(index);
            path.AddAge(deltaTime);
            if (NeedsPath(path, goalLocal))
                _fieldAgentDeckNavigation.TryFindPath(state.Position, goalLocal, path);

            Walk(ref state, path, profile, deltaTime);
        }

        private bool NeedsPath(FieldAgentDeckPath path, Vector3 goalLocal) =>
            path.HasPath == false
            || path.Age >= _fieldAgentConfiguration.DeckRepathSeconds
            || Vector3.Distance(path.Goal, goalLocal) > _fieldAgentConfiguration.DeckRepathMeters;

        private void Walk(ref FieldAgentState state, FieldAgentDeckPath path, FieldAgentProfile profile, float deltaTime) {
            Vector3 previous = state.Position;
            float stepLeft = profile.DeckWalkSpeed * deltaTime;
            while (stepLeft > 0f && path.IsAtEnd == false) {
                Vector3 corner = path.Corners[path.NextCorner];
                float distance = Vector3.Distance(state.Position, corner);
                if (distance <= stepLeft + _fieldAgentConfiguration.DeckCornerMeters) {
                    state.Position = corner;
                    stepLeft -= distance;
                    path.PassCorner();
                    continue;
                }

                state.Position = Vector3.MoveTowards(state.Position, corner, stepLeft);
                stepLeft = 0f;
            }

            state.Velocity = deltaTime > 0f ? (state.Position - previous) / deltaTime : Vector3.zero;
            Turn(ref state, profile, deltaTime);
        }

        private static void Turn(ref FieldAgentState state, FieldAgentProfile profile, float deltaTime) {
            Vector3 flat = state.Velocity;
            flat.y = 0f;
            if (flat.sqrMagnitude < MIN_FLAT_SQR)
                return;

            Quaternion facing = Quaternion.LookRotation(flat, Vector3.up);
            state.Rotation = Quaternion.RotateTowards(state.Rotation, facing, profile.DeckTurnRateDegrees * deltaTime);
        }
    }
}
