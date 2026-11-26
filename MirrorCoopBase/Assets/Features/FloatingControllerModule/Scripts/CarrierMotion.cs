using UnityEngine;

namespace Features.FloatingControllerModule {
    // How a carrier's step moves what rides on it: a point keeps its place in the carrier's axes, a velocity relative to
    // the carrier turns with it, and the speed of a point of the carrier is how far that step carries it.
    public static class CarrierMotion {
        public static Vector3 CarryPoint(Pose before, Pose now, Vector3 point) =>
            now.position + now.rotation * (Quaternion.Inverse(before.rotation) * (point - before.position));

        public static Quaternion TurnOf(Pose before, Pose now) =>
            now.rotation * Quaternion.Inverse(before.rotation);

        // The turn around the up axis only, degrees: what a rider's look follows.
        public static float YawOf(Pose before, Pose now) =>
            Mathf.DeltaAngle(before.rotation.eulerAngles.y, now.rotation.eulerAngles.y);

        // The pose's turn around the up axis only: walking and looking follow the carrier's heading, not its rocking.
        public static Quaternion HeadingOf(Pose pose) =>
            Quaternion.Euler(0f, pose.rotation.eulerAngles.y, 0f);

        public static Vector3 PointVelocity(Pose before, Pose now, Vector3 point, float stepSeconds) =>
            (CarryPoint(before, now, point) - point) / stepSeconds;

        public static Vector3 ToLocalPoint(Pose pose, Vector3 worldPoint) =>
            Quaternion.Inverse(pose.rotation) * (worldPoint - pose.position);

        public static Vector3 ToWorldPoint(Pose pose, Vector3 localPoint) =>
            pose.position + pose.rotation * localPoint;

        public static Vector3 ToLocalDirection(Pose pose, Vector3 worldDirection) =>
            Quaternion.Inverse(pose.rotation) * worldDirection;

        public static Vector3 ToWorldDirection(Pose pose, Vector3 localDirection) =>
            pose.rotation * localDirection;
    }
}
