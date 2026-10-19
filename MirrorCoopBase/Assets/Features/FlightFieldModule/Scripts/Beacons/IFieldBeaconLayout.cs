using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Debris;

namespace Features.FlightFieldModule.Scripts.Beacons {
    public interface IFieldBeaconLayout {
        // Lays out the beacons of a field from its routes and its cells (walls, debris, empty); the result depends on
        // nothing else, so every peer gets the same beacons.
        public void Fill(IReadOnlyList<FieldBeaconRoute> routes, float cellSize, IFieldDebrisCells cells, List<FieldBeacon> beacons);
    }
}
