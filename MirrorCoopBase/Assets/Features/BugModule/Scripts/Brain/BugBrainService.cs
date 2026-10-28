using System;
using System.Collections.Generic;
using Features.BugModule.Scripts.Behaviours;
using Features.BugModule.Scripts.Configuration;
using Features.BugModule.Scripts.Gnaw;
using Features.BugModule.Scripts.Health;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Cargo;
using Features.FieldAgentModule.Scripts.Server;
using Features.ShipDamageModule.Scripts.Damage;
using Features.ShipModule.Scripts;
using UnityEngine;

namespace Features.BugModule.Scripts.Brain {
    // A bug's simple mind: it flies in to its own side of the ship, then goes for a cargo on board (grabs it and drags
    // it away) or lands at the nearest damageable place and gnaws it, a bite every few seconds. A cargo someone else
    // grabbed first or a knock off the deck makes it choose again; once the ship no longer flies it leaves. A bug
    // that is far enough away is gone.
    public sealed class BugBrainService : IBugBrainService {
        private const float MIN_AWAY_SQR = 0.01f;

        private readonly IFieldAgentControl _fieldAgentControl;
        private readonly IFieldAgentCargoTargets _fieldAgentCargoTargets;
        private readonly IShipDamageCandidateSource _shipDamageCandidateSource;
        private readonly IBugTargetRule _bugTargetRule;
        private readonly IBugBehaviourFactory _bugBehaviourFactory;
        private readonly IBugGnawService _bugGnawService;
        private readonly IBugRemovalService _bugRemovalService;
        private readonly BugConfiguration _bugConfiguration;

        public BugBrainService(
            IFieldAgentControl fieldAgentControl,
            IFieldAgentCargoTargets fieldAgentCargoTargets,
            IShipDamageCandidateSource shipDamageCandidateSource,
            IBugTargetRule bugTargetRule,
            IBugBehaviourFactory bugBehaviourFactory,
            IBugGnawService bugGnawService,
            IBugRemovalService bugRemovalService,
            BugConfiguration bugConfiguration) {
            _fieldAgentControl = fieldAgentControl;
            _fieldAgentCargoTargets = fieldAgentCargoTargets;
            _shipDamageCandidateSource = shipDamageCandidateSource;
            _bugTargetRule = bugTargetRule;
            _bugBehaviourFactory = bugBehaviourFactory;
            _bugGnawService = bugGnawService;
            _bugRemovalService = bugRemovalService;
            _bugConfiguration = bugConfiguration;
        }

        public void ServerStep(BugAgent bug, in FieldAgentState state, ShipBase ship, float deltaTime) {
            if (ship.IsFlying == false && IsStillAtTheShip(bug)) {
                Leave(bug, state);
                return;
            }

            switch (bug.Stage) {
                case BugStage.Approach:
                    if (state.IsBehaviourFinished)
                        ChooseTarget(bug, state, ship);
                    break;
                case BugStage.SeekCargo:
                    StepSeekCargo(bug, state, ship);
                    break;
                case BugStage.Carry:
                case BugStage.Leave:
                    if (state.IsBehaviourFinished)
                        _bugRemovalService.ServerRemoveGone(bug.Id);
                    break;
                case BugStage.Land:
                    if (state.Mode == FieldAgentMode.Deck)
                        bug.StartGnawing(_bugConfiguration.BiteSeconds);
                    break;
                case BugStage.Gnaw:
                    StepGnaw(bug, state, ship, deltaTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bug.Stage), bug.Stage, $"Bug {bug.Id} has no stage.");
            }
        }

        private void StepSeekCargo(BugAgent bug, in FieldAgentState state, ShipBase ship) {
            if (bug.Cargo.IsTaken) {
                ChooseTarget(bug, state, ship);
                return;
            }

            if (state.IsBehaviourFinished == false)
                return;

            bug.Cargo.ServerTake();
            bug.CarryCargo();
            _fieldAgentControl.SetBehaviour(bug.Id, _bugBehaviourFactory.CreateCarry(bug.Cargo, CalculateCarryDestination(state, ship)));
        }

        private void StepGnaw(BugAgent bug, in FieldAgentState state, ShipBase ship, float deltaTime) {
            if (state.Mode != FieldAgentMode.Deck) {
                ChooseTarget(bug, state, ship);
                return;
            }

            bug.CountDownBite(deltaTime);
            if (bug.SecondsToBite > 0f)
                return;

            _bugGnawService.ServerBite(bug.Id, ship.transform.TransformPoint(state.Position));
            bug.WaitForNextBite(_bugConfiguration.BiteSeconds);
        }

        private void ChooseTarget(BugAgent bug, in FieldAgentState state, ShipBase ship) {
            Vector3 shipLocal = state.IsInShipSpace ? state.Position : ship.transform.InverseTransformPoint(state.Position);
            BugTarget target = _bugTargetRule.Choose(_fieldAgentCargoTargets.Items, CollectShipLocalSpots(ship), shipLocal);
            if (target.Kind == BugTargetKind.Cargo) {
                bug.GoForCargo(target.Cargo);
                _fieldAgentControl.SetBehaviour(bug.Id, _bugBehaviourFactory.CreateSeekCargo(target.Cargo));
                return;
            }

            bug.GoLand(target.GnawPoint);
            _fieldAgentControl.SetBehaviour(bug.Id, _bugBehaviourFactory.CreateLand(target.GnawPoint));
        }

        private void Leave(BugAgent bug, in FieldAgentState state) {
            if (state.Mode == FieldAgentMode.Deck)
                _fieldAgentControl.TakeOff(bug.Id);

            bug.Leave();
            _fieldAgentControl.SetBehaviour(bug.Id, _bugBehaviourFactory.CreateLeave());
        }

        // A bug carrying a cargo off is on its way out already.
        private static bool IsStillAtTheShip(BugAgent bug) =>
            bug.Stage != BugStage.Leave && bug.Stage != BugStage.Carry;

        // Rare (once per bug that reaches the ship), so the lists are made per call.
        private List<ShipDamageCandidate> CollectShipLocalSpots(ShipBase ship) {
            List<ShipDamageCandidate> modules = new();
            List<ShipDamageCandidate> points = new();
            _shipDamageCandidateSource.CollectModules(ship, modules);
            _shipDamageCandidateSource.CollectDamagePoints(points);
            modules.AddRange(points);
            Transform shipTransform = ship.transform;
            for (int i = 0; i < modules.Count; i++) {
                ShipDamageCandidate spot = modules[i];
                modules[i] = new ShipDamageCandidate(spot.Id, shipTransform.InverseTransformPoint(spot.Position), spot.IsDamageable, spot.ChanceMultiplier);
            }

            return modules;
        }

        // Straight away from the ship, level with it.
        private Vector3 CalculateCarryDestination(in FieldAgentState state, ShipBase ship) {
            Transform shipTransform = ship.transform;
            Vector3 position = state.IsInShipSpace ? shipTransform.TransformPoint(state.Position) : state.Position;
            Vector3 away = position - shipTransform.position;
            away.y = 0f;
            away = away.sqrMagnitude > MIN_AWAY_SQR ? away.normalized : -shipTransform.forward;
            return shipTransform.position + away * _bugConfiguration.CarryAwayMeters;
        }
    }
}
