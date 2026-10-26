using UnityEngine;

namespace Features.ShipModule.Scripts {
    // The lowest point of an item's solid colliders in ship space, where the item touches the deck when it rests on it.
    internal sealed class DeckCargoLowestPoint {
        private const int CAPSULE_AXIS_X = 0;
        private const int CAPSULE_AXIS_Y = 1;

        private readonly Transform _ship;

        internal DeckCargoLowestPoint(Transform ship) =>
            _ship = ship;

        public bool TryFindLowestShipY(Collider[] colliders, out float lowestY) {
            lowestY = float.PositiveInfinity;
            for (int i = 0; i < colliders.Length; i++) {
                Collider collider = colliders[i];
                if (IsSolid(collider) == false)
                    continue;

                lowestY = Mathf.Min(lowestY, FindLowestShipY(collider));
            }

            return float.IsPositiveInfinity(lowestY) == false;
        }

        private static bool IsSolid(Collider collider) =>
            collider != null && collider.enabled && collider.isTrigger == false && collider.gameObject.activeInHierarchy;

        private float FindLowestShipY(Collider collider) {
            switch (collider) {
                case BoxCollider box:
                    return FindLowestBoxY(box.transform, box.center, box.size);
                case SphereCollider sphere:
                    return FindLowestSphereY(sphere);
                case CapsuleCollider capsule:
                    return FindLowestCapsuleY(capsule);
                default:
                    return FindLowestBoundsY(collider.bounds);
            }
        }

        private float FindLowestBoxY(Transform frame, Vector3 center, Vector3 size) {
            Vector3 half = size * 0.5f;
            float lowest = float.PositiveInfinity;
            for (int corner = 0; corner < 8; corner++) {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                lowest = Mathf.Min(lowest, ShipY(frame.TransformPoint(center + Vector3.Scale(half, sign))));
            }

            return lowest;
        }

        private float FindLowestBoundsY(Bounds bounds) {
            float lowest = float.PositiveInfinity;
            for (int corner = 0; corner < 8; corner++) {
                Vector3 point = new Vector3(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                lowest = Mathf.Min(lowest, ShipY(point));
            }

            return lowest;
        }

        private float FindLowestSphereY(SphereCollider sphere) {
            Vector3 scale = Abs(sphere.transform.lossyScale);
            float radius = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            return ShipY(sphere.transform.TransformPoint(sphere.center)) - radius;
        }

        private float FindLowestCapsuleY(CapsuleCollider capsule) {
            Transform frame = capsule.transform;
            Vector3 scale = Abs(frame.lossyScale);
            Vector3 axis = CapsuleAxis(capsule.direction);
            float radius = capsule.radius * CapsuleRadiusScale(capsule.direction, scale);
            float halfSegment = Mathf.Max(0f, capsule.height * 0.5f * Vector3.Dot(scale, axis) - radius);
            Vector3 center = frame.TransformPoint(capsule.center);
            Vector3 segment = frame.TransformDirection(axis) * halfSegment;
            return Mathf.Min(ShipY(center + segment), ShipY(center - segment)) - radius;
        }

        private float ShipY(Vector3 world) =>
            _ship.InverseTransformPoint(world).y;

        private static Vector3 CapsuleAxis(int direction) {
            if (direction == CAPSULE_AXIS_X)
                return Vector3.right;

            return direction == CAPSULE_AXIS_Y ? Vector3.up : Vector3.forward;
        }

        // A capsule's radius scales with the larger of the two scale axes across its own axis, as PhysX does.
        private static float CapsuleRadiusScale(int direction, Vector3 scale) {
            if (direction == CAPSULE_AXIS_X)
                return Mathf.Max(scale.y, scale.z);

            return direction == CAPSULE_AXIS_Y ? Mathf.Max(scale.x, scale.z) : Mathf.Max(scale.x, scale.y);
        }

        private static Vector3 Abs(Vector3 value) =>
            new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
