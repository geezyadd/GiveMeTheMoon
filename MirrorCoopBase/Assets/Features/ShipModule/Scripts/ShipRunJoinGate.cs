using System;
using Features.GameCoreModule.Contracts;
using Features.ShipModule.Scripts.Generated;
using Zenject;

namespace Features.ShipModule.Scripts {
    // Tells the session whether the run lets players in: only while the ship stands at a station (Build).
    public sealed class ShipRunJoinGate : ISessionJoinGate, IInitializable, IDisposable {
        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly IShipRunBindingModel _shipRunBindingModel;

        public ShipRunJoinGate(IReadOnlyShipRunModel shipRunModel, IShipRunBindingModel shipRunBindingModel) {
            _shipRunModel = shipRunModel;
            _shipRunBindingModel = shipRunBindingModel;
        }

        public event Action OnWindowChanged;

        public RunJoinWindow Window {
            get {
                if (_shipRunBindingModel.Ship == null)
                    return RunJoinWindow.NoRun;

                return _shipRunModel.Phase == ShipRunPhase.Build ? RunJoinWindow.AtStation : RunJoinWindow.InFlight;
            }
        }

        public void Initialize() {
            _shipRunModel.OnPhaseChanged += OnRunChanged;
            _shipRunModel.OnAvailableChanged += OnRunChanged;
        }

        public void Dispose() {
            _shipRunModel.OnPhaseChanged -= OnRunChanged;
            _shipRunModel.OnAvailableChanged -= OnRunChanged;
        }

        private void OnRunChanged() =>
            OnWindowChanged?.Invoke();
    }
}
