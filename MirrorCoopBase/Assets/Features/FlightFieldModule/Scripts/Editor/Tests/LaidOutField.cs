using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Beacons;
using Features.FlightFieldModule.Scripts.Model;

namespace Features.FlightFieldModule.Scripts.Editor.Tests {
    // A generated field of the beacon tests with the seed and loop it came from and its beacons.
    public sealed class LaidOutField {
        public FlightField Field { get; }
        public int Seed { get; }
        public int Loop { get; }
        public IReadOnlyList<FieldBeacon> Beacons { get; }

        public LaidOutField(FlightField field, int seed, int loop, IReadOnlyList<FieldBeacon> beacons) {
            Field = field;
            Seed = seed;
            Loop = loop;
            Beacons = beacons;
        }
    }
}
