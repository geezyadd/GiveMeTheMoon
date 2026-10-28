using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Deck;
using Features.FlightFieldModule.Contracts;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Steering {
    // Flight: seek + rock avoidance + separation + wander, limited by the profile, then a push out of any rock sphere
    // as the last guard. A goal in ship space (the ship, an orbit round it, a deck point) also gets the ship's velocity,
    // so the top speed is the agent's speed against the ship and it keeps up with a ship in cruise. A landing goal
    // starts the landing once the agent is at its approach point above the deck.
    public sealed class FieldAgentFlightStepService : IFieldAgentFlightStepService {
        private const float MIN_SPEED_SQR = 0.01f;

        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IFieldAgentSteering _fieldAgentSteering;
        private readonly IFieldAgentRockAvoidance _fieldAgentRockAvoidance;
        private readonly IFieldAgentSeparation _fieldAgentSeparation;
        private readonly IFieldAgentWander _fieldAgentWander;
        private readonly IFieldAgentTransitionService _fieldAgentTransitionService;
        private readonly IFieldAgentDeckNavigation _fieldAgentDeckNavigation;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly FieldAgentDiagnosticsModel _fieldAgentDiagnosticsModel;

        public FieldAgentFlightStepService(
            IFieldRockQuery fieldRockQuery,
            IFieldAgentSteering fieldAgentSteering,
            IFieldAgentRockAvoidance fieldAgentRockAvoidance,
            IFieldAgentSeparation fieldAgentSeparation,
            IFieldAgentWander fieldAgentWander,
            IFieldAgentTransitionService fieldAgentTransitionService,
            IFieldAgentDeckNavigation fieldAgentDeckNavigation,
            FieldAgentConfiguration fieldAgentConfiguration,
            FieldAgentDiagnosticsModel fieldAgentDiagnosticsModel) {
            _fieldRockQuery = fieldRockQuery;
            _fieldAgentSteering = fieldAgentSteering;
            _fieldAgentRockAvoidance = fieldAgentRockAvoidance;
            _fieldAgentSeparation = fieldAgentSeparation;
            _fieldAgentWander = fieldAgentWander;
            _fieldAgentTransitionService = fieldAgentTransitionService;
            _fieldAgentDeckNavigation = fieldAgentDeckNavigation;
            _fieldAgentConfiguration = fieldAgentConfiguration;
            _fieldAgentDiagnosticsModel = fieldAgentDiagnosticsModel;
        }

        public void Step(int index, FieldAgentServerModel model, FieldAgentProfile profile, in FieldAgentShipFrame ship, float time, float deltaTime) {
            ref FieldAgentState state = ref model.GetState(index);
            IFieldAgentBehaviour behaviour = model.GetBehaviour(index);
            Vector3 shipLocal = ship.HasShip ? ship.ToShipPoint(state.Position) : state.Position;
            FieldAgentBehaviourInput input = new(state.Id, state.Mode, state.Position, shipLocal, ship, time);
            FieldAgentGoal goal = behaviour.Evaluate(input);
            state.IsBehaviourFinished = behaviour.IsFinished(input);
            if (goal.IsShipSpace && ship.HasShip == false)
                goal = new FieldAgentGoal(state.Position, false, goal.ArriveRadius, goal.SpeedShare, false);

            Vector3 target = CalculateTarget(goal, ship, profile);
            if (IsAtLandingApproach(goal, ship, state.Position, target, profile)) {
                _fieldAgentTransitionService.BeginLanding(ref state, ship, CalculateDeckPoint(goal.Point));
                return;
            }

            IReadOnlyList<FieldRockDescription> rocks = ReadRocksAround(index, model, profile, deltaTime);
            Vector3 desired = CalculateDesiredVelocity(index, model, profile, goal, target, ship, rocks, time);
            Fly(ref state, desired, rocks, profile, deltaTime);
        }

        private Vector3 CalculateDesiredVelocity(int index, FieldAgentServerModel model, FieldAgentProfile profile, FieldAgentGoal goal, Vector3 target, in FieldAgentShipFrame ship, IReadOnlyList<FieldRockDescription> rocks, float time) {
            ref FieldAgentState state = ref model.GetState(index);
            float sideSign = (state.Id & 1) == 0 ? 1f : -1f;
            Vector3 seek = _fieldAgentSteering.CalculateSeek(state.Position, target, goal.SpeedShare, profile);
            Vector3 relative = seek * _fieldAgentConfiguration.SeekWeight
                + _fieldAgentRockAvoidance.CalculateAvoidance(state.Position, state.Velocity, seek, rocks, profile, sideSign) * _fieldAgentConfiguration.AvoidanceWeight
                + _fieldAgentSeparation.CalculateSeparation(index, model, profile) * _fieldAgentConfiguration.SeparationWeight
                + _fieldAgentWander.CalculateWander(state.NoisePhase, time, profile);
            relative = Vector3.ClampMagnitude(relative, profile.MaxSpeed);
            return goal.IsShipSpace ? relative + ship.Velocity : relative;
        }

        private void Fly(ref FieldAgentState state, Vector3 desired, IReadOnlyList<FieldRockDescription> rocks, FieldAgentProfile profile, float deltaTime) {
            state.Velocity = _fieldAgentSteering.LimitVelocity(state.Velocity, desired, profile, deltaTime);
            state.Position += state.Velocity * deltaTime;
            if (_fieldAgentRockAvoidance.PushOutOfRocks(ref state.Position, ref state.Velocity, rocks, profile))
                _fieldAgentDiagnosticsModel.RecordRockPushOut();
            if (state.Velocity.sqrMagnitude > MIN_SPEED_SQR)
                state.Rotation = Quaternion.LookRotation(state.Velocity, Vector3.up);
        }

        // All around the agent, not only ahead (the course the avoidance checks is the goal's, which may differ from the
        // flight direction), and wide enough for where the agent can fly until its next query. The query intervals of
        // the agents differ a little by id, so a flock spawned together does not query in the same tick.
        private IReadOnlyList<FieldRockDescription> ReadRocksAround(int index, FieldAgentServerModel model, FieldAgentProfile profile, float deltaTime) {
            ref FieldAgentState state = ref model.GetState(index);
            FieldAgentRockCache cache = model.GetRockCache(index);
            cache.AddAge(deltaTime);
            float interval = CalculateQueryInterval(state.Id);
            float reach = _fieldAgentRockAvoidance.CalculateLookAheadMeters(state.Velocity, profile) + profile.Radius + profile.RockClearance + _fieldAgentConfiguration.RockQueryMarginMeters;
            bool isStale = cache.Age >= interval || Vector3.Distance(state.Position, cache.Center) + reach > cache.Radius;
            if (isStale == false)
                return cache.Rocks;

            float radius = reach + state.Velocity.magnitude * interval * 2f;
            _fieldRockQuery.FindInSphere(state.Position, radius, cache.Rocks);
            cache.Refill(state.Position, radius);
            return cache.Rocks;
        }

        private float CalculateQueryInterval(int agentId) {
            float spread = agentId % _fieldAgentConfiguration.RockQueryIntervalSpreadSteps * _fieldAgentConfiguration.RockQueryIntervalStepShare;
            return _fieldAgentConfiguration.RockQuerySeconds * (_fieldAgentConfiguration.RockQueryIntervalMinShare + spread);
        }

        private static Vector3 CalculateTarget(FieldAgentGoal goal, in FieldAgentShipFrame ship, FieldAgentProfile profile) {
            if (goal.IsShipSpace == false)
                return goal.Point;

            Vector3 shipPoint = goal.IsLandingOnArrival ? goal.Point + Vector3.up * profile.LandingApproachHeight : goal.Point;
            return ship.ToWorldPoint(shipPoint);
        }

        private static bool IsAtLandingApproach(FieldAgentGoal goal, in FieldAgentShipFrame ship, Vector3 position, Vector3 target, FieldAgentProfile profile) =>
            goal.IsLandingOnArrival && ship.HasShip && Vector3.Distance(position, target) <= profile.LandingStartMeters;

        private Vector3 CalculateDeckPoint(Vector3 shipPoint) =>
            _fieldAgentDeckNavigation.TrySamplePoint(shipPoint, out Vector3 deckPoint) ? deckPoint : shipPoint;
    }
}
