using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Data {
    // One crew mark as this peer's screen shows it: over its object, or on the edge with the arrow turned to it.
    public readonly struct SonarMarkerDisplay {
        public SonarContactKind Kind { get; }
        public Vector2 ScreenPosition { get; }
        public bool IsOnScreen { get; }
        public float ArrowDegrees { get; }
        public float DistanceMeters { get; }

        public SonarMarkerDisplay(SonarContactKind kind, Vector2 screenPosition, bool isOnScreen, float arrowDegrees, float distanceMeters) {
            Kind = kind;
            ScreenPosition = screenPosition;
            IsOnScreen = isOnScreen;
            ArrowDegrees = arrowDegrees;
            DistanceMeters = distanceMeters;
        }
    }
}
