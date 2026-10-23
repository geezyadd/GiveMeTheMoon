using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // The configuration's profiles by layout: the corridor rules (walls, routes, beacons, zone shares) hold only for
    // the corridor profiles, the obstacle field rules only for the obstacle ones.
    public static class FieldTestProfiles {
        public static List<FieldDifficultyProfile> Corridors(FlightFieldConfiguration configuration) =>
            OfLayout(configuration, FieldLayoutKind.Corridors);

        public static List<FieldDifficultyProfile> Obstacles(FlightFieldConfiguration configuration) =>
            OfLayout(configuration, FieldLayoutKind.Obstacles);

        private static List<FieldDifficultyProfile> OfLayout(FlightFieldConfiguration configuration, FieldLayoutKind layout) =>
            configuration.DifficultyProfiles.Where(profile => profile.Layout == layout).ToList();
    }
}
