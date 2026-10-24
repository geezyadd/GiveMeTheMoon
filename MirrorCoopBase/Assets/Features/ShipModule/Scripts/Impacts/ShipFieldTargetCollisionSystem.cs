using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.NetworkModelModule.Scripts;
using Zenject;

namespace Features.ShipModule.Scripts.Impacts {
    // Server, every physics step of a flight (takeoff, cruise, landing): the target balloons the hull flew into during
    // the step pop as a ShipCollision. A balloon neither pushes nor damages the ship. The hull's sweep of the step is
    // the one the rock check uses; the balloons are read from the field data (one query around all the boxes, then
    // every box).
    public sealed class ShipFieldTargetCollisionSystem : IFixedTickable {
        private static readonly PlayerKey _noShooter = new(string.Empty);

        private readonly IShipHullStepSource _shipHullStepSource;
        private readonly IFieldTargetQuery _fieldTargetQuery;
        private readonly IFieldTargetPopper _fieldTargetPopper;
        private readonly List<FieldTargetDescription> _targets = new();

        public ShipFieldTargetCollisionSystem(IShipHullStepSource shipHullStepSource, IFieldTargetQuery fieldTargetQuery, IFieldTargetPopper fieldTargetPopper) {
            _shipHullStepSource = shipHullStepSource;
            _fieldTargetQuery = fieldTargetQuery;
            _fieldTargetPopper = fieldTargetPopper;
        }

        public void FixedTick() {
            if (!_shipHullStepSource.TryGetStep(out ShipHullStep step))
                return;

            ShipHullSweep query = ShipHullSweep.Create(ShipHullBox.Enclose(step.Boxes), step.From, step.To);
            _fieldTargetQuery.FindInBox(query.Center, query.HalfExtents, query.Rotation, _targets);
            foreach (FieldTargetDescription target in _targets) {
                if (IsTouching(step, target))
                    _fieldTargetPopper.ServerPop(target.Id, FieldTargetPopCause.ShipCollision, _noShooter);
            }
        }

        private static bool IsTouching(ShipHullStep step, FieldTargetDescription target) {
            for (int i = 0; i < step.Boxes.Count; i++) {
                if (ShipHullSweep.Create(step.Boxes[i], step.From, step.To).Touches(target.Center, target.Radius))
                    return true;
            }

            return false;
        }
    }
}
