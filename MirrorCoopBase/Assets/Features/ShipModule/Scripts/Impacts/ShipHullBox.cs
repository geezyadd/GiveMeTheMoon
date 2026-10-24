using System;
using UnityEngine;

namespace Features.ShipModule.Scripts.Impacts {
    // One box of the hull volume in ship space.
    [Serializable]
    public struct ShipHullBox {
        public Vector3 Center;
        public Vector3 HalfExtents;
    }
}
