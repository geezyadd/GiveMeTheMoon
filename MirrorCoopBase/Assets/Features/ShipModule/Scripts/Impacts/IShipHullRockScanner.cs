using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    public interface IShipHullRockScanner {
        // Fills contacts (cleared first) with every rock the hull boxes touch on their way from one pose to the other,
        // each rock once. A large rock counts from further on the way (the look-ahead) and with the configured margin.
        public void Scan(IReadOnlyList<ShipHullBox> boxes, Pose from, Pose to, Vector3 largeLookAhead, List<ShipRockContact> contacts);
    }
}
