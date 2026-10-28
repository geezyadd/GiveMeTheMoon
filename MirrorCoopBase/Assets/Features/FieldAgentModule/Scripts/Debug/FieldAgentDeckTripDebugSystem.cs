#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Deck;
using Features.FieldAgentModule.Scripts.Server;
using Features.FieldAgentModule.Scripts.Spawn;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using UnityEngine;
using Zenject;

namespace Features.FieldAgentModule.Scripts.Debug {
    // Debug only, on the host: "Land an agent on the deck" spawns one test agent at a hidden point; it flies in and
    // lands at a deck point by the stern, walks the deck NavMesh to the helm, stands there, walks back, takes off and
    // orbits the ship. Every stage works while the ship flies and turns: the deck part runs in ship space.
    public sealed class FieldAgentDeckTripDebugSystem : IInitializable, IDisposable, ITickable, IFieldAgentDeckTripDebug {
        private const string LABEL = "Land an agent on the deck";
        private const string DESCRIPTION = "One test agent flies in, lands by the stern, walks to the helm module and back, then takes off and orbits.";
        private const int SINGLE_AGENT = 1;
        private const int SPAWN_SEED = 7919;
        private const float FULL_SPEED = 1f;

        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly IFieldAgentControl _fieldAgentControl;
        private readonly IFieldAgentGroupSpawnService _fieldAgentGroupSpawnService;
        private readonly IFieldAgentDeckNavigation _fieldAgentDeckNavigation;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly FieldAgentDeckTripDebugModel _fieldAgentDeckTripDebugModel;
        private readonly List<int> _spawned = new();
        private IDisposable _registration;

