using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // Server: works out the hull's sweep once per physics step of takeoff, cruise and landing (not Build / Wreck) and
    // hands the same step to every caller of that step. A world shift or a debug move jumps the ship: that jump is not
    // swept. Outside a checked phase it forgets the last pose, so the next flight starts unswept.
    public sealed class ShipHullStepTracker : IShipHullStepSource {
        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;

        private Pose _lastPose;
        private bool _hasLastPose;
        private float _stepTime = float.NegativeInfinity;
        private bool _hasStep;
        private ShipHullStep _step;

        public ShipHullStepTracker(IShipRunBindingModel shipRunBindingModel, IReadOnlyShipRunModel shipRunModel, ShipRockImpactConfiguration shipRockImpactConfiguration) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipRunModel = shipRunModel;
            _shipRockImpactConfiguration = shipRockImpactConfiguration;
        }

        public bool TryGetStep(out ShipHullStep step) {
            bool isNewStep = !Mathf.Approximately(Time.fixedTime, _stepTime);
            if (isNewStep) {
                _stepTime = Time.fixedTime;
                _hasStep = TrackStep(out _step);
            }

            step = _step;
            return _hasStep;
        }

        private bool TrackStep(out ShipHullStep step) {
            step = default;
            ShipBase ship = _shipRunBindingModel.Ship;
            if (!NetworkServer.active || ship == null || !ship.IsFlying || !IsCheckedPhase(_shipRunModel.Phase)) {
                _hasLastPose = false;
                return false;
            }

            Pose pose = new(ship.transform.position, ship.transform.rotation);
            Pose from = IsSweepable(pose) ? _lastPose : pose;
            _lastPose = pose;
            _hasLastPose = true;
            step = new ShipHullStep(ship, ship.HullVolume.Boxes, from, pose);
            return true;
        }

        private static bool IsCheckedPhase(ShipRunPhase phase) =>
            phase is ShipRunPhase.Takeoff or ShipRunPhase.Cruise or ShipRunPhase.Landing;

        private bool IsSweepable(Pose pose) =>
            _hasLastPose && (pose.position - _lastPose.position).sqrMagnitude <= _shipRockImpactConfiguration.MaxSweepMeters * _shipRockImpactConfiguration.MaxSweepMeters;
    }
}
