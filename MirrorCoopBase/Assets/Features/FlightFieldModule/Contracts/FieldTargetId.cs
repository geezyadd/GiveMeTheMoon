using System;

namespace Features.FlightFieldModule.Contracts {
    // A target balloon of the current flight field, the same on every peer: its index in the field's target list. It
    // names a balloon in field space, so it stays the same across world shifts.
    public readonly struct FieldTargetId : IEquatable<FieldTargetId> {
        public int Index { get; }

        public FieldTargetId(int index) =>
            Index = index;

        public bool Equals(FieldTargetId other) =>
            Index == other.Index;

        public override bool Equals(object obj) =>
            obj is FieldTargetId other && Equals(other);

        public override int GetHashCode() =>
            Index;

        public override string ToString() =>
            $"target {Index}";
    }
}
