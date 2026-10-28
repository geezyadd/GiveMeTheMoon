using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
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
        // The preset of the field's structure; every number of it is read at Difficulty.
        public FieldDifficultyProfile Profile { get; }
        // The field's difficulty (0..1): the hardest of its routes.
        public float Difficulty { get; }

        public FieldRequest(Vector3 stationA, Vector3 stationB, Vector3 cruiseStart, int loopIndex, int seed, IReadOnlyList<FieldRouteRequest> routeRequests,
                            FieldDifficultyProfile profile) {
            StationA = stationA;
            StationB = stationB;
            CruiseStart = cruiseStart;
            LoopIndex = loopIndex;
            Seed = seed;
            RouteRequests = routeRequests;
            Profile = profile;
            Difficulty = HardestDifficulty(routeRequests);
        }

        private static float HardestDifficulty(IReadOnlyList<FieldRouteRequest> routeRequests) {
            float hardest = 0f;
            foreach (FieldRouteRequest routeRequest in routeRequests)
                hardest = Mathf.Max(hardest, routeRequest.Difficulty);

            return hardest;
        }
    }
}
