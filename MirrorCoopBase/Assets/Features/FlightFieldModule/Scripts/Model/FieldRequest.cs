using System.Collections.Generic;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // Everything one field is generated from: the same request gives the same field.
    public sealed class FieldRequest {
        public Vector3 StationA { get; }
        public Vector3 StationB { get; }
        // Where the takeoff ends and the pilot takes over; its height is the height of the field plane.
        public Vector3 CruiseStart { get; }
        public int LoopIndex { get; }
        public int Seed { get; }
        public IReadOnlyList<FieldRouteRequest> RouteRequests { get; }

        public FieldRequest(Vector3 stationA, Vector3 stationB, Vector3 cruiseStart, int loopIndex, int seed, IReadOnlyList<FieldRouteRequest> routeRequests) {
            StationA = stationA;
            StationB = stationB;
            CruiseStart = cruiseStart;
            LoopIndex = loopIndex;
            Seed = seed;
            RouteRequests = routeRequests;
        }
    }
}
