using System.Collections.Generic;

namespace Features.FlightFieldModule.Scripts.Editor {
    // The upper diameters of the small, medium and large rocks in the editor's size counts (debug map, obstacle test
    // table); above the last one the massive rocks.
    public static class FieldRockSizeLimits {
        public static readonly IReadOnlyList<float> UpperDiametersMeters = new[] { 35f, 150f, 350f };
    }
}
