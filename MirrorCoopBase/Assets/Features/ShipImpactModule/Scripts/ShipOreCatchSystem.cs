using System.Collections.Generic;
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts.Hull;
using Zenject;

namespace Features.ShipImpactModule.Scripts {
    // Server, every physics step of a flight (takeoff, cruise, landing): every piece of ore the hull touched during
    // the step reaches the ship, pulled in on a rope or flown into. Ore neither pushes nor damages the ship. The hull's
    // sweep of the step is the one the rock check uses; the ore is read from the live field (one query around all the
    // boxes, then every box).
    public sealed class ShipOreCatchSystem : IFixedTickable {
        private readonly IShipHullStepSource _shipHullStepSource;
        private readonly IFieldOreQuery _fieldOreQuery;
        private readonly IFieldOreCollector _fieldOreCollector;
        private readonly List<FieldOreDescription> _ores = new();

        public ShipOreCatchSystem(IShipHullStepSource shipHullStepSource, IFieldOreQuery fieldOreQuery, IFieldOreCollector fieldOreCollector) {
            _shipHullStepSource = shipHullStepSource;
            _fieldOreQuery = fieldOreQuery;
            _fieldOreCollector = fieldOreCollector;
        }

        public void FixedTick() {
            if (!_shipHullStepSource.TryGetStep(out ShipHullStep step))
                return;

            ShipHullSweep query = ShipHullSweep.Create(ShipHullBox.Enclose(step.Boxes), step.From, step.To);
            _fieldOreQuery.FindInBox(query.Center, query.HalfExtents, query.Rotation, _ores);
            foreach (FieldOreDescription ore in _ores) {
                if (IsTouching(step, ore))
                    _fieldOreCollector.ServerCollect(ore.Id);
            }
        }

        private static bool IsTouching(ShipHullStep step, FieldOreDescription ore) {
            for (int i = 0; i < step.Boxes.Count; i++) {
                if (ShipHullSweep.Create(step.Boxes[i], step.From, step.To).Touches(ore.Center, ore.Radius))
                    return true;
            }

            return false;
        }
    }
}
