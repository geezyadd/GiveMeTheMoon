using System;
using Features.CameraModule.Contracts;
using Zenject;

namespace Features.ShipModule.Scripts.Impacts {
    // Every peer: a rock hit shakes the view of the local player while it rides the ship.
    public sealed class ShipImpactCameraShakeSystem : IInitializable, IDisposable {
        private readonly IShipImpactEvents _shipImpactEvents;
        private readonly ICameraShakeService _cameraShakeService;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipClientBindingModel _shipClientBindingModel;
        private readonly IShipRockImpactFeelSource _shipRockImpactFeelSource;

        public ShipImpactCameraShakeSystem(
            IShipImpactEvents shipImpactEvents,
            ICameraShakeService cameraShakeService,
            IShipRunBindingModel shipRunBindingModel,
            IShipClientBindingModel shipClientBindingModel,
            IShipRockImpactFeelSource shipRockImpactFeelSource) {
            _shipImpactEvents = shipImpactEvents;
            _cameraShakeService = cameraShakeService;
            _shipRunBindingModel = shipRunBindingModel;
            _shipClientBindingModel = shipClientBindingModel;
            _shipRockImpactFeelSource = shipRockImpactFeelSource;
        }

        public void Initialize() =>
            _shipImpactEvents.OnImpact += OnImpact;

        public void Dispose() =>
            _shipImpactEvents.OnImpact -= OnImpact;

        private bool IsLocalPlayerOnBoard() {
            ShipBase ship = _shipRunBindingModel.Ship != null ? _shipRunBindingModel.Ship : _shipClientBindingModel.Ship;
            return ship != null && ship.Riders.IsLocalPlayerRiding();
        }

        private void OnImpact(ShipRockImpact impact) {
            if (IsLocalPlayerOnBoard())
                _cameraShakeService.AddShake(_shipRockImpactFeelSource.GetFeel(impact.Kind).Shake, impact.Strength);
        }
    }
}
