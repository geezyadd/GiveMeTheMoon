using UnityEngine;

namespace Features.ShipModule.Scripts {
    /// <summary>
    /// Route progress in work-seconds at flight speed 1.
    /// HUD time = WorkRemaining / Speed.
    /// Each cruise frame: WorkRemaining -= dt * Speed * Alignment.
    /// Alignment is Dot(nose, destDir): 1 toward (time falls), -1 away (time rises).
    /// </summary>
    public sealed class ShipTransit {
        public const float MinSpeed = 0.01f;

        public float RouteWork { get; private set; }
        public float WorkRemaining { get; private set; }
        public float Speed { get; private set; } = 1f;
        public float Alignment { get; private set; } = 1f;
        public Vector3 DestinationDirection { get; private set; } = Vector3.forward;

        public float SecondsRemaining => SecondsRemainingAt(WorkRemaining, Speed);

        public static float SecondsRemainingAt(float workRemaining, float speed) {
            float speedSafe = Mathf.Max(MinSpeed, speed);
            return Mathf.Max(0f, workRemaining / speedSafe);
        }

        public bool HasArrived => WorkRemaining <= 0f;

        public void Reset() {
            RouteWork = 0f;
            WorkRemaining = 0f;
            Speed = 1f;
            Alignment = 1f;
            DestinationDirection = Vector3.forward;
        }

        public void BeginRoute(float routeWork, Vector3 destinationDirection) {
            RouteWork = Mathf.Max(0f, routeWork);
            WorkRemaining = RouteWork;
            DestinationDirection = destinationDirection.sqrMagnitude > 0.0001f
                ? destinationDirection.normalized
                : Vector3.forward;
            Alignment = 1f;
        }

        public void PreviewRoute(float routeWork, float speed, Vector3 destinationDirection) {
            BeginRoute(routeWork, destinationDirection);
            SetSpeed(speed);
        }

        public void SetSpeed(float speed) {
            Speed = Mathf.Max(0f, speed);
        }

        public void SetAlignment(float alignment) {
            Alignment = alignment;
        }

        public bool Tick(float deltaTime) {
            WorkRemaining -= deltaTime * Speed * Alignment;
            if (WorkRemaining > 0f)
                return false;

            WorkRemaining = 0f;
            return true;
        }

        public static float EvaluateAlignment(Vector3 travelDirection, Vector3 destinationDirection) {
            if (travelDirection.sqrMagnitude < 0.0001f || destinationDirection.sqrMagnitude < 0.0001f)
                return 1f;

            return Vector3.Dot(travelDirection.normalized, destinationDirection.normalized);
        }
    }
}
