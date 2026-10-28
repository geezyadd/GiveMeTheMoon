using System.Collections.Generic;
using Features.BugModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Cargo;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.BugModule.Scripts.Spawn {
    // Server: while the ship flies, one roll every interval whether a bug group comes (rare; more likely with a cargo
    // on board). Off the flight the interval starts over.
    public sealed class BugRandomSpawnSystem : ITickable {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IFieldAgentCargoTargets _fieldAgentCargoTargets;
        private readonly IBugRandomSpawnRule _bugRandomSpawnRule;
        private readonly IBugSpawnService _bugSpawnService;
        private readonly BugRandomSpawnModel _bugRandomSpawnModel;
        private readonly BugConfiguration _bugConfiguration;
        private readonly List<int> _spawned = new();
        private float _sinceRoll;

        public BugRandomSpawnSystem(
            IShipRunBindingModel shipRunBindingModel,
            IFieldAgentCargoTargets fieldAgentCargoTargets,
            IBugRandomSpawnRule bugRandomSpawnRule,
            IBugSpawnService bugSpawnService,
            BugRandomSpawnModel bugRandomSpawnModel,
            BugConfiguration bugConfiguration) {
            _shipRunBindingModel = shipRunBindingModel;
            _fieldAgentCargoTargets = fieldAgentCargoTargets;
            _bugRandomSpawnRule = bugRandomSpawnRule;
            _bugSpawnService = bugSpawnService;
            _bugRandomSpawnModel = bugRandomSpawnModel;
            _bugConfiguration = bugConfiguration;
        }

        public void Tick() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (NetworkServer.active == false || ship == null || ship.IsFlying == false) {
                _sinceRoll = 0f;
                return;
            }

            _sinceRoll += Time.deltaTime;
            if (_sinceRoll < _bugConfiguration.RandomSpawnIntervalSeconds)
                return;

            _sinceRoll = 0f;
            if (_bugRandomSpawnRule.IsGroupComing(_bugRandomSpawnModel.IsEnabled, Random.value, _fieldAgentCargoTargets.Items.Count > 0))
                _bugSpawnService.ServerSpawnGroup(_bugRandomSpawnRule.CalculateGroupSize(Random.value), _spawned);
        }
    }
}
