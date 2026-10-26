using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    // A top view around the ship with its nose up: a contact's place on the scope, -1..1 on both axes, the circle's
    // edge at the sonar range. Height does not count.
    public sealed class SonarScopeLayout : ISonarScopeLayout {
        public bool TryPlace(Vector3 shipPosition, float shipYawDegrees, Vector3 contactPosition, float rangeMeters, bool isShownBeyondRange, out Vector2 scopePosition) {
            scopePosition = Vector2.zero;
            Vector3 offset = Quaternion.Euler(0f, -shipYawDegrees, 0f) * (contactPosition - shipPosition);
            Vector2 flat = new Vector2(offset.x, offset.z) / rangeMeters;
            bool isBeyondRange = flat.sqrMagnitude > 1f;
            if (isBeyondRange && isShownBeyondRange == false)
                return false;

            scopePosition = isBeyondRange ? flat.normalized : flat;
            return true;
        }

        public float GetDistanceShare(Vector3 shipPosition, Vector3 contactPosition, float rangeMeters) {
            Vector3 offset = contactPosition - shipPosition;
            offset.y = 0f;
            return offset.magnitude / rangeMeters;
        }
    }
}
