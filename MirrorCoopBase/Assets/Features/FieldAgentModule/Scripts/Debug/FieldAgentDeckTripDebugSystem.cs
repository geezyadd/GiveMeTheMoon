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
        private readonly IFieldAgentDebugConditions _fieldAgentDebugConditions;
        private readonly IFieldAgentControl _fieldAgentControl;
        private readonly IFieldAgentGroupSpawnService _fieldAgentGroupSpawnService;
        private readonly IFieldAgentDeckNavigation _fieldAgentDeckNavigation;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly List<int> _spawned = new();
        private IDisposable _registration;
        private float _atModuleSeconds;

        public FieldAgentDeckTripDebugSystem(
            IDebugActionRegistry debugActionRegistry,
            IFieldAgentDebugConditions fieldAgentDebugConditions,
            IFieldAgentControl fieldAgentControl,
            IFieldAgentGroupSpawnService fieldAgentGroupSpawnService,
            IFieldAgentDeckNavigation fieldAgentDeckNavigation,
            IShipRunBindingModel shipRunBindingModel,
            FieldAgentConfiguration fieldAgentConfiguration) {
            _debugActionRegistry = debugActionRegistry;
            _fieldAgentDebugConditions = fieldAgentDebugConditions;
            _fieldAgentControl = fieldAgentControl;
            _fieldAgentGroupSpawnService = fieldAgentGroupSpawnService;
            _fieldAgentDeckNavigation = fieldAgentDeckNavigation;
            _shipRunBindingModel = shipRunBindingModel;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public FieldAgentDeckTripStage Stage { get; private set; }
        public int AgentId { get; private set; }
        public Vector3 LandingPoint { get; private set; }
        public Vector3 ModulePoint { get; private set; }

        public void Initialize() =>
            _registration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.AGENTS, LABEL, DESCRIPTION, _fieldAgentDebugConditions.ReadHostWithShipReason, StartFromWindow));

        public void Dispose() =>
            _registration.Dispose();

        public void Tick() {
            if (Stage == FieldAgentDeckTripStage.None || Stage == FieldAgentDeckTripStage.Lost || Stage == FieldAgentDeckTripStage.Orbiting)
                return;

            if (_fieldAgentControl.TryGetAgent(AgentId, out FieldAgentState state) == false) {
                Stage = FieldAgentDeckTripStage.Lost;
                return;
            }

            Advance(state);
        }

        public bool DebugStartDeckTrip() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || TryFindDeckPoints(ship, out Vector3 landingPoint, out Vector3 modulePoint) == false)
                return false;

            LandingPoint = landingPoint;
            ModulePoint = modulePoint;
            _fieldAgentGroupSpawnService.SpawnHiddenGroup(_fieldAgentConfiguration.Profiles[0], SINGLE_AGENT, ship.transform.position, SPAWN_SEED, CreateAttach, _spawned);
            if (_spawned.Count == 0)
                return false;

            AgentId = _spawned[0];
            Stage = FieldAgentDeckTripStage.FlyingIn;
            return true;
        }

        private void Advance(FieldAgentState state) {
            switch (Stage) {
                case FieldAgentDeckTripStage.FlyingIn:
                    GoWhenFinished(state, ModulePoint, FieldAgentDeckTripStage.WalkingToModule);
                    break;
                case FieldAgentDeckTripStage.WalkingToModule:
                    if (state.IsBehaviourFinished)
                        BeginStandingAtModule();
                    break;
                case FieldAgentDeckTripStage.AtModule:
                    StandAtModule();
                    break;
                case FieldAgentDeckTripStage.WalkingBack:
                    TakeOffWhenFinished(state);
                    break;
                case FieldAgentDeckTripStage.TakingOff:
                    if (state.Mode == FieldAgentMode.Flight)
                        Stage = FieldAgentDeckTripStage.Orbiting;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Stage), Stage, "The deck trip has no step for this stage.");
            }
        }

        private void GoWhenFinished(FieldAgentState state, Vector3 deckPoint, FieldAgentDeckTripStage next) {
            if (state.IsBehaviourFinished == false)
                return;

            _fieldAgentControl.SetBehaviour(AgentId, new SeekBehaviour(deckPoint, true, _fieldAgentConfiguration.DebugDeckArriveMeters, FULL_SPEED));
            Stage = next;
        }

        private void BeginStandingAtModule() {
            _fieldAgentControl.SetBehaviour(AgentId, new AttachBehaviour(ModulePoint, _fieldAgentConfiguration.DebugDeckArriveMeters));
            _atModuleSeconds = 0f;
            Stage = FieldAgentDeckTripStage.AtModule;
        }

        private void StandAtModule() {
            _atModuleSeconds += Time.deltaTime;
            if (_atModuleSeconds < _fieldAgentConfiguration.DebugDeckPauseSeconds)
                return;

            _fieldAgentControl.SetBehaviour(AgentId, new SeekBehaviour(LandingPoint, true, _fieldAgentConfiguration.DebugDeckArriveMeters, FULL_SPEED));
            Stage = FieldAgentDeckTripStage.WalkingBack;
        }

        private void TakeOffWhenFinished(FieldAgentState state) {
            if (state.IsBehaviourFinished == false || _fieldAgentControl.TakeOff(AgentId) == false)
                return;

            _fieldAgentControl.SetBehaviour(AgentId, new OrbitBehaviour(_fieldAgentConfiguration.DebugOrbitRadius, _fieldAgentConfiguration.DebugOrbitHeight, _fieldAgentConfiguration.DebugOrbitSpeedShare, _fieldAgentConfiguration.DebugOrbitLeadDegrees));
            Stage = FieldAgentDeckTripStage.TakingOff;
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

        private IFieldAgentBehaviour CreateAttach() =>
            new AttachBehaviour(LandingPoint, _fieldAgentConfiguration.DebugDeckArriveMeters);

        private void StartFromWindow() =>
            DebugStartDeckTrip();
    }
}
#endif
