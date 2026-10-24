using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // Requests for tests: the debug scenario's flight with a chosen profile.
    public static class FieldTestRequests {
        public static FieldRequest WithProfile(FieldRequest request, FieldDifficultyProfile profile) =>
            new(request.StationA, request.StationB, request.CruiseStart, request.LoopIndex, request.Seed, request.RouteRequests, profile);
    }
}
