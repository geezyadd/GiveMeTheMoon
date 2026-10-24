using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    // Where a world marker goes on the screen: over its object, or pinned to the screen's edge with an arrow that
    // points the way to it.
    public readonly struct SonarMarkerPlacement {
        public Vector2 ScreenPosition { get; }
        public bool IsOnScreen { get; }
        public float ArrowDegrees { get; }

        public SonarMarkerPlacement(Vector2 screenPosition, bool isOnScreen, float arrowDegrees) {
            ScreenPosition = screenPosition;
            IsOnScreen = isOnScreen;
            ArrowDegrees = arrowDegrees;
        }
    }
}
