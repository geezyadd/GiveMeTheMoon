using System.Collections.Generic;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Hull;
using UnityEngine;

namespace Features.ShipImpactModule.Scripts {
    // The hull's motion over one physics step of a flight: the flying ship, its hull boxes and the pose they are swept
    // from (the last step's pose, or the pose now after a jump that is not swept) to the pose now.
    public readonly struct ShipHullStep {
        public ShipBase Ship { get; }
        public IReadOnlyList<ShipHullBox> Boxes { get; }
        public Pose From { get; }
        public Pose To { get; }

        public ShipHullStep(ShipBase ship, IReadOnlyList<ShipHullBox> boxes, Pose from, Pose to) {
            Ship = ship;
            Boxes = boxes;
            From = from;
            To = to;
        }
    }
}
