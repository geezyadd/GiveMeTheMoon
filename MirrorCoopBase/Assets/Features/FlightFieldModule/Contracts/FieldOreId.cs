using System;

namespace Features.FlightFieldModule.Contracts {
    // A piece of ore of the current flight field, the same on every peer: the number the server gave it when a broken
    // rock dropped it. It names the piece in field space, so it stays the same across world shifts.
    public readonly struct FieldOreId : IEquatable<FieldOreId> {
        public int Value { get; }

        public FieldOreId(int value) =>
            Value = value;

        public bool Equals(FieldOreId other) =>
            Value == other.Value;

        public override bool Equals(object obj) =>
            obj is FieldOreId other && Equals(other);

        public override int GetHashCode() =>
            Value;

        public override string ToString() =>
            $"ore {Value}";
    }
}
