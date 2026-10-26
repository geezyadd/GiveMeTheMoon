using System.Collections.Generic;

namespace Features.FlightFieldModule.Scripts.Editor {
    // The flight plane of a field as the slalom numbers see it: its area and the diameters of its giants.
    public sealed class FieldSlalomPlane {
        public float AreaSquareMeters { get; }
        public IReadOnlyList<float> GiantDiametersMeters { get; }

        public FieldSlalomPlane(float areaSquareMeters, IReadOnlyList<float> giantDiametersMeters) {
            AreaSquareMeters = areaSquareMeters;
            GiantDiametersMeters = giantDiametersMeters;
        }
    }
}
