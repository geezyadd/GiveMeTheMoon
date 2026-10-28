#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Features.DebugModule.Contracts;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Server;
using Features.FieldAgentModule.Scripts.Spawn;
using Features.ShipModule.Scripts;
using UnityEngine;
using Zenject;

namespace Features.FieldAgentModule.Scripts.Debug {
    // Debug only, on the host: "Spawn test agents (N)" makes a flock of the first agent kind at a hidden point that
    // seeks the ship through the field; each agent that reaches it starts to orbit it. "Remove all field agents"
    // clears every agent.
    public sealed class FieldAgentFlockDebugSystem : IInitializable, IDisposable, ITickable, IFieldAgentFlockDebug {
        private const string SPAWN_LABEL = "Spawn test agents (N)";
        private const string SPAWN_DESCRIPTION = "Spawns N test agents at a point hidden from every player; they fly to the ship through the field and orbit it.";
        private const string SPAWN_VALUE_LABEL = "N";
        private const string REMOVE_LABEL = "Remove all field agents";
        private const string REMOVE_DESCRIPTION = "Removes every field agent on the server (all clients lose them with the next batch).";
        private const float FULL_SPEED = 1f;
        private const string SPAWNED_LOG_FORMAT = "[FieldAgents] spawned {0} test agents, hidden by {1}";

        private readonly IDebugActionRegistry _debugActionRegistry;
        private readonly IFieldAgentDebugConditions _fieldAgentDebugConditions;
        private readonly IFieldAgentControl _fieldAgentControl;
        private readonly IFieldAgentGroupSpawnService _fieldAgentGroupSpawnService;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly List<IDisposable> _registrations = new();
        private readonly List<int> _flockIds = new();
        private readonly List<int> _seekingIds = new();
        private readonly List<int> _spawned = new();
        private int _spawnSeed;

        public FieldAgentFlockDebugSystem(
            IDebugActionRegistry debugActionRegistry,
            IFieldAgentDebugConditions fieldAgentDebugConditions,
            IFieldAgentControl fieldAgentControl,
            IFieldAgentGroupSpawnService fieldAgentGroupSpawnService,
            IShipRunBindingModel shipRunBindingModel,
            FieldAgentConfiguration fieldAgentConfiguration) {
            _debugActionRegistry = debugActionRegistry;
            _fieldAgentDebugConditions = fieldAgentDebugConditions;
            _fieldAgentControl = fieldAgentControl;
            _fieldAgentGroupSpawnService = fieldAgentGroupSpawnService;
            _shipRunBindingModel = shipRunBindingModel;
            _fieldAgentConfiguration = fieldAgentConfiguration;
        }

        public IReadOnlyList<int> FlockIds => _flockIds;
        public int OrbitingCount => _flockIds.Count - _seekingIds.Count;
        public FieldAgentSpawnCover LastSpawnCover { get; private set; }

        public void Initialize() {
            DebugActionValue count = new(SPAWN_VALUE_LABEL, _fieldAgentConfiguration.DebugFlockSize);
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.AGENTS, SPAWN_LABEL, SPAWN_DESCRIPTION, _fieldAgentDebugConditions.ReadHostWithShipReason, count, SpawnFromWindow)));
            _registrations.Add(_debugActionRegistry.Register(new DebugActionDefinition(DebugSections.AGENTS, REMOVE_LABEL, REMOVE_DESCRIPTION, _fieldAgentDebugConditions.ReadHostWithShipReason, DebugRemoveAll)));
        }

        public void Dispose() {
            foreach (IDisposable registration in _registrations)
                registration.Dispose();

            _registrations.Clear();
        }

        public void Tick() {
            for (int i = _flockIds.Count - 1; i >= 0; i--) {
                if (_fieldAgentControl.TryGetAgent(_flockIds[i], out FieldAgentState _) == false) {
                    _seekingIds.Remove(_flockIds[i]);
                    _flockIds.RemoveAt(i);
                }
            }

            for (int i = _seekingIds.Count - 1; i >= 0; i--)
                StartOrbitWhenArrived(i);
        }

        public int DebugSpawnTestFlock(int count) {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null || count <= 0)
                return 0;

            LastSpawnCover = _fieldAgentGroupSpawnService.SpawnHiddenGroup(_fieldAgentConfiguration.Profiles[0], count, ship.transform.position, ++_spawnSeed, CreateSeek, _spawned);
            _flockIds.AddRange(_spawned);
            _seekingIds.AddRange(_spawned);
            return _spawned.Count;
        }

        public void DebugRemoveAll() {
            _fieldAgentControl.DespawnAll();
            _flockIds.Clear();
            _seekingIds.Clear();
        }

        private void StartOrbitWhenArrived(int seekingIndex) {
            int id = _seekingIds[seekingIndex];
            if (_fieldAgentControl.TryGetAgent(id, out FieldAgentState state) == false || state.IsBehaviourFinished == false)
                return;

            float lead = (id & 1) == 0 ? _fieldAgentConfiguration.DebugOrbitLeadDegrees : -_fieldAgentConfiguration.DebugOrbitLeadDegrees;
            _fieldAgentControl.SetBehaviour(id, new OrbitBehaviour(_fieldAgentConfiguration.DebugOrbitRadius, _fieldAgentConfiguration.DebugOrbitHeight, _fieldAgentConfiguration.DebugOrbitSpeedShare, lead));
            _seekingIds.RemoveAt(seekingIndex);
        }

        private IFieldAgentBehaviour CreateSeek() =>
            new SeekBehaviour(Vector3.up * _fieldAgentConfiguration.DebugOrbitHeight, true, _fieldAgentConfiguration.DebugSeekArriveMeters, FULL_SPEED);

        private void SpawnFromWindow(float count) =>
            UnityEngine.Debug.Log(string.Format(SPAWNED_LOG_FORMAT, DebugSpawnTestFlock(Mathf.RoundToInt(count)), LastSpawnCover));
    }
}
#endif
