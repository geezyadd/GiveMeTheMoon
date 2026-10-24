using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.NetworkModelModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Mirror;
using UnityEngine;
using Zenject;

namespace Features.ShipModule.Scripts.Impacts {
    // Server, every physics step of a flight (takeoff, cruise, landing): the target balloons the hull flew into since
    // the last step pop as a ShipCollision. A balloon neither pushes nor damages the ship. The hull boxes are swept
    // like for rocks, the balloons are read from the field data (one query around all the boxes, then every box).
    public sealed class ShipFieldTargetCollisionSystem : IFixedTickable {
        private static readonly PlayerKey _noShooter = new(string.Empty);

        private readonly IShipRunBindingModel _shipRunBindingModel;
        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly IFieldTargetQuery _fieldTargetQuery;
        private readonly IFieldTargetPopper _fieldTargetPopper;
        private readonly ShipRockImpactConfiguration _shipRockImpactConfiguration;
        private readonly List<FieldTargetDescription> _targets = new();

        private Pose _lastPose;
        private bool _hasLastPose;

        public ShipFieldTargetCollisionSystem(IShipRunBindingModel shipRunBindingModel, IReadOnlyShipRunModel shipRunModel, IFieldTargetQuery fieldTargetQuery,
                                              IFieldTargetPopper fieldTargetPopper, ShipRockImpactConfiguration shipRockImpactConfiguration) {
            _shipRunBindingModel = shipRunBindingModel;
            _shipRunModel = shipRunModel;
            _fieldTargetQuery = fieldTargetQuery;
            _fieldTargetPopper = fieldTargetPopper;
            _shipRockImpactConfiguration = shipRockImpactConfiguration;
        }

        public void FixedTick() {
            ShipBase ship = _shipRunBindingModel.Ship;
            if (!NetworkServer.active || ship == null || !ship.IsFlying || !IsCheckedPhase(_shipRunModel.Phase)) {
                _hasLastPose = false;
                return;
            }

            Pose pose = new(ship.transform.position, ship.transform.rotation);
            Pose from = IsSweepable(pose) ? _lastPose : pose;
            _lastPose = pose;
            _hasLastPose = true;
            IReadOnlyList<ShipHullBox> boxes = ship.HullVolume.Boxes;
            ShipHullSweep query = ShipHullSweep.Create(ShipHullBox.Enclose(boxes), from, pose);
            _fieldTargetQuery.FindInBox(query.Center, query.HalfExtents, query.Rotation, _targets);
            foreach (FieldTargetDescription target in _targets) {
                if (IsTouching(boxes, from, pose, target))
                    _fieldTargetPopper.ServerPop(target.Id, FieldTargetPopCause.ShipCollision, _noShooter);
            }
        }

        private static bool IsCheckedPhase(ShipRunPhase phase) =>
            phase is ShipRunPhase.Takeoff or ShipRunPhase.Cruise or ShipRunPhase.Landing;

        // A world shift or a debug move jumps the ship: that jump is not swept.
        private bool IsSweepable(Pose pose) =>
            _hasLastPose && (pose.position - _lastPose.position).sqrMagnitude <= _shipRockImpactConfiguration.MaxSweepMeters * _shipRockImpactConfiguration.MaxSweepMeters;

        private static bool IsTouching(IReadOnlyList<ShipHullBox> boxes, Pose from, Pose to, FieldTargetDescription target) {
            for (int i = 0; i < boxes.Count; i++) {
                if (ShipHullSweep.Create(boxes[i], from, to).Touches(target.Center, target.Radius))
                    return true;
            }

            return false;
        }
    }
}
