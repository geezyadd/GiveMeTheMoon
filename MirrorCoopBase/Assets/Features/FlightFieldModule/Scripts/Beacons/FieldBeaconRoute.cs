using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Model;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Beacons {
    // A route as the beacons see it, the same on the server and on a peer: its kind and its centerline cells in order
    // with the corridor's half width at each.
    public sealed class FieldBeaconRoute {
        public int Id { get; }
        public FieldRouteKind Kind { get; }
        public IReadOnlyList<Vector2Int> Centerline { get; }
        public IReadOnlyList<int> HalfWidths { get; }

        public FieldBeaconRoute(int id, FieldRouteKind kind, IReadOnlyList<Vector2Int> centerline, IReadOnlyList<int> halfWidths) {
            Id = id;
            Kind = kind;
            Centerline = centerline;
            HalfWidths = halfWidths;
        }
    }
}
