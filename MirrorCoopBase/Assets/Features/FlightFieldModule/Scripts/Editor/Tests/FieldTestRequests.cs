using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Requests;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Requests for tests: the debug scenario's flight with a chosen profile, or a straight flight along +z.
    public static class FieldTestRequests {
        private const float TAKEOFF_FORWARD = 350f;
        private const float TAKEOFF_HEIGHT = 100f;

        public static FieldRequest WithProfile(FieldRequest request, FieldDifficultyProfile profile) =>
            new(request.StationA, request.StationB, request.CruiseStart, request.LoopIndex, request.Seed, request.RouteRequests, profile);

        public static FieldRequest Straight(int seed, float distance, FieldDifficultyProfile profile, params float[] difficulties) {
            List<FieldRouteRequest> routeRequests = new();
            foreach (float difficulty in difficulties)
                routeRequests.Add(new FieldRouteRequest(difficulty));

            Vector3 cruiseStart = new(0f, TAKEOFF_HEIGHT, TAKEOFF_FORWARD);
            return new FieldRequest(Vector3.zero, Vector3.forward * distance, cruiseStart, 0, seed, routeRequests, profile);
        }

        public static FieldDifficultyProfile FindProfile(FlightFieldConfiguration configuration, string id) =>
            new FieldProfileLookup(configuration).GetRequired(id);
    }
}
