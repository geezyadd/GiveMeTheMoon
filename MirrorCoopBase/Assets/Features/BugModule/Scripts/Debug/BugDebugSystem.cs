#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Configuration;
using Features.BugModule.Scripts.Spawn;
using Features.DebugModule.Contracts;
using Features.FieldAgentModule.Scripts.Cargo;
using Features.FieldAgentModule.Scripts.Spawn;
using Features.ShipModule.Scripts.Debug;
using UnityEngine;
using Zenject;

namespace Features.BugModule.Scripts.Debug {
    // Debug only, on the host: "Spawn bugs (N)" sends a group of N bugs at the ship from a point no player sees,
    // "A bug steals the parcel" sends one bug straight for the free cargo on board (the parcel) and "Random bugs: on /
    // off" switches the rare random groups in flight. A button label cannot change, so the toggle registers again with
    // the label of the new state.
    public sealed class BugDebugSystem : IInitializable, IDisposable {
        private const string SPAWN_LABEL = "Spawn bugs (N)";
        private const string SPAWN_DESCRIPTION = "N bugs come out from behind a rock no player sees (else past the haze) and fly at the ship: they grab a cargo on board or land and gnaw the ship until shot.";
        private const string SPAWN_VALUE_LABEL = "N";
        private const string RANDOM_ON_LABEL = "Random bugs: on";
        private const string RANDOM_OFF_LABEL = "Random bugs: off";
        private const string RANDOM_DESCRIPTION = "Switches the rare random bug groups in flight.";
        private const string STEAL_LABEL = "A bug steals the parcel";
        private const string STEAL_DESCRIPTION = "One bug comes out from behind a rock no player sees and flies straight for the parcel on board to steal it.";
        private const string NO_CARGO_REASON = "No parcel lies on board.";
        private const string SPAWNED_LOG_FORMAT = "[Bugs] spawned {0} bugs, hidden by {1}";
        private const int THIEF_COUNT = 1;

        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IShipDebugActionConditions _shipDebugActionConditions;
        private readonly IBugSpawnService _bugSpawnService;
        private readonly IBugBrainService _bugBrainService;
        private readonly IFieldAgentCargoTargets _fieldAgentCargoTargets;
        private readonly BugServerModel _bugServerModel;
        private readonly BugRandomSpawnModel _bugRandomSpawnModel;
        private readonly BugConfiguration _bugConfiguration;
        private readonly List<int> _spawned = new();

        private IDisposable _spawnRegistration;
        private IDisposable _stealRegistration;
        private IDisposable _randomRegistration;

        public BugDebugSystem(
            IDebugActionRegistry debugActionRegistry,
            IShipDebugActionConditions shipDebugActionConditions,
            IBugSpawnService bugSpawnService,
            IBugBrainService bugBrainService,
            IFieldAgentCargoTargets fieldAgentCargoTargets,
            BugServerModel bugServerModel,
            BugRandomSpawnModel bugRandomSpawnModel,
            BugConfiguration bugConfiguration) {
            _debugActionRegistry = debugActionRegistry;
            _shipDebugActionConditions = shipDebugActionConditions;
            _bugSpawnService = bugSpawnService;
            _bugBrainService = bugBrainService;
            _fieldAgentCargoTargets = fieldAgentCargoTargets;
            _bugServerModel = bugServerModel;
            _bugRandomSpawnModel = bugRandomSpawnModel;
            _bugConfiguration = bugConfiguration;
        }

        public void Initialize() {
            DebugActionValue count = new(SPAWN_VALUE_LABEL, _bugConfiguration.DebugGroupSize);
            _spawnRegistration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.AGENTS, SPAWN_LABEL, SPAWN_DESCRIPTION, _shipDebugActionConditions.ReadHostWithShipReason, count, SpawnFromWindow));
            _stealRegistration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.AGENTS, STEAL_LABEL, STEAL_DESCRIPTION, ReadStealReason, SendThief));
            _bugRandomSpawnModel.OnEnabledChanged += HandleRandomSpawnChanged;
            RegisterRandomToggle();
        }

        public void Dispose() {
            _bugRandomSpawnModel.OnEnabledChanged -= HandleRandomSpawnChanged;
            _spawnRegistration.Dispose();
            _stealRegistration.Dispose();
            _randomRegistration.Dispose();
        }

        private void RegisterRandomToggle() {
            string label = _bugRandomSpawnModel.IsEnabled ? RANDOM_ON_LABEL : RANDOM_OFF_LABEL;
            _randomRegistration = _debugActionRegistry.Register(new DebugActionDefinition(DebugSections.AGENTS, label, RANDOM_DESCRIPTION, _shipDebugActionConditions.ReadHostReason, ToggleRandomSpawn));
        }

        private void SpawnFromWindow(float count) {
            FieldAgentSpawnCover cover = _bugSpawnService.ServerSpawnGroup(Mathf.RoundToInt(count), _spawned);
            UnityEngine.Debug.Log(string.Format(SPAWNED_LOG_FORMAT, _spawned.Count, cover));
        }

        private string ReadStealReason() {
            string reason = _shipDebugActionConditions.ReadHostWithShipReason();
            if (reason != null)
                return reason;

            return TryFindFreeCargo(out _) ? null : NO_CARGO_REASON;
        }

        private void SendThief() {
            if (TryFindFreeCargo(out IFieldAgentCargoTarget cargo) == false)
                return;

            _bugSpawnService.ServerSpawnGroup(THIEF_COUNT, _spawned);
            foreach (int id in _spawned) {
                if (_bugServerModel.TryGet(id, out BugAgent bug))
                    _bugBrainService.ServerGoForCargo(bug, cargo);
            }
        }

        private bool TryFindFreeCargo(out IFieldAgentCargoTarget cargo) {
            cargo = null;
            foreach (IFieldAgentCargoTarget candidate in _fieldAgentCargoTargets.Items) {
                if (candidate.IsTaken)
                    continue;

                cargo = candidate;
                return true;
            }

            return false;
        }

        private void ToggleRandomSpawn() =>
            _bugRandomSpawnModel.SetEnabled(_bugRandomSpawnModel.IsEnabled == false);

        private void HandleRandomSpawnChanged() {
            _randomRegistration.Dispose();
            RegisterRandomToggle();
        }
    }
}
#endif
