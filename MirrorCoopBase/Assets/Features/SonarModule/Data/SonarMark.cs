using System;
using UnityEngine;

namespace Features.SonarModule.Data {
    // A contact the navigator marked for the crew: which one, of what kind, where it was when it was marked and the
    // server's network time when the mark goes out.
    [Serializable]
    public struct SonarMark : IEquatable<SonarMark> {
        public string ContactId;
        public string KindId;
        public Vector3 Position;
        public double ExpiresAt;

        public SonarMark(string contactId, string kindId, Vector3 position, double expiresAt) {
            ContactId = contactId;
            KindId = kindId;
            Position = position;
            ExpiresAt = expiresAt;
        }

        public bool Equals(SonarMark other) =>
            ContactId == other.ContactId && KindId == other.KindId && Position == other.Position && ExpiresAt.Equals(other.ExpiresAt);

        public override bool Equals(object obj) =>
            obj is SonarMark other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(ContactId, KindId, Position, ExpiresAt);
    }
}
