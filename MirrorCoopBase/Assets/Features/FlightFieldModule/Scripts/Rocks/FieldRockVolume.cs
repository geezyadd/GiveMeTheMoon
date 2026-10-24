using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // A world volume the rock query looks into: a sphere or a turned box, with the radius of a sphere around it that
    // picks the field cells to look at.
    public readonly struct FieldRockVolume {
        private readonly Vector3 _halfExtents;
        private readonly Quaternion _inverseRotation;
        private readonly bool _isBox;

        public Vector3 Center { get; }
        public float BoundingRadius { get; }

        private FieldRockVolume(Vector3 center, float boundingRadius, Vector3 halfExtents, Quaternion inverseRotation, bool isBox) {
            Center = center;
            BoundingRadius = boundingRadius;
            _halfExtents = halfExtents;
            _inverseRotation = inverseRotation;
            _isBox = isBox;
        }

        public static FieldRockVolume CreateSphere(Vector3 center, float radius) =>
            new(center, radius, Vector3.zero, Quaternion.identity, false);

        public static FieldRockVolume CreateBox(Vector3 center, Vector3 halfExtents, Quaternion rotation) =>
            new(center, halfExtents.magnitude, halfExtents, Quaternion.Inverse(rotation), true);

        // Whether a sphere around a rock touches the volume.
        public bool Intersects(Vector3 rockCenter, float rockRadius) {
            if (!_isBox)
                return (rockCenter - Center).sqrMagnitude <= (BoundingRadius + rockRadius) * (BoundingRadius + rockRadius);

            Vector3 local = _inverseRotation * (rockCenter - Center);
            Vector3 nearest = new(Mathf.Clamp(local.x, -_halfExtents.x, _halfExtents.x), Mathf.Clamp(local.y, -_halfExtents.y, _halfExtents.y),
                Mathf.Clamp(local.z, -_halfExtents.z, _halfExtents.z));
            return (local - nearest).sqrMagnitude <= rockRadius * rockRadius;
        }
    }
}
