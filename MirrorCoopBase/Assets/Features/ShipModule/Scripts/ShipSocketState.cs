using System;

namespace Features.ShipModule.Scripts {
    // One ship socket as the crew sees it; keyed by the socket's stable id in the ShipSockets model.
    public struct ShipSocketState : IEquatable<ShipSocketState> {
        public bool Occupied;
        public ItemViewId View;
        public uint OccupantNetId;

        public bool Equals(ShipSocketState other) =>
            Occupied == other.Occupied && View == other.View && OccupantNetId == other.OccupantNetId;

        public override bool Equals(object obj) =>
            obj is ShipSocketState other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Occupied, View, OccupantNetId);
    }
}
