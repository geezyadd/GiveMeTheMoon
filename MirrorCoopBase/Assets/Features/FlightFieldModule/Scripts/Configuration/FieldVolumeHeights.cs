using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Configuration {
    // Heights read from the bands of a volume configuration.
    public static class FieldVolumeHeights {
        // The highest volume band: planets keep their clearance above and below it, a profile's volume stays under the
        // configuration's.
        public static float FindTop(FieldVolumeConfiguration volume) {
            float top = 0f;
            foreach (FieldVolumeConfiguration.Band band in volume.Bands)
                top = Mathf.Max(top, band.HeightMeters.y);

            return top;
        }
    }
}
