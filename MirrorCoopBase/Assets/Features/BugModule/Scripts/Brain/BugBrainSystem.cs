using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Server;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.BugModule.Scripts.Brain {
    // Server, every frame: every bug's mind takes its step; a bug whose field agent is gone (all agents removed) leaves
    // the bug model, and every bug goes with the run's ship or the server.
    public sealed class BugBrainSystem : ITickable {
        private readonly BugServerModel _bugServerModel;
        private readonly IFieldAgentControl _fieldAgentControl;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IBugBrainService _bugBrainService;
        private readonly List<int> _ids = new();

        public BugBrainSystem(BugServerModel bugServerModel, IFieldAgentControl fieldAgentControl, IShipRunBindingModel shipRunBindingModel, IBugBrainService bugBrainService) {
            _bugServerModel = bugServerModel;
            _fieldAgentControl = fieldAgentControl;
            _shipRunBindingModel = shipRunBindingModel;
            _bugBrainService = bugBrainService;
        }

        public void Tick() {
            if (_bugServerModel.Count == 0)
                return;

            ShipBase ship = _shipRunBindingModel.Ship;
            if (NetworkServer.active == false || ship == null) {
                _bugServerModel.Clear();
                return;
            }

            _ids.Clear();
            _ids.AddRange(_bugServerModel.Ids);
            foreach (int id in _ids)
                StepBug(id, ship);
        }

        private void StepBug(int id, ShipBase ship) {
            if (_bugServerModel.TryGet(id, out BugAgent bug) == false)
                return;

            if (_fieldAgentControl.TryGetAgent(id, out FieldAgentState state) == false) {
                _bugServerModel.Remove(id);
                return;
            }

            _bugBrainService.ServerStep(bug, state, ship, Time.deltaTime);
        }
    }
}
