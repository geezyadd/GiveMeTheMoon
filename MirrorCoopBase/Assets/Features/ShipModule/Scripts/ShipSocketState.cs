using System;

namespace Features.ShipModule.Scripts {
    // One ship socket as the crew sees it; keyed by the socket's stable id in the ShipSockets model.
    public struct ShipSocketState : IEquatable<ShipSocketState> {
        public bool Occupied;
        public string ModuleId;
        // The seated player's stable key (PlayerKey.Id); empty when the seat is free.
        public string OccupantKey;

        public bool Equals(ShipSocketState other) =>
            Occupied == other.Occupied && string.Equals(ModuleId, other.ModuleId, StringComparison.Ordinal) && string.Equals(OccupantKey ?? string.Empty, other.OccupantKey ?? string.Empty, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is ShipSocketState other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Occupied, ModuleId, OccupantKey ?? string.Empty);
    }
}
