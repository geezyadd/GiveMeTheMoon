using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Model {
    // Everything one field is generated from: the same request gives the same field.
    public sealed class FieldRequest {
        public const float NO_TARGET_COUNT_SCALE = 1f;

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
        // The stage preset's scale of the target balloon count (1 = the target configuration's count).
        public float TargetCountScale { get; }

        public FieldRequest(Vector3 stationA, Vector3 stationB, Vector3 cruiseStart, int loopIndex, int seed, IReadOnlyList<FieldRouteRequest> routeRequests,
                            FieldDifficultyProfile profile, float targetCountScale = NO_TARGET_COUNT_SCALE) {
            StationA = stationA;
            StationB = stationB;
            CruiseStart = cruiseStart;
            LoopIndex = loopIndex;
            Seed = seed;
            RouteRequests = routeRequests;
            Profile = profile;
            Difficulty = HardestDifficulty(routeRequests);
            TargetCountScale = targetCountScale;
        }

        private static float HardestDifficulty(IReadOnlyList<FieldRouteRequest> routeRequests) {
            float hardest = 0f;
            foreach (FieldRouteRequest routeRequest in routeRequests)
                hardest = Mathf.Max(hardest, routeRequest.Difficulty);

            return hardest;
        }
    }
}
