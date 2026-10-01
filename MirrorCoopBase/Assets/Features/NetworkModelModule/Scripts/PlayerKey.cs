using System;

namespace Features.NetworkModelModule.Scripts {
    public readonly struct PlayerKey : IEquatable<PlayerKey> {
        public PlayerKey(string id) {
            Id = id ?? string.Empty;
        }

        public string Id { get; }

        public bool IsEmpty =>
            string.IsNullOrEmpty(Id);

        public bool Equals(PlayerKey other) =>
            Id == other.Id;

        public override bool Equals(object obj) =>
            obj is PlayerKey other && Equals(other);

        public override int GetHashCode() =>
            Id == null ? 0 : Id.GetHashCode();

        public override string ToString() =>
            Id ?? string.Empty;
    }
}
