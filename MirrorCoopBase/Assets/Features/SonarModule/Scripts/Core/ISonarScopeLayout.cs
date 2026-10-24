using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    public interface ISonarScopeLayout {
        public bool TryPlace(Vector3 shipPosition, float shipYawDegrees, Vector3 contactPosition, float rangeMeters, bool isShownBeyondRange, out Vector2 scopePosition);
        public float GetDistanceShare(Vector3 shipPosition, Vector3 contactPosition, float rangeMeters);
    }
}
