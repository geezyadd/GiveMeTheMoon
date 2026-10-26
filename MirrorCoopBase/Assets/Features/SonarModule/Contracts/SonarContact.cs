using UnityEngine;

namespace Features.SonarModule.Contracts {
    // One thing the sonar sees: an id stable on every peer (a mark names it), its kind, its middle in the world now
    // and how far it reaches from there.
    public readonly struct SonarContact {
        public string Id { get; }
        public SonarContactKind Kind { get; }
        public Vector3 Position { get; }
        public float Radius { get; }

        public SonarContact(string id, SonarContactKind kind, Vector3 position, float radius) {
            Id = id;
            Kind = kind;
            Position = position;
            Radius = radius;
        }
    }
}
