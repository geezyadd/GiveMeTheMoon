using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // One generated obstacle field of the field tests with everything they check on it: the request it came from, its
    // profile, every rock laid out as the peers build them with the slalom way, and the narrowest gap in the plane.
    public sealed class FieldObstacleSample {
        public string Name { get; }
        public FieldRequest Request { get; }
        public FlightField Field { get; }
        public FieldDifficultyProfile Profile { get; }
        public FieldSlalomReport Report { get; }
        // The smallest gap in the flight plane between two debris rocks, a rock and a giant, or two giants.
        public float PlaneGapMeters { get; }

        public FieldObstacleSample(FieldRequest request, FlightField field, FieldDifficultyProfile profile, FieldSlalomReport report, float planeGapMeters) {
            Name = $"seed {field.Seed} loop {field.LoopIndex} ({field.ProfileId})";
            Request = request;
            Field = field;
            Profile = profile;
            Report = report;
            PlaneGapMeters = planeGapMeters;
        }
    }
}
