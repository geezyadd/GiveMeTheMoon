using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Decor {
    // One band of the volume on one side of the flight plane (+1 above, -1 below).
    public readonly struct FieldVolumeBandKey {
        public FieldVolumeConfiguration.Band Band { get; }
        public int Index { get; }
        public int Side { get; }

        public FieldVolumeBandKey(FieldVolumeConfiguration.Band band, int index, int side) {
            Band = band;
            Index = index;
            Side = side;
        }
    }
}
