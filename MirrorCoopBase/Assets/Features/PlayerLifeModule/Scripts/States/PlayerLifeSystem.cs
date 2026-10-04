using System;
using System.Collections.Generic;
using Features.GameCoreModule.Contracts;
using Features.NetworkModelModule.Scripts;
using Features.PlayerLifeModule.Scripts.Generated;
using Game.Connection;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.PlayerLifeModule.Scripts {
    // Server: one life state machine per connected player, the all-dead return to the lobby and the revive on a new map.
    public sealed class PlayerLifeSystem : IInitializable, IDisposable, ITickable, IGameplaySession, IPlayerLifeReviver {
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly PlayerLifeRegistry _playerLifeRegistry;
        private readonly IAllDeadRule _allDeadRule;
        private readonly IPlayerLifeStateMachineFactory _playerLifeStateMachineFactory;
        private readonly IConnectionSessionService _connectionSessionService;
        private readonly PlayerLifeConfiguration _playerLifeConfiguration;
        private readonly Dictionary<PlayerLifeBody, IPlayerLifeStateMachine> _machinesByBody = new();
        private readonly List<IPlayerLifeStateMachine> _machines = new();
        private readonly List<PlayerKey> _knownKeys = new();
        private bool _returnPending;
        private float _returnAt;

        public PlayerLifeSystem(
            IPlayerBodyRegistry playerBodyRegistry,
            PlayerLifeRegistry playerLifeRegistry,
            IAllDeadRule allDeadRule,
            IPlayerLifeStateMachineFactory playerLifeStateMachineFactory,
            IConnectionSessionService connectionSessionService,
            PlayerLifeConfiguration playerLifeConfiguration) {
            _playerBodyRegistry = playerBodyRegistry;
            _playerLifeRegistry = playerLifeRegistry;
            _allDeadRule = allDeadRule;
            _playerLifeStateMachineFactory = playerLifeStateMachineFactory;
            _connectionSessionService = connectionSessionService;
            _playerLifeConfiguration = playerLifeConfiguration;
        }

        public void Initialize() {
            _playerBodyRegistry.OnServerBodyAdded += OnServerBodyAdded;
            _playerBodyRegistry.OnServerBodyRemoved += OnServerBodyRemoved;
            _playerLifeRegistry.OnPlayerAdded += OnLifeRecordAdded;
            _connectionSessionService.OnMapReady += OnMapReady;
        }

        public void Dispose() {
            _playerBodyRegistry.OnServerBodyAdded -= OnServerBodyAdded;
            _playerBodyRegistry.OnServerBodyRemoved -= OnServerBodyRemoved;
            _playerLifeRegistry.OnPlayerAdded -= OnLifeRecordAdded;
            _connectionSessionService.OnMapReady -= OnMapReady;
        }

        public void Tick() {
            if (_returnPending == false || NetworkServer.active == false)
                return;

            if (Time.time < _returnAt)
                return;

            _returnPending = false;
            _connectionSessionService.ReturnToLobby();
        }

        public void ServerReviveAll() {
            if (NetworkServer.active == false)
                throw new InvalidOperationException(nameof(ServerReviveAll) + " can only be called on the server.");

            for (int i = 0; i < _machines.Count; i++)
                _machines[i].Revive();

            EvaluateAllDead();
        }

        public void CleanupGameplay() {
            _returnPending = false;
            // Leaving the session: the next host starts with fresh records (alive, full health).
            for (int i = 0; i < _knownKeys.Count; i++)
                _playerLifeRegistry.Remove(_knownKeys[i]);

            _knownKeys.Clear();
        }

        public void RestartGameplay() {
            _returnPending = false;
            if (NetworkServer.active)
                ServerReviveAll();
        }

        private void OnServerBodyAdded(PlayerLifeBody body) {
            IPlayerLifeStateMachine machine = _playerLifeStateMachineFactory.Create(body, body.Damageable);
            _machinesByBody.Add(body, machine);
            _machines.Add(machine);
            machine.OnStateChanged += OnLifeStateChanged;
            machine.Start();
            // The lobby is between runs: a player whose record stayed dead while offline (died, left, came back) joins alive.
            if (_connectionSessionService.IsInLobby)
                machine.Revive();

            EvaluateAllDead();
        }

        private void OnServerBodyRemoved(PlayerLifeBody body) {
            if (_machinesByBody.Remove(body, out IPlayerLifeStateMachine machine) == false)
                return;

            machine.OnStateChanged -= OnLifeStateChanged;
            machine.Stop();
            _machines.Remove(machine);
            EvaluateAllDead();
        }

        private void OnLifeRecordAdded(PlayerKey key, IReadOnlyPlayerLifeModel model) =>
            _knownKeys.Add(key);

        private void OnLifeStateChanged() =>
            EvaluateAllDead();

        // Every map change (back to the lobby, or a new run) starts with everyone alive.
        private void OnMapReady() {
            if (NetworkServer.active)
                ServerReviveAll();
        }

        private void EvaluateAllDead() {
            if (_allDeadRule.AreAllDead(_machines) == false) {
                _returnPending = false;
                return;
            }

            if (_returnPending)
                return;

            _returnPending = true;
            _returnAt = Time.time + _playerLifeConfiguration.AllDeadReturnDelay;
        }
    }
}
