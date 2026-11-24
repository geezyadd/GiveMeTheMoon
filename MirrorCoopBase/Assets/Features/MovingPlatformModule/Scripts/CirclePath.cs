using System;
using UnityEngine;

namespace Features.MovingPlatformModule {
    // A platform's way around a circle: where it is on the circle at a given time and how it is turned. The motion is a
    // pure function of time, so every peer computes the same pose from the same few synced values with no buffering.
    public static class CirclePath {
        private const double FULL_TURN = Math.PI * 2d;

        // The angle on the circle, radians in [0, 2π). While running it grows from the angle and time of the last start
        // or stop; while stopped it stays.
        public static float AngleAt(bool isRunning, float angleAtChange, double changedAt, float angularSpeed, double time) {
            double angle = angleAtChange;
            if (isRunning)
                angle += angularSpeed * (time - changedAt);

            angle %= FULL_TURN;
            if (angle < 0d)
                angle += FULL_TURN;

            return (float)angle;
        }

        public static Vector3 PositionAt(Vector3 center, float radius, float angle) =>
            center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

        // Faces along the way it goes (the angle grows counterclockwise seen from above), so it turns a full circle per
        // lap.
        public static Quaternion RotationAt(float angle) =>
            Quaternion.LookRotation(new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)), Vector3.up);
    }
}
