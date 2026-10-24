using Features.SonarModule.Scripts.Configurations;
using UnityEngine;

namespace Features.SonarModule.Scripts.Core {
    // A marker in front of the camera and inside the screen less the edge margin stays over its object; any other
    // one slides along the line from the screen's middle to the edge of that inner rectangle. Behind the camera the
    // projection is mirrored, so the line is turned around to point the right way.
    public sealed class SonarMarkerProjection : ISonarMarkerProjection {
        private const float ARROW_UP_DEGREES = 90f;
        private const float MIN_DIRECTION = 0.0001f;

        private readonly SonarConfiguration _sonarConfiguration;

        public SonarMarkerProjection(SonarConfiguration sonarConfiguration) =>
            _sonarConfiguration = sonarConfiguration;

        public SonarMarkerPlacement Place(Vector3 screenPoint, Vector2 screenSize) {
            float margin = _sonarConfiguration.MarkerEdgeMarginPixels;
            Vector2 point = new Vector2(screenPoint.x, screenPoint.y);
            bool isInFront = screenPoint.z > 0f;
            bool isInside = point.x >= margin && point.x <= screenSize.x - margin && point.y >= margin && point.y <= screenSize.y - margin;
            if (isInFront && isInside)
                return new SonarMarkerPlacement(point, true, 0f);

            Vector2 center = screenSize * 0.5f;
            Vector2 direction = isInFront ? point - center : center - point;
            if (direction.sqrMagnitude < MIN_DIRECTION)
                direction = Vector2.down;

            Vector2 halfInner = center - new Vector2(margin, margin);
            float scale = Mathf.Min(
                Mathf.Abs(direction.x) < MIN_DIRECTION ? float.MaxValue : halfInner.x / Mathf.Abs(direction.x),
                Mathf.Abs(direction.y) < MIN_DIRECTION ? float.MaxValue : halfInner.y / Mathf.Abs(direction.y));
            float degrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - ARROW_UP_DEGREES;
            return new SonarMarkerPlacement(center + direction * scale, false, degrees);
        }
    }
}
