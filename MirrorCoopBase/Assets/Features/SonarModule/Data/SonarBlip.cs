using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Data {
    // One echo as the scope shows it this frame: -1..1 around the ship with the nose up, its real radius as a share of
    // the scope's radius, fading out.
    public readonly struct SonarBlip {
        public string ContactId { get; }
        public SonarContactKind Kind { get; }
        public Vector2 ScopePosition { get; }
        public float RadiusShare { get; }
        public float Alpha { get; }
        public bool IsMarked { get; }

        public SonarBlip(string contactId, SonarContactKind kind, Vector2 scopePosition, float radiusShare, float alpha, bool isMarked) {
            ContactId = contactId;
            Kind = kind;
            ScopePosition = scopePosition;
            RadiusShare = radiusShare;
            Alpha = alpha;
            IsMarked = isMarked;
        }
    }
}
