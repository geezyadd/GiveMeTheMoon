using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network.Generated;

namespace Features.FlightFieldModule.Scripts.Beacons {
    public interface IFieldBeaconSource {
        // The beacons of the live field, from its routes, route cells, placements and debris runs.
        public void Read(IReadOnlyLiveFieldModel liveFieldModel, List<FieldBeacon> beacons);
    }
}
