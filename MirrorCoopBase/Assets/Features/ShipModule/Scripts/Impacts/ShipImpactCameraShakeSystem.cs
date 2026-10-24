using System;
using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Impacts {
    // Every peer: a rock hit shakes the view of the local player while it rides the ship. The shake turns and moves
    // the look pivot after the look rig has posed it and before the camera brain reads it, so it never adds up.
    public sealed class ShipImpactCameraShakeSystem : IInitializable, IDisposable {
        private readonly IShipImpactEvents _shipImpactEvents;
        private readonly IGameCameraService _gameCameraService;
        private readonly GameCameraModel _gameCameraModel;
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IShipClientBindingModel _shipClientBindingModel;
        private readonly IShipRockImpactFeelSource _shipRockImpactFeelSource;
        private readonly ShipCameraShake _shake = new();

        public ShipImpactCameraShakeSystem(
            IShipImpactEvents shipImpactEvents,
            IGameCameraService gameCameraService,
            GameCameraModel gameCameraModel,
            IShipRunBindingModel shipRunBindingModel,
            IShipClientBindingModel shipClientBindingModel,
            IShipRockImpactFeelSource shipRockImpactFeelSource) {
            _shipImpactEvents = shipImpactEvents;
            _gameCameraService = gameCameraService;
            _gameCameraModel = gameCameraModel;
            _shipRunBindingModel = shipRunBindingModel;
            _shipClientBindingModel = shipClientBindingModel;
            _shipRockImpactFeelSource = shipRockImpactFeelSource;
        }

        public void Initialize() {
            _shipImpactEvents.OnImpact += OnImpact;
            _gameCameraService.LookApplied += OnLookApplied;
        }

        public void Dispose() {
            _shipImpactEvents.OnImpact -= OnImpact;
            _gameCameraService.LookApplied -= OnLookApplied;
        }

        private bool IsLocalPlayerOnBoard() {
            ShipBase ship = _shipRunBindingModel.Ship != null ? _shipRunBindingModel.Ship : _shipClientBindingModel.Ship;
            return ship != null && ship.Riders.IsLocalPlayerRiding();
        }

        private void OnImpact(ShipRockImpact impact) {
            if (IsLocalPlayerOnBoard())
                _shake.Start(_shipRockImpactFeelSource.GetFeel(impact.Kind), impact.Strength);
        }

        private void OnLookApplied() {
            Transform pivot = _gameCameraModel.LookPivot;
            if (!_shake.IsActive || pivot == null)
                return;

            _shake.Advance(Time.deltaTime);
            pivot.localRotation *= _shake.SampleRotation();
            pivot.localPosition += _shake.SampleOffset();
        }
    }
}