        public FieldAgentDeckTripDebugSystem(
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions,
            IFieldAgentControl fieldAgentControl,
            IFieldAgentGroupSpawnService fieldAgentGroupSpawnService,
            IFieldAgentDeckNavigation fieldAgentDeckNavigation,
            IShipRunBindingModel shipRunBindingModel,
            FieldAgentConfiguration fieldAgentConfiguration,
            FieldAgentDeckTripDebugModel fieldAgentDeckTripDebugModel) {
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
            _fieldAgentControl = fieldAgentControl;
            _fieldAgentGroupSpawnService = fieldAgentGroupSpawnService;
            _fieldAgentDeckNavigation = fieldAgentDeckNavigation;
            _shipRunBindingModel = shipRunBindingModel;
            _fieldAgentConfiguration = fieldAgentConfiguration;
            _fieldAgentDeckTripDebugModel = fieldAgentDeckTripDebugModel;
        }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.AGENTS, LABEL, DESCRIPTION, _shipDebugActionConditions.ReadHostWithShipReason, StartFromWindow));

        public void Dispose() =>
            _registration.Dispose();

        public void Tick() {
            if (_fieldAgentDeckTripDebugModel.IsIdle)
                return;

            if (_fieldAgentControl.TryGetAgent(_fieldAgentDeckTripDebugModel.AgentId, out FieldAgentState state) == false) {
                _fieldAgentDeckTripDebugModel.SetStage(FieldAgentDeckTripStage.Lost);
                return;
            }

            Advance(state);
        }

        public bool DebugStartDeckTrip() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || TryFindDeckPoints(ship, out Vector3 landingPoint, out Vector3 modulePoint) == false)
                return false;

            AttachBehaviour landing = new(landingPoint, _fieldAgentConfiguration.DebugDeckArriveMeters);
            _fieldAgentGroupSpawnService.SpawnHiddenGroup(_fieldAgentConfiguration.Profiles[0], SINGLE_AGENT, ship.transform.position, SPAWN_SEED, () => landing, _spawned);
            if (_spawned.Count == 0)
                return false;

            _fieldAgentDeckTripDebugModel.Begin(_spawned[0], landingPoint, modulePoint);
            return true;
        }

        private void Advance(FieldAgentState state) {
            switch (_fieldAgentDeckTripDebugModel.Stage) {
                case FieldAgentDeckTripStage.FlyingIn:
                    WalkWhenFinished(state, _fieldAgentDeckTripDebugModel.ModulePoint, FieldAgentDeckTripStage.WalkingToModule);
                    break;
                case FieldAgentDeckTripStage.WalkingToModule:
                    StandWhenFinished(state);
                    break;
                case FieldAgentDeckTripStage.AtModule:
                    StandAtModule();
                    break;
                case FieldAgentDeckTripStage.WalkingBack:
                    TakeOffWhenFinished(state);
                    break;
                case FieldAgentDeckTripStage.TakingOff:
                    OrbitWhenFlying(state);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_fieldAgentDeckTripDebugModel.Stage), _fieldAgentDeckTripDebugModel.Stage, "The deck trip has no step for this stage.");
            }
        }

        private void WalkWhenFinished(FieldAgentState state, Vector3 deckPoint, FieldAgentDeckTripStage next) {
            if (state.IsBehaviourFinished == false)
                return;

            _fieldAgentControl.SetBehaviour(state.Id, new SeekBehaviour(deckPoint, true, _fieldAgentConfiguration.DebugDeckArriveMeters, FULL_SPEED));
            _fieldAgentDeckTripDebugModel.SetStage(next);
        }

        private void StandWhenFinished(FieldAgentState state) {
            if (state.IsBehaviourFinished == false)
                return;

            _fieldAgentControl.SetBehaviour(state.Id, new AttachBehaviour(_fieldAgentDeckTripDebugModel.ModulePoint, _fieldAgentConfiguration.DebugDeckArriveMeters));
            _fieldAgentDeckTripDebugModel.SetStage(FieldAgentDeckTripStage.AtModule);
        }

        private void StandAtModule() {
            _fieldAgentDeckTripDebugModel.AddAtModuleSeconds(Time.deltaTime);
            if (_fieldAgentDeckTripDebugModel.AtModuleSeconds < _fieldAgentConfiguration.DebugDeckPauseSeconds)
                return;

            _fieldAgentControl.SetBehaviour(_fieldAgentDeckTripDebugModel.AgentId, new SeekBehaviour(_fieldAgentDeckTripDebugModel.LandingPoint, true, _fieldAgentConfiguration.DebugDeckArriveMeters, FULL_SPEED));
            _fieldAgentDeckTripDebugModel.SetStage(FieldAgentDeckTripStage.WalkingBack);
        }

        private void TakeOffWhenFinished(FieldAgentState state) {
            if (state.IsBehaviourFinished == false || _fieldAgentControl.TakeOff(state.Id) == false)
                return;

            _fieldAgentControl.SetBehaviour(state.Id, new OrbitBehaviour(_fieldAgentConfiguration.DebugOrbitRadius, _fieldAgentConfiguration.DebugOrbitHeight, _fieldAgentConfiguration.DebugOrbitSpeedShare, _fieldAgentConfiguration.DebugOrbitLeadDegrees));
            _fieldAgentDeckTripDebugModel.SetStage(FieldAgentDeckTripStage.TakingOff);
        }

        private void OrbitWhenFlying(FieldAgentState state) {
            if (state.Mode == FieldAgentMode.Flight)
                _fieldAgentDeckTripDebugModel.SetStage(FieldAgentDeckTripStage.Orbiting);
        }

        // The landing point by the stern, and a stand-off point in front of the foremost socket (the helm).
        private bool TryFindDeckPoints(ShipBase ship, out Vector3 landingPoint, out Vector3 modulePoint) {
            modulePoint = Vector3.zero;
            if (_fieldAgentDeckNavigation.TrySamplePoint(_fieldAgentConfiguration.DebugDeckLandingPoint, out landingPoint) == false)
                return false;

            Vector3 socket = FindForemostSocket(ship);
            Vector3 towardLanding = landingPoint - socket;
            towardLanding.y = 0f;
            Vector3 standOff = socket + towardLanding.normalized * _fieldAgentConfiguration.DebugModuleStandOffMeters;
            standOff.y = landingPoint.y;
            return _fieldAgentDeckNavigation.TrySamplePoint(standOff, out modulePoint);
        }

        private static Vector3 FindForemostSocket(ShipBase ship) {
            Vector3 foremost = Vector3.zero;
            float foremostZ = float.MinValue;
            foreach (ShipSocket socket in ship.Sockets) {
                Vector3 local = ship.transform.InverseTransformPoint(socket.transform.position);
                if (local.z <= foremostZ)
                    continue;

                foremostZ = local.z;
                foremost = local;
            }

            return foremost;
        }

        private void StartFromWindow() =>
            DebugStartDeckTrip();
    }
}
#endif
