using System;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Network {
    // One centerline cell of a route, in centerline order: a sync list cannot nest the cells inside the route entry.
    // The half width is the corridor's there (cells on each side), so a peer knows where the lane ends.
    [Serializable]
    public struct FieldRouteCellEntry {
        public int RouteId;
        public Vector2Int Cell;
        public int HalfWidth;
    }
}
