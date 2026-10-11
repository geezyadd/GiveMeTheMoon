using System;

namespace Features.ShipModule.Scripts {
    // One ship socket as the crew sees it; keyed by the socket's stable id in the ShipSockets model.
    public struct ShipSocketState : IEquatable<ShipSocketState> {
        public bool Occupied;
        public string ModuleId;
        public uint OccupantNetId;

        public bool Equals(ShipSocketState other) =>
            Occupied == other.Occupied && string.Equals(ModuleId, other.ModuleId, StringComparison.Ordinal) && OccupantNetId == other.OccupantNetId;

        public override bool Equals(object obj) =>
            obj is ShipSocketState other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Occupied, ModuleId, OccupantNetId);
    }
}
