using System;
using System.Diagnostics;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Deck;
using Features.FieldAgentModule.Scripts.Steering;
using Features.ShipModule.Scripts;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.FieldAgentModule.Scripts.Server {
    // Server, every frame: steps every agent of the model by its mode in one pass, then sends the batch to the
    // clients at the configured rate. Without a bound ship (lobby, menu, between maps) there are no agents.
    public sealed class FieldAgentSimulationSystem : ITickable {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly FieldAgentServerModel _fieldAgentServerModel;
        private readonly FieldAgentDiagnosticsModel _fieldAgentDiagnosticsModel;
        private readonly FieldAgentConfiguration _fieldAgentConfiguration;
        private readonly IFieldAgentFlightStepService _fieldAgentFlightStepService;
        private readonly IFieldAgentDeckStepService _fieldAgentDeckStepService;
        private readonly IFieldAgentTransitionService _fieldAgentTransitionService;
        private readonly IFieldAgentBatchPublishService _fieldAgentBatchPublishService;
        private readonly Stopwatch _stopwatch = new();
        private float _sinceBatch;

        public FieldAgentSimulationSystem(
            IShipRunBindingModel shipRunBindingModel,
            FieldAgentServerModel fieldAgentServerModel,
            FieldAgentDiagnosticsModel fieldAgentDiagnosticsModel,
            FieldAgentConfiguration fieldAgentConfiguration,
            IFieldAgentFlightStepService fieldAgentFlightStepService,
            IFieldAgentDeckStepService fieldAgentDeckStepService,
            IFieldAgentTransitionService fieldAgentTransitionService,
            IFieldAgentBatchPublishService fieldAgentBatchPublishService) {
            _shipRunBindingModel = shipRunBindingModel;
            _fieldAgentServerModel = fieldAgentServerModel;
            _fieldAgentDiagnosticsModel = fieldAgentDiagnosticsModel;
            _fieldAgentConfiguration = fieldAgentConfiguration;
            _fieldAgentFlightStepService = fieldAgentFlightStepService;
            _fieldAgentDeckStepService = fieldAgentDeckStepService;
            _fieldAgentTransitionService = fieldAgentTransitionService;
            _fieldAgentBatchPublishService = fieldAgentBatchPublishService;
        }

        public void Tick() {
            if (NetworkServer.active == false)
                return;

            ShipBase ship = _shipRunBindingModel.Ship;
            if (ship == null) {
                _fieldAgentServerModel.Clear();
                return;
            }

            StepAll(ship, Time.time, Time.deltaTime);
            PublishOnSchedule(Time.deltaTime);
        }

        private void StepAll(ShipBase ship, float time, float deltaTime) {
            _stopwatch.Restart();
            Transform shipTransform = ship.transform;
            _fieldAgentServerModel.RecordShipPosition(shipTransform.position, deltaTime);
            FieldAgentShipFrame frame = new(true, shipTransform.position, shipTransform.rotation, _fieldAgentServerModel.ShipVelocity);
            for (int i = 0; i < _fieldAgentServerModel.Count; i++)
                StepAgent(i, frame, time, deltaTime);

            _stopwatch.Stop();
            _fieldAgentDiagnosticsModel.RecordTick(_stopwatch.Elapsed.TotalMilliseconds);
        }

        private void StepAgent(int index, in FieldAgentShipFrame ship, float time, float deltaTime) {
            ref FieldAgentState state = ref _fieldAgentServerModel.GetState(index);
            FieldAgentProfile profile = _fieldAgentConfiguration.Profiles[state.KindIndex];
            switch (state.Mode) {
                case FieldAgentMode.Flight:
                    _fieldAgentFlightStepService.Step(index, _fieldAgentServerModel, profile, ship, time, deltaTime);
                    break;
                case FieldAgentMode.Landing:
                    _fieldAgentTransitionService.StepLanding(ref state, profile, deltaTime);
                    break;
                case FieldAgentMode.Deck:
                    _fieldAgentDeckStepService.Step(index, _fieldAgentServerModel, profile, ship, time, deltaTime);
                    break;
                case FieldAgentMode.TakingOff:
                    _fieldAgentTransitionService.StepTakeOff(ref state, profile, ship, deltaTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state.Mode), state.Mode, $"Field agent {state.Id} has no movement mode.");
            }
        }

        private void PublishOnSchedule(float deltaTime) {
            _sinceBatch += deltaTime;
            if (_sinceBatch < 1f / _fieldAgentConfiguration.SendRate)
                return;

            _sinceBatch = 0f;
            _fieldAgentBatchPublishService.Publish(_fieldAgentServerModel);
            _fieldAgentDiagnosticsModel.RecordBatch();
        }
    }
}
